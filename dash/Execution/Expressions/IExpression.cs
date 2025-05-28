using dash.Execution.Standart;
using dash.Execution.Structures;
using dash.Execution.Units;
using dash.Lexing;
using dash.Parsing;

namespace dash.Execution.Expressions;

public interface IExpression
{
    public ParsedMeta Meta { get; set; }
    public ADValue Do(ref ExecCtx ctx);
}

public interface ILoop : IExpression
{
    ADValue IExpression.Do(ref ExecCtx ctx)
    {
        ctx.CallTrace.NewCall(Call.LOOP);
        var a = Loop(ref ctx);
        ctx.CallTrace.EndCall();
        return a;
    }

    public ADValue Loop(ref ExecCtx ctx);
}

public interface IAccess : IExpression
{
    public DValue Get(ref ExecCtx ctx);
    public void Set(DValue value, ref ExecCtx ctx);
}

public class BoxExpression(ParsedMeta meta, IExpression folded) : IExpression
{
    public ParsedMeta Meta { get; set; } = meta;
    public ADValue Do(ref ExecCtx ctx)
    {
        return folded.Do(ref ctx);
    }
}

public class IfExpression(ParsedMeta meta, IExpression condition, IExpression ifTrue, IExpression? ifElse) : IExpression
{
    public ParsedMeta Meta { get; set; } = meta;
    public ADValue Do(ref ExecCtx ctx)
    {
        var condCtx = ctx.Child();

        bool a = condition.Do(ref condCtx).AsD(Meta).AsBool(Meta);

        var doCtx = condCtx.Child();

        if (a)
        {
            return ifTrue.Do(ref doCtx);
        }
        return ifElse?.Do(ref doCtx) ?? new DValue(null, SimpleDType.NULL);
    }
}

public class LambdaExpression(List<(string Id, DType type)> mask, DType ret, IExpression expr, ParsedMeta meta) : IExpression
{
    public ParsedMeta Meta { get; set; } = meta;
    public ADValue Do(ref ExecCtx ctx)
    {
        Dictionary<string, DType> masked = [];

        foreach (var i in mask)
        {
            masked[i.Id] = i.type;
        }

        return new DValue(new DFunction(masked, ret, expr, ctx.CurrentScope), SimpleDType.CALLABLE);
    }
}

public class FunctionExpression(List<(string Id, DType type)> mask, string name, DType ret, IExpression expr, ParsedMeta meta,
    string? module = null) : IExpression
{

    public string Name = name;
    public ParsedMeta Meta { get; set; } = meta;
    public ADValue Do(ref ExecCtx ctx)
    {
        Dictionary<string, DType> masked = [];

        foreach (var i in mask)
        {
            masked[i.Id] = i.type;
        }

        ctx.CurrentScope.Let(name, new DValue(new DFunction(masked, ret, expr, ctx.CurrentScope), SimpleDType.CALLABLE), meta);

        return ctx.CurrentScope.Get(name, meta);
    }
}

public class StructDecl(string name, List<(string Id, DType type)> mask, List<string> inherited, ParsedMeta meta) : IExpression
{
    public ParsedMeta Meta { get; set; } = meta;
    public ADValue Do(ref ExecCtx ctx)
    {
        ctx.CurrentScope.Let(name,
            new DValue(new Constructor(mask, name, inherited), SimpleDType.CONSTRUCTOR), meta);

        return ctx.CurrentScope.Get(name, meta);
    }
}

public class LetExpression(ParsedMeta meta, string name, IExpression value, DType expect) : IExpression
{
    public ParsedMeta Meta { get; set; } = meta;
    public ADValue Do(ref ExecCtx ctx)
    {
        var res = value.Do(ref ctx).AsD(meta);

        if (!expect.IsCompatibleWith(res.GetDType()))
        {
            Eroro.MakeEroro($"Expected '{expect.GetName()}', got '{res.GetDType().GetName()}'", meta);
        }

        ctx.CurrentScope.Let(name, res, meta);

        return ctx.CurrentScope.Get(name, meta);
    }
}

public class NoCListedExpression(ParsedMeta meta, List<IExpression> expressions) : IExpression
{
    public ParsedMeta Meta { get; set; } = meta;
    public ADValue Do(ref ExecCtx ctx)
    {

        DValue end = new DValue(null, SimpleDType.NULL);

        foreach (var i in expressions)
        {
            ctx.CallTrace.NewCall(i is ILoop ? Call.LOOP : Call.EXPR);
            var b = i.Do(ref ctx);

            if (b is DReturn ret)
            {
                return ret;
            }
            if (b is DBreak @break)
            {
                return @break;
            }
            if (b is DContinue @continue)
            {
                return @continue;
            }

            end = b.AsD(meta);

            ctx.CallTrace.EndCall();
        }

        return end;
    }
}

public class ListedExpression(ParsedMeta meta, List<IExpression> expressions) : IExpression
{
    public ParsedMeta Meta { get; set; } = meta;
    public ADValue Do(ref ExecCtx ctx)
    {
        var a = ctx.Child();

        DValue end = new DValue(null, SimpleDType.NULL);

        foreach (var i in expressions)
        {
            a.CallTrace.NewCall(i is ILoop ? Call.LOOP : Call.EXPR);
            var b = i.Do(ref a);

            if (b is DReturn ret)
            {
                return ret;
            }
            if (b is DBreak @break)
            {
                return @break;
            }
            if (b is DContinue @continue)
            {
                return @continue;
            }

            end = b.AsD(meta);

            a.CallTrace.EndCall();
        }

        return end;
    }
}

public class CallableFromScope(ParsedMeta meta, string scopeName, string funName) : IExpression
{
    public ParsedMeta Meta { get; set; } = meta;
    public ADValue Do(ref ExecCtx ctx)
    {
        if (ctx.CurrentState.Modules.ContainsKey(scopeName))
        {
            return new DValue(ctx.CurrentState.GetModule(scopeName, Meta).Get(funName, Meta).AsCallable(Meta), SimpleDType.CALLABLE);
        }
        else
        {
            Eroro.MakeEroro($"Module '{scopeName}' doesn't exist or is not imported", Meta);
            throw new Exception();
        }
    }
}

public class InvokeExpression(ParsedMeta meta, IExpression callable, List<IExpression> args) : IExpression
{
    public ParsedMeta Meta { get; set; } = meta;
    public IExpression Callable = callable;
    public List<IExpression> Args = args;


    public ADValue Do(ref ExecCtx ctx)
    {
        var _call = callable.Do(ref ctx).AsD(Meta);

        ICallable call;

        if (_call.GetDType() is StructDType)
        {
            var _struct = _call.AsStruct(meta);
            if (_struct._values.Count > 0)
            {
                var firstField = _struct._values.Keys.ToList()[0];

                if (_struct._types[firstField].IsCompatibleWith(SimpleDType.CALLABLE) ||
                    _struct._types[firstField].IsCompatibleWith(SimpleDType.CONSTRUCTOR))
                {
                    call = _struct._values[firstField].AsCallable(meta);
                }
                else
                {
                    Eroro.MakeEroro("First field of callable struct must be callable", meta);
                    throw new Exception();
                }
            }
            else
            {
                Eroro.MakeEroro("Callable struct must have at least one callable field", meta);
                throw new Exception();
            }
        }
        else
        {
            call = _call.AsCallable(meta);
        }

        List<ADValue> newArgs = [];

        foreach (var i in args)
        {
            newArgs.Add(i.Do(ref ctx).AsD(Meta));
        }

        var res = call.Call(newArgs, Meta, ref ctx);

        return res;
    }
}

public class DictExpression(ParsedMeta meta, List<(string key, IExpression val)> contents) : IExpression
{
    public ParsedMeta Meta { get; set; }
    public ADValue Do(ref ExecCtx ctx)
    {
        Dictionary<string, DValue> res = [];

        foreach (var i in contents)
        {
            res[i.key] = i.val.Do(ref ctx).AsD(i.val.Meta);
        }

        return new DValue(res, SimpleDType.DICT);
    }
}

public class ListExpression(ParsedMeta meta, List<IExpression> contents) : IExpression
{
    public ParsedMeta Meta { get; set; } = meta;
    public ADValue Do(ref ExecCtx ctx)
    {
        List<DValue> values = [];

        foreach (var i in contents)
        {
            values.Add(i.Do(ref ctx).AsD(i.Meta));
        }

        return new DValue(values, SimpleDType.LIST);
    }
}

public class IndexExpression(ParsedMeta meta, IExpression lst, IExpression ind) : IAccess
{
    public ParsedMeta Meta { get; set; } = meta;
    private string indexedType = "";
    public ADValue Do(ref ExecCtx ctx)
    {
        if (indexedType == "")
        {
            indexedType = lst.Do(ref ctx).AsD(Meta).GetDType().GetName();
        }
        return Get(ref ctx);
    }

    public DValue Get(ref ExecCtx ctx)
    {
        if (indexedType == "list")
        {
            var index = ind.Do(ref ctx).AsD(meta).AsInt(Meta);
            var list = lst.Do(ref ctx).AsD(meta).AsList(Meta);
            bool negative = false;

            if (index < 0)
            {
                index *= -1;
                negative = true;
            }

            if (index >= list.Count)
            {
                Eroro.MakeEroro($"Index out of bounds: tried to get '{index}' in list of '{list.Count}' elements",
                    Meta);
                throw new Exception();
            }

            if (negative)
            {
                return list[^index];
            }

            return list[index];
        }
        if (indexedType == "dict")
        {
            var index = ind.Do(ref ctx).AsD(meta).AsString();
            var dict = lst.Do(ref ctx).AsD(meta).AsDict(meta);

            if (dict.TryGetValue(index, out var value))
            {
                return value;
            }
            Eroro.MakeEroro($"Dict doesn't contain key '{index}'", Meta);
            throw new Exception();
        }

        if (indexedType == "str")
        {
            var index = ind.Do(ref ctx).AsD(meta).AsInt(meta);
            var str = lst.Do(ref ctx).AsD(meta).AsString();

            if (index >= str.Length)
            {
                Eroro.MakeEroro($"Index out of bounds: tried to get '{index}' in str with length of '{str.Length}' chars",
                    Meta);
            }

            return new DValue(str[index].ToString(), SimpleDType.STR);
        }
        Eroro.MakeEroro($"Type '{indexedType}' can't be indexed", Meta);
        throw new Exception();
    }

    public void Set(DValue value, ref ExecCtx ctx)
    {
        if (indexedType == "")
        {
            indexedType = lst.Do(ref ctx).AsD(Meta).GetDType().GetName();
        }

        if (indexedType == "list")
        {
            var index = ind.Do(ref ctx).AsD(meta).AsInt(Meta);
            var list = lst.Do(ref ctx).AsD(meta).AsList(Meta);
            bool negative = false;

            if (index < 0)
            {
                index *= -1;
                negative = true;
            }

            if (index >= list.Count)
            {
                Eroro.MakeEroro($"Index out of bounds: tried to get '{index}' in list of '{list.Count}' elements",
                    Meta);
                throw new Exception();
            }

            if (negative)
            {
                list[^index] = value;
            }
            else
            {
                list[index] = value;
            }
        }
        else if (indexedType == "dict")
        {
            var index = ind.Do(ref ctx).AsD(meta).AsString();
            var dict = lst.Do(ref ctx).AsD(meta).AsDict(meta);

            dict[index] = value;
        }
        else
        {
            Eroro.MakeEroro($"Type '{indexedType}' can't be indexed", Meta);
            throw new Exception();
        }
    }
}

public class ExecExpression(ParsedMeta meta, IExpression expr) : IExpression
{
    public ParsedMeta Meta { get; set; } = meta;
    public ADValue Do(ref ExecCtx ctx)
    {
        var a = expr.Do(ref ctx).AsD(Meta).AsString();

        var tokens = Lexer.Tokenize(a, meta.Meta.File);
        var parsed = new Parser(meta.Meta.File, tokens, ctx.CurrentState).ParseEverything();

        ADValue res = new DValue(null, SimpleDType.NULL);

        foreach (var i in parsed)
        {
            res = i.Do(ref ctx);
        }

        return res;
    }
}

public class AssignExpression(ParsedMeta meta, IExpression a, IExpression b) : IExpression
{
    public ParsedMeta Meta { get; set; } = meta;
    public ADValue Do(ref ExecCtx ctx)
    {
        if (a is IAccess access)
        {
            access.Set(b.Do(ref ctx).AsD(meta), ref ctx);

            return access.Get(ref ctx);
        }
        else
        {
            var t = a.Do(ref ctx).AsD(Meta);

            if (t.GetDType().GetName() == "ref")
            {
                var c = t.AsRefObj(Meta);
                c.Set(b.Do(ref ctx).AsD(Meta));
                return c.Get();
            }

            Eroro.MakeEroro("Non-accessible identifier", Meta);
            throw new Exception();
        }
    }
}

public class IdExpression(ParsedMeta meta, string name) : IAccess
{
    public ParsedMeta Meta { get; set; } = meta;

    public string GetName => name;

    public ADValue Do(ref ExecCtx ctx)
    {
        return Get(ref ctx);
    }

    public DValue Get(ref ExecCtx ctx)
    {
        var c = ctx.CurrentScope.Get(name, Meta);

        if (c.GetDType().GetName() == "ref")
        {
            var t = c.AsRefObj(Meta).Get();
            return t;
        }

        return ctx.CurrentScope.Get(name, Meta);
    }

    public void Set(DValue value, ref ExecCtx ctx)
    {
        var c = ctx.CurrentScope.Get(name, Meta);

        if (c.GetDType().GetName() == "ref")
        {
            c.AsRefObj(Meta).Set(value);
            return;
        }

        ctx.CurrentScope.ReAssign(name, value, Meta);
    }
}

public class RefExpression(ParsedMeta meta, IAccess access) : IExpression
{
    public ParsedMeta Meta { get; set; } = meta;
    public ADValue Do(ref ExecCtx ctx)
    {
        return new DValue(new RefObj(access, ctx), SimpleDType.REF);
    }
}

public class ScopedAccess(ParsedMeta meta, string scope, string name) : IAccess
{
    public ParsedMeta Meta { get; set; } = meta;
    public ADValue Do(ref ExecCtx ctx)
    {
        return Get(ref ctx);
    }

    public DValue Get(ref ExecCtx ctx)
    {
        if (ctx.CurrentState.Modules.ContainsKey(scope))
        {
            return ctx.CurrentState.GetModule(scope, Meta).Get(name, Meta);
        }

        Eroro.MakeEroro($"Module '{scope}' not imported", Meta);
        throw new Exception();
    }

    public void Set(DValue value, ref ExecCtx ctx)
    {

    }
}

public class VoidExpression(ParsedMeta meta) : IAccess
{
    public ParsedMeta Meta { get; set; } = meta;
    public ADValue Do(ref ExecCtx ctx)
    {
        return Get(ref ctx);
    }

    public DValue Get(ref ExecCtx ctx)
    {
        return new DValue(null, SimpleDType.NULL);
    }

    public void Set(DValue value, ref ExecCtx ctx) { /* Ignore */}
}

public class FieldExpression(ParsedMeta meta, IExpression structExpr, string field) : IAccess
{
    public ParsedMeta Meta { get; set; } = meta;
    public ADValue Do(ref ExecCtx ctx)
    {
        return Get(ref ctx);
    }

    public DValue Get(ref ExecCtx ctx)
    {
        var _struct = structExpr.Do(ref ctx).AsD(Meta).AsStruct(Meta);
        var _field = _struct.Get(field, Meta);

        return _field;
    }

    public void Set(DValue value, ref ExecCtx ctx)
    {
        var _struct = structExpr.Do(ref ctx).AsD(Meta).AsStruct(Meta);

        _struct.Set(field, value, Meta);
    }
}

public class AsOperator(ParsedMeta meta, IExpression source, DType targetType) : IExpression
{
    public ParsedMeta Meta { get; set; } = meta;
    public ADValue Do(ref ExecCtx ctx)
    {
        var a = source.Do(ref ctx).AsD(meta);
        var sourceType = a.GetDType().GetName();
        var targetTypeStr = targetType.GetName();

        if (targetType is EitherDType either)
        {
            Eroro.MakeEroro($"Cast {sourceType} -> {targetTypeStr} unsupported", meta);
            throw new Exception();
        }

        if (targetTypeStr == "decimal")
        {
            try
            {
                return new DValue(Convert.ToDecimal(a.GetRawNonNull(meta)), SimpleDType.DECIMAL);
            }
            catch
            {
                CastErr();
            }
        }

        if (targetTypeStr == "double")
        {
            try
            {
                return new DValue(Convert.ToDouble(a.GetRawNonNull(meta)), SimpleDType.DOUBLE);
            }
            catch
            {
                CastErr();
            }
        }

        if (targetTypeStr == "float")
        {
            try
            {
                return new DValue((float)a.GetRawNonNull(meta), SimpleDType.FLOAT);
            }
            catch
            {
                CastErr();
            }
        }

        if (targetTypeStr == "int")
        {
            try
            {
                return new DValue(Convert.ToInt32(a.GetRawNonNull(meta)), SimpleDType.INT);
            }
            catch
            {
                CastErr();
            }
        }

        if (targetTypeStr == "callable")
        {
            try
            {
                return new DValue((a.GetRawNonNull(meta) as ICallable), SimpleDType.CALLABLE);
            }
            catch
            {
                CastErr();
            }
        }

        if (targetTypeStr == "bool")
        {
            try
            {
                return new DValue(Convert.ToBoolean(a.GetRawNonNull(meta)), SimpleDType.BOOL);
            }
            catch
            {
                CastErr();
            }
        }

        if (targetTypeStr == "str")
        {
            return new DValue(a.Show(), SimpleDType.STR);
        }

        if (targetTypeStr == "list" && sourceType == "str")
        {
            List<DValue> listedStr = [];

            foreach (var i in a.AsString())
            {
                listedStr.Add(new DValue($"{i}", SimpleDType.STR));
            }

            return new DValue(listedStr, SimpleDType.LIST);
        }

        CastErr();
        throw new Exception();

        void CastErr()
        {
            Eroro.MakeEroro($"Cast {sourceType} -> {targetTypeStr} unsupported", meta);
            throw new Exception();
        }
    }
}

public class IsOperator(ParsedMeta meta, IExpression source, DType targetType) : IExpression
{
    public ParsedMeta Meta { get; set; } = meta;
    public ADValue Do(ref ExecCtx ctx)
    {
        var val = source.Do(ref ctx).AsD(meta);
        return new DValue(targetType.IsCompatibleWith(val.GetDType()), SimpleDType.BOOL);
    }
}

public class ReturnExpression(ParsedMeta meta, IExpression toReturn) : IExpression
{
    public ParsedMeta Meta { get; set; } = meta;
    public ADValue Do(ref ExecCtx ctx)
    {
        if (ctx.CallTrace.IsIn(Call.FUNCTION))
        {
            return new DReturn(toReturn.Do(ref ctx));
        }

        Eroro.MakeEroro("Can't return outside the function", Meta);
        throw new Exception();
    }
}

public class Continue(ParsedMeta meta) : IExpression
{
    public ParsedMeta Meta { get; set; } = meta;
    public ADValue Do(ref ExecCtx ctx)
    {
        if (ctx.CallTrace.IsIn(Call.LOOP))
        {
            return new DContinue();
        }
        Eroro.MakeEroro("Can't continue outside the loop", Meta);
        throw new Exception();
    }
}

public class Break(ParsedMeta meta) : IExpression
{
    public ParsedMeta Meta { get; set; } = meta;
    public ADValue Do(ref ExecCtx ctx)
    {
        if (ctx.CallTrace.IsIn(Call.LOOP))
        {
            return new DBreak();
        }
        Eroro.MakeEroro("Can't break outside the loop", Meta);
        throw new Exception();
    }
}

public class ForeachLoop(ParsedMeta meta, IExpression list, string varName, IExpression body) : ILoop
{
    public ParsedMeta Meta { get; set; } = meta;
    public ADValue Loop(ref ExecCtx ctx)
    {
        var listCtx = ctx.Child();
        var listItself = list.Do(ref listCtx).AsD(Meta).AsList(Meta);

        if (listItself.Count == 0)
        {
            return new DValue(null, SimpleDType.NULL);
        }

        int currentCount = 0;

        while (currentCount < listItself.Count)
        {
            var execCtx = listCtx.Child();
            execCtx.CurrentScope.Set(varName, listItself[currentCount]);

            var res = body.Do(ref execCtx);

            if (res is DBreak)
            {
                break;
            }

            if (res is DContinue)
            {
                currentCount++;
                continue;
            }

            if (res is DReturn ret)
            {
                return ret;
            }

            currentCount++;
        }

        return new DValue(null, SimpleDType.NULL);
    }
}

public class ForLoop(ParsedMeta meta, IExpression before, IExpression condition, IExpression after, IExpression body)
    : ILoop
{
    public ParsedMeta Meta { get; set; } = meta;
    public ADValue Loop(ref ExecCtx ctx)
    {
        var beforeCtx = ctx.Child();
        before.Do(ref beforeCtx);

        var conditionCtx = beforeCtx.Child();

        while (condition.Do(ref conditionCtx).AsD(meta).AsBool(meta))
        {
            var bodyCtx = conditionCtx.Child();
            var res = body.Do(ref bodyCtx);

            if (res is DBreak)
            {
                break;
            }

            if (res is DContinue)
            {
                After();
                continue;
            }

            if (res is DReturn ret)
            {
                return ret;
            }

            After();

            void After()
            {
                var afterCtx = conditionCtx.Child();
                after.Do(ref afterCtx);
            }
        }

        return new DValue(null, SimpleDType.NULL);
    }
}

public class WhileLoop(ParsedMeta meta, IExpression condition, IExpression body) : ILoop
{
    public ParsedMeta Meta { get; set; } = meta;
    public ADValue Loop(ref ExecCtx ctx)
    {
        var conditionScope = ctx.CurrentScope.MakeScope();
        var conditionCtx = new ExecCtx()
        {
            CurrentScope = conditionScope,
            CurrentState = ctx.CurrentState,
            CallTrace = ctx.CallTrace
        };

        while (condition.Do(ref conditionCtx).AsD(Meta).AsBool(Meta))
        {
            var bodyScope = conditionScope.MakeScope();
            var bodyCtx = new ExecCtx
            {
                CurrentScope = bodyScope,
                CurrentState = conditionCtx.CurrentState,
                CallTrace = conditionCtx.CallTrace
            };

            var res = body.Do(ref bodyCtx);

            if (res is DBreak)
            {
                break;
            }

            if (res is DContinue)
            {
                continue;
            }

            if (res is DReturn ret)
            {
                return ret;
            }
        }

        return new DValue(null, SimpleDType.NULL);
    }
}

public class UnaryOperator(ParsedMeta meta, IExpression expr, string op) : IExpression
{
    public ParsedMeta Meta { get; set; } = meta;
    public IExpression Expr = expr;

    public ADValue Do(ref ExecCtx ctx)
    {
        if (ctx.CurrentScope.Exists(op))
        {
            if (ctx.CurrentScope.Get(op, Meta).AsCallable(Meta).isUnary)
            {
                var res = ctx.CurrentScope.Get(op, Meta).AsCallable(Meta).Call([Expr.Do(ref ctx)], Meta, ref ctx);
                return res;
            }
        }

        if (UnaryOperators.Registry.TryGetValue(op, out var callable))
        {
            var res = callable(ref ctx, new UnaryCtx(Expr.Do(ref ctx).AsD(Meta)), Meta);
            return res;
        }

        Eroro.MakeEroro($"Operator '{op}' doesn't exist", Meta);
        throw new Exception();
    }
}

public class BinaryOperator(ParsedMeta meta, IExpression a, IExpression b, string op) : IExpression
{
    public IExpression A = a;
    public IExpression B = b;
    public string Op = op;

    public ParsedMeta Meta { get; set; } = meta;
    public ADValue Do(ref ExecCtx ctx)
    {
        if (ctx.CurrentScope.Exists(Op))
        {
            if (!ctx.CurrentScope.Get(op, Meta).AsCallable(Meta).isUnary)
            {
                var res = ctx.CurrentScope.Get(Op, Meta).AsCallable(Meta)
                    .Call([A.Do(ref ctx), B.Do(ref ctx)], Meta, ref ctx);
                return res;
            }
        }

        if (BinaryOperators.Registry.TryGetValue(Op, out var callable))
        {
            var res = callable(ref ctx, new OperatorCtx(A.Do(ref ctx).AsD(Meta), B.Do(ref ctx).AsD(Meta)), Meta);
            return res;
        }

        Eroro.MakeEroro($"Operator '{Op}' doesn't exist", Meta);
        throw new Exception();
    }
}

public class AtomExpression(ParsedMeta meta, DValue dValue) : IExpression
{
    public ParsedMeta Meta { get; set; } = meta;
    public ADValue Do(ref ExecCtx ctx) => dValue;
}
