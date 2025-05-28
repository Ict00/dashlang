using dash.Etc;
using dash.Execution.Expressions;
using dash.Execution.Units;
using dash.Lexing;

namespace dash.Execution.Standart;

public delegate DValue DOperator(ref ExecCtx ctx, OperatorCtx ctx2, ParsedMeta meta);
public delegate DValue DUOperator(ref ExecCtx ctx, UnaryCtx ctx2, ParsedMeta meta);

public static class UnaryOperators
{
    public static Dictionary<string, DUOperator> Registry = new()
    {
        ["!"] = (ref ExecCtx ctx, UnaryCtx ctx2, ParsedMeta meta) =>
            new DValue(!ctx2.val.AsBool(meta), SimpleDType.BOOL),
        ["-"] = (ref ExecCtx ctx, UnaryCtx ctx2, ParsedMeta meta) =>
        {
            var t = ctx2.val.GetDType().GetName();
            if (t == "int" || t == "double" || t == "decimal" || t == "float")
            {
                var c = Convert.ToDecimal(ctx2.val.GetRawNonNull(meta)) * -1;

                return CastUtil.CastNum(c, t);
            }

            Eroro.MakeEroro($"Expected number, got {ctx2.val.GetDType().GetName()}", meta);

            throw new Exception();
        }
    };
}

public static class BinaryOperators
{
    public static Dictionary<string, DOperator> Registry = new()
    {
        ["+"] = (ref ExecCtx ctx, OperatorCtx ctx2, ParsedMeta meta) =>
        {
            var t = ctx2.first.GetDType().GetName();

            if (t == "int" || t == "double" || t == "decimal" || t == "float")
            {
                var a = ctx2.first.AnyNumber(meta);
                var b = ctx2.second.AnyNumber(meta);

                var c = a + b;
                try
                {
                    return CastUtil.CastNum(c, t);
                }
                catch (OverflowException _)
                {
                    Eroro.MakeEroro("Overflow", meta);
                    throw new Exception();
                }
            }

            if (t == "str")
            {
                return new DValue($"{ctx2.first.AsString()}{ctx2.second.AsString()}", SimpleDType.STR);
            }

            if (t == "list")
            {
                var a = ctx2.first.AsList(meta);
                var b = ctx2.second.AsList(meta);

                foreach (var i in b)
                {
                    a.Add(i);
                }

                return new DValue(a, SimpleDType.LIST);
            }

            if (t == "callable")
            {
                ICallable a = ctx2.first.AsCallable(meta);
                ICallable b = ctx2.second.AsCallable(meta);

                if (a is DFunction ad && b is DFunction bd)
                {
                    var eA = ad.Execute;
                    var eB = bd.Execute;

                    var newExpr = new ListedExpression(meta, [eA, eB]);

                    var mask = new Dictionary<string, DType>();

                    foreach (var _a in ad.Mask)
                    {
                        mask[_a.Key] = _a.Value;
                    }

                    foreach (var _a in bd.Mask)
                    {
                        if (!mask.ContainsKey(_a.Key))
                            mask[_a.Key] = _a.Value;
                    }

                    return new DValue(new DFunction(mask, bd.Returned, newExpr, ad.Related), SimpleDType.CALLABLE);
                }
                else
                {
                    Eroro.MakeEroro("Invalid operation", meta);
                    throw new Exception();
                }

            }

            Eroro.MakeEroro("Invalid operation", meta);
            throw new Exception();
        },
        ["-"] = (ref ExecCtx ctx, OperatorCtx ctx2, ParsedMeta meta) =>
        {
            var t = ctx2.first.GetDType().GetName();

            if (t == "int" || t == "double" || t == "decimal" || t == "float")
            {
                var a = ctx2.first.AnyNumber(meta);
                var b = ctx2.second.AnyNumber(meta);

                var c = a - b;
                try
                {
                    return CastUtil.CastNum(c, t);
                }
                catch (OverflowException _)
                {
                    Eroro.MakeEroro("Overflow", meta);
                    throw new Exception();
                }
            }

            Eroro.MakeEroro("Invalid operation", meta);
            throw new Exception();
        },
        ["*"] = (ref ExecCtx ctx, OperatorCtx ctx2, ParsedMeta meta) =>
        {
            var t = ctx2.first.GetDType().GetName();

            if (t == "int" || t == "double" || t == "decimal" || t == "float")
            {
                var a = ctx2.first.AnyNumber(meta);
                var b = ctx2.second.AnyNumber(meta);

                var c = a * b;
                try
                {
                    return CastUtil.CastNum(c, t);
                }
                catch (OverflowException _)
                {
                    Eroro.MakeEroro("Overflow", meta);
                    throw new Exception();
                }
            }

            Eroro.MakeEroro("Invalid operation", meta);
            throw new Exception();
        },
        ["/"] = (ref ExecCtx ctx, OperatorCtx ctx2, ParsedMeta meta) =>
        {
            var t = ctx2.first.GetDType().GetName();

            if (t == "int" || t == "double" || t == "decimal" || t == "float")
            {
                var a = ctx2.first.AnyNumber(meta);
                var b = ctx2.second.AnyNumber(meta);

                if (b == 0)
                {
                    Eroro.MakeEroro("Division by zero", meta);
                    throw new Exception();
                }

                var c = a / b;
                try
                {
                    return CastUtil.CastNum(c, t);
                }
                catch (OverflowException _)
                {
                    Eroro.MakeEroro("Overflow", meta);
                    throw new Exception();
                }
            }

            Eroro.MakeEroro("Invalid operation", meta);
            throw new Exception();
        },
        ["%"] = (ref ExecCtx ctx, OperatorCtx ctx2, ParsedMeta meta) =>
        {
            var t = ctx2.first.GetDType().GetName();

            if (t == "int" || t == "double" || t == "decimal" || t == "float")
            {
                var a = ctx2.first.AnyNumber(meta);
                var b = ctx2.second.AnyNumber(meta);

                if (b == 0)
                {
                    Eroro.MakeEroro("Division by zero", meta);
                    throw new Exception();
                }

                var c = a % b;
                try
                {
                    return CastUtil.CastNum(c, t);
                }
                catch (OverflowException _)
                {
                    Eroro.MakeEroro("Overflow", meta);
                    throw new Exception();
                }
            }

            Eroro.MakeEroro("Invalid operation", meta);
            throw new Exception();
        },
        ["=="] = (ref ExecCtx ctx, OperatorCtx ctx2, ParsedMeta meta) => new DValue(ctx2.first.GetRawNonNull(meta).Equals(ctx2.second.GetRawNonNull(meta)),
            SimpleDType.BOOL),
        ["!="] = (ref ExecCtx ctx, OperatorCtx ctx2, ParsedMeta meta) => new DValue(!ctx2.first.GetRawNonNull(meta).Equals(ctx2.second.GetRawNonNull(meta)),
            SimpleDType.BOOL),
        [">="] = (ref ExecCtx ctx, OperatorCtx ctx2, ParsedMeta meta) =>
        {
            var a = ctx2.first.AnyNumber(meta);
            var b = ctx2.second.AnyNumber(meta);

            return new DValue(a >= b, SimpleDType.BOOL);
        },
        ["<="] = (ref ExecCtx ctx, OperatorCtx ctx2, ParsedMeta meta) =>
        {
            var a = ctx2.first.AnyNumber(meta);
            var b = ctx2.second.AnyNumber(meta);

            return new DValue(a <= b, SimpleDType.BOOL);
        },
        [">"] = (ref ExecCtx ctx, OperatorCtx ctx2, ParsedMeta meta) =>
        {
            var a = ctx2.first.AnyNumber(meta);
            var b = ctx2.second.AnyNumber(meta);

            return new DValue(a > b, SimpleDType.BOOL);
        },
        ["<"] = (ref ExecCtx ctx, OperatorCtx ctx2, ParsedMeta meta) =>
        {
            var a = ctx2.first.AnyNumber(meta);
            var b = ctx2.second.AnyNumber(meta);

            return new DValue(a < b, SimpleDType.BOOL);
        },
        ["&&"] = (ref ExecCtx ctx, OperatorCtx ctx2, ParsedMeta meta) =>
        {
            var a = ctx2.first.AsBool(meta);
            var b = ctx2.second.AsBool(meta);

            return new DValue(a && b, SimpleDType.BOOL);
        },
        ["||"] = (ref ExecCtx ctx, OperatorCtx ctx2, ParsedMeta meta) =>
        {
            var a = ctx2.first.AsBool(meta);
            var b = ctx2.second.AsBool(meta);

            return new DValue(a || b, SimpleDType.BOOL);
        }
    };
}
