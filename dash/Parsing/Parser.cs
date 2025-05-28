using dash.Etc;
using dash.Execution;
using dash.Execution.Expressions;
using dash.Execution.Units;
using dash.Lexing;
using dash.Standart.Modules;

namespace dash.Parsing;

public class Parser(string fileName, List<Token> tokens, DashState state)
{
    public Stack<IExpression> _stack = new();

    private Dictionary<string, int> priority = new()
    {
        ["+"] = 10,
        ["-"] = 10,
        ["*"] = 9,
        ["/"] = 9,
        ["%"] = 9,
        ["&&"] = 12,
        ["||"] = 12,
        ["=="] = 11,
        ["!="] = 11,
        [">="] = 11,
        ["<="] = 11,
        ["<"] = 11,
        [">"] = 11
    };

    private List<string> DefaultTypes =
    [
        "int",
        "ref",
        "float",
        "decimal",
        "double",
        "any",
        "null",
        "list",
        "str",
        "bool",
        "callable",
        "constructor",
        "dict"
    ];

    public const int defaultPriority = 10;
    public string ModuleName = "";

    private int _idx = 0;

    void Next(Token requiredBy)
    {
        if (_idx + 1 < tokens.Count)
        {
            _idx++;
        }
        else
        {
            Eroro.MakeEroro("Expected expression, got nothing", requiredBy.Meta);
            throw new Exception();
        }
    }

    Maybe<Token> CheckNext()
    {
        if (_idx + 1 < tokens.Count)
        {
            return new Just<Token>(tokens[_idx + 1]);
        }
        return new Nothing<Token>();
    }

    Token Current() => tokens[_idx];

    bool Is(Token @checked, TokenType reqType)
    {
        return @checked.TokenType == reqType;
    }

    bool Is(Token @checked, string value)
    {
        return @checked.Value.Equals(value);
    }

    bool Is(Token @checked, TokenType reqType, string value)
    {
        return @checked.TokenType == reqType && @checked.Value.Equals(value);
    }

    bool Until(TokenType tokenType) => tokenType != tokens[_idx].TokenType;
    bool Until(TokenType tokenType, string val) => !(tokenType == tokens[_idx].TokenType && val == tokens[_idx].Value);

    public void ParseRounds(bool tryOp = true)
    {
        var requester = Current();
        IExpression folded =
            new AtomExpression(new ParsedMeta(requester.Meta, requester), new DValue(null, SimpleDType.NULL));
        Next(requester);
        for (; Until(TokenType.Bracket, ")"); Next(requester))
        {
            ParseAny();
        }

        if (_stack.Count > 0)
        {
            folded = _stack.DPop(requester.Meta);
        }

        _stack.Push(new BoxExpression(new ParsedMeta(requester.Meta, requester), folded));

        TryParseExtension(tryOp);
    }

    public void ParseCBrackets()
    {
        var requester = Current();
        Next(requester);

        if (!Is(Current(), TokenType.Bracket, "{"))
        {
            _idx--;
            ParseId();
            return;
        }

        Next(requester);

        List<IExpression> expr = [];

        for (; Until(TokenType.Bracket, "}"); Next(requester))
        {
            ParseAny();
            if(_stack.Count > 0)
                expr.Add(_stack.DPop(requester.Meta));
        }

        _stack.Push(new NoCListedExpression(new ParsedMeta(requester.Meta, requester), [.. expr]));


    }

    public void ParseBrackets()
    {
        var requester = Current();
        Next(requester);
        List<IExpression> expr = [];

        for (; Until(TokenType.Bracket, "}"); Next(requester))
        {
            ParseAny();
            if(_stack.Count > 0)
                expr.Add(_stack.DPop(requester.Meta));
        }

        _stack.Push(new ListedExpression(new ParsedMeta(requester.Meta, requester), [.. expr]));
    }

    public void ParseUnary()
    {
        var requester = Current();

        if (requester.Value == ",") return;

        Next(requester);
        ParseAny(false);
        var exp = _stack.DPop(requester.Meta);
        _stack.Push(new UnaryOperator(new ParsedMeta(requester.Meta, requester), exp, requester.Value));

        TryParseExtension(true);
    }

    public void ParseBinary()
    {
        var requester = Current();



        var _a = _stack.DPop(requester.Meta);
        Next(requester);
        ParseAny();
        var _b = _stack.DPop(requester.Meta);

        BinaryOperator Rec(BinaryOperator t, int priority)
        {
            int other = GetP(t.Op);

            if (priority <= other)
            {
                if (t.A is BinaryOperator bin)
                {
                    return new BinaryOperator(t.Meta, Rec(bin, priority), t.B, t.Op);
                }
                else
                {
                    return new BinaryOperator(t.Meta, new BinaryOperator(new ParsedMeta(requester.Meta, requester),
                        _a, t.A, requester.Value), t.B, t.Op);
                }
            }
            else
            {
                return new BinaryOperator(new ParsedMeta(requester.Meta, requester),
                    _a, t, requester.Value);
            }
        }

        if (_b is BinaryOperator s)
        {
            _stack.Push(Rec(s, GetP(requester.Value)));
        }
        else
        {
            _stack.Push(new BinaryOperator(new ParsedMeta(requester.Meta, requester), _a, _b, requester.Value));
        }


        int GetP(string op)
        {
            return priority.GetValueOrDefault(op, defaultPriority);
        }
    }

    public void ParseLet()
    {
        var requester = Current();
        Next(requester);

        if (Current().TokenType == TokenType.Identifier || Current().TokenType == TokenType.Operator)
        {
            var name = Current().Value;
            DType expect = SimpleDType.ANY;

            Next(requester);
            if (Is(Current(), TokenType.Operator, ":"))
            {
                Next(requester);
                expect = ParseType();
                Next(requester);
            }


            if (Is(Current(), TokenType.Operator, "="))
            {
                Next(requester);
                ParseAny();

                var expr = _stack.DPop(requester.Meta);

                _stack.Push(new LetExpression(new ParsedMeta(requester.Meta, requester),
                    name, expr, expect));
            }
            else
            {
                Eroro.MakeEroro($"Expected '=', not '{Current().Value}'", requester.Meta);
                throw new Exception();
            }
        }
        else
        {
            Eroro.MakeEroro("Expected variable name", requester.Meta);
            throw new Exception();
        }
    }

    public IExpression ParseLine()
    {
        var requester = Current();

        for (; Until(TokenType.Eol); Next(requester))
        {
            ParseAny();
        }

        if (_stack.Count == 0)
        {
            return new VoidExpression(new ParsedMeta(requester.Meta, requester));
        }

        return _stack.DPop(requester.Meta);
    }

    public DType ParseType()
    {
        var requester = Current();

        if (Is(requester, TokenType.Identifier) || Is(requester, TokenType.Keyword) || Is(requester, TokenType.Operator))
        {
            DType type;

            if (requester.Value == "predicate")
            {
                Next(requester);
                ParseAny();
                var expr = _stack.DPop(requester.Meta);
                return new PredicateType(expr);
            }
            if (requester.Value == "!")
            {
                Next(requester);
                var a = ParseType();
                return new NotDType(a);
            }
            if (requester.Value == "any")
            {
                type = SimpleDType.ANY;
            }
            else if (DefaultTypes.Contains(requester.Value))
            {
                type = new SimpleDType(requester.Value);
            }
            else
            {
                type = new StructDType(requester.Value, []);
            }

            if (CheckNext() is Just<Token> token)
            {
                if (Is(token.Get(), TokenType.Operator, "|"))
                {
                    Next(requester);
                    Next(requester);
                    type = new EitherDType(type, ParseType());
                }
            }

            return type;
        }
        else
        {
            Eroro.MakeEroro("Expected identifier", requester.Meta);
            throw new Exception();
        }
    }

    public (string Id, DType type) ParseIdType()
    {
        (string Id, DType type) a = new();

        var requester = Current();

        if (Is(requester, TokenType.Identifier) || Is(requester, TokenType.Keyword))
        {
            a.Id = requester.Value;
            if (CheckNext() is Just<Token> next)
            {
                if (Is(next.Get(), TokenType.Operator, ":"))
                {
                    Next(requester);
                    Next(requester);
                    a.type = ParseType();
                }
                else
                {
                    a.type = SimpleDType.ANY;
                }
            }
            else
            {
                a.type = SimpleDType.ANY;
            }
        }
        else
        {
            Eroro.MakeEroro("Expected identifier", requester.Meta);
        }

        return a;
    }

    public void ParseIf()
    {
        var requester = Current();
        Next(requester);

        ParseAny();
        var condition = _stack.DPop(requester.Meta);

        Next(requester);

        if (!Is(Current(), TokenType.Keyword, "then"))
        {
            Eroro.MakeEroro($"Expected 'then', not '{Current().Value}'", requester.Meta);
            throw new Exception();
        }

        Next(requester);

        ParseAny();
        var then = _stack.DPop(requester.Meta);
        IExpression? elseExpr = null;
        if (_idx + 1 < tokens.Count)
        {
            Next(Current());
            if (Current().Value == "else" && Current().TokenType == TokenType.Keyword)
            {
                var elseP = Current();
                Next(elseP);
                ParseAny();
                elseExpr = _stack.DPop(elseP.Meta);
            }
            else
            {
                _idx--;
            }
        }

        _stack.Push(new IfExpression(new(requester.Meta, requester),
            condition, then, elseExpr));
    }

    public void ParseImport()
    {
        var requester = Current();


        Next(requester);
        if (Is(Current(), TokenType.Bracket, "["))
        {
            Next(requester);

            List<string> modulesNames = [];


            for (; Until(TokenType.Bracket, "]"); Next(requester))
            {
                if (Current().Value != ",")
                {
                    modulesNames.Add(Current().Value);
                }
            }


            // Begin parsing modules

            foreach (var i in modulesNames)
            {
                if (!state.ParsedModules.Contains(i))
                {
                    state.ParsedModules.Add(i);
                    ModuleProvider.AddModule(i, state, TokenExt.FakePMeta($"Parser of {fileName}"));
                }
            }
        }
        else
        {
            Eroro.MakeEroro($"Expected '[', got '{Current().Value}'", requester.Meta);
        }
    }

    public void ParseScoped()
    {
        var requester = Current();
        var scoped = _stack.DPop(requester.Meta);

        if (scoped is IdExpression id)
        {
            var scopeName = id.GetName;
            Next(requester);
            var funName = Current().Value;

            _stack.Push(new ScopedAccess(new ParsedMeta(requester.Meta, requester), scopeName, funName));
            TryParseExtension(true);
        }
        else
        {
            Eroro.MakeEroro($"Expected identifier, got {scoped.Meta.Origin.Value}", scoped.Meta);
            throw new Exception();
        }
    }

    public void ParseMethod()
    {
        var requester = Current();

        var ext = _stack.DPop(requester.Meta);

        Next(requester);
        ParseAny(false);
        var t = _stack.DPop(requester.Meta);
        if (t is InvokeExpression expr)
        {
            expr.Args.Insert(0, ext);
            _stack.Push(t);
        }
        else
        {
            Eroro.MakeEroro("Expected call", t.Meta);
        }
    }

    public void TryParseExtension(bool tryOp)
    {
        if (CheckNext() is Just<Token> t)
        {
            if (Is(t.Get(), TokenType.Operator, "."))
            {

                Next(t.Get());
                Next(t.Get());

                if (Current().TokenType == TokenType.Identifier || Current().TokenType == TokenType.Keyword)
                {
                    var _struct = _stack.DPop(t.Get().Meta);
                    var field = Current().Value;

                    _stack.Push(new FieldExpression(new ParsedMeta(t.Get().Meta, t.Get()),
                        _struct, field));
                }
                else
                {
                    Eroro.MakeEroro("Expected identifier", t.Get().Meta);
                }

                TryParseExtension(tryOp);
            }

            else if (Is(t.Get(), TokenType.Bracket, "["))
            {
                Next(t.Get());
                Next(t.Get());

                for (; Until(TokenType.Bracket, "]"); Next(t.Get()))
                {
                    ParseAny();
                }

                var xpr = _stack.DPop(t.Get().Meta);
                var lst = _stack.DPop(t.Get().Meta);

                _stack.Push(new IndexExpression(new ParsedMeta(t.Get().Meta, t.Get()), lst, xpr));

                TryParseExtension(tryOp);
            }

            else if (Is(t.Get(), TokenType.Bracket, "("))
            {
                Next(t.Get());

                List<IExpression> expressions = [];

                var before = _stack.Count;

                Next(t.Get());
                for (; Until(TokenType.Bracket, ")"); Next(t.Get()))
                {
                    ParseAny();
                    if (Is(Current(), TokenType.Operator, ","))
                    {
                        expressions.Add(_stack.DPop(t.Get().Meta));
                    }
                }

                if (before != _stack.Count)
                {
                    expressions.Add(_stack.DPop(t.Get().Meta));
                }


                var callable = _stack.DPop(t.Get().Meta);
                
                _stack.Push(new InvokeExpression(new ParsedMeta(t.Get().Meta, t.Get()), callable, expressions));

                TryParseExtension(tryOp);
            }
            else if (Is(t.Get(), TokenType.Operator, "->"))
            {
                Next(t.Get());
                ParseMethod();
            }
            else if (Is(t.Get(), TokenType.Operator) && tryOp && t.Get().Value != "=" && t.Get().Value != "::")
            {
                if (t.Get().Value != ",")
                {
                    Next(t.Get());
                    ParseBinary();
                }
            }
            else
            {
                if (Is(t.Get(), TokenType.Keyword))
                {
                    switch (t.Get().Value)
                    {
                        case "is":
                            Next(t.Get());
                            ParseIs();
                            break;
                        case "as":
                            Next(t.Get());
                            ParseAs();
                            break;
                    }
                }

                if (Is(t.Get(), TokenType.Operator, "=") && tryOp)
                {
                    Next(t.Get());
                    ParseAssign();
                }

                if (Is(t.Get(), TokenType.Operator, "::"))
                {
                    Next(t.Get());
                    ParseScoped();
                }
            }
        }
    }

    public void ParseId(bool tryOp = true)
    {
        var requester = Current();

        _stack.Push(new IdExpression(new ParsedMeta(requester.Meta, requester), requester.Value));

        TryParseExtension(tryOp);
    }

    public void ParseStr(bool tryOp = true)
    {
        var requester = Current();

        _stack.Push(new AtomExpression(new ParsedMeta(requester.Meta, requester),
            new DValue(requester.Value, SimpleDType.STR)));

        TryParseExtension(tryOp);
    }

    public void ParseAssign()
    {
        var requester = Current();
        var a = _stack.DPop(requester.Meta);
        Next(requester);
        ParseAny();
        var b = _stack.DPop(requester.Meta);
        _stack.Push(new AssignExpression(new ParsedMeta(requester.Meta, requester),
            a, b));
    }

    public void ParseList(bool tryOp = true)
    {
        var requester = Current();
        List<IExpression> expressions = [];

        int before = _stack.Count;

        Next(requester);
        for (; Until(TokenType.Bracket, "]"); Next(requester))
        {
            ParseAny();
            if (Is(Current(), TokenType.Operator, ","))
            {
                expressions.Add(_stack.DPop(requester.Meta));
            }
        }

        if (_stack.Count != before)
        {
            expressions.Add(_stack.DPop(requester.Meta));
        }

        _stack.Push(new ListExpression(new ParsedMeta(requester.Meta, requester), expressions));

        TryParseExtension(tryOp);
    }

    public void ParseStruct()
    {
        var requester = Current();

        Next(requester);
        if (Is(Current(), TokenType.Identifier))
        {
            string name = Current().Value;
            Next(requester);
            if (Is(Current(), TokenType.Bracket, "("))
            {
                Next(Current());
                List<(string Id, DType type)> mask = [];
                List<string> inherits = [];

                for (; Until(TokenType.Bracket, ")"); Next(requester))
                {
                    if (!Is(Current(), TokenType.Operator, ","))
                    {
                        mask.Add(ParseIdType());
                    }
                }

                if (CheckNext() is Just<Token> check)
                {
                    if (Is(check.Get(), TokenType.Operator, ":"))
                    {
                        Next(requester);
                        Next(requester);

                        void ParseInherit()
                        {
                            if (Is(Current(), TokenType.Identifier))
                            {
                                inherits.Add(Current().Value);
                                if (CheckNext() is Just<Token> next)
                                {
                                    if (Is(next.Get(), TokenType.Operator, ","))
                                    {
                                        Next(requester);
                                        Next(requester);
                                        ParseInherit();
                                    }
                                }
                            }
                            else
                            {
                                Eroro.MakeEroro("Expected identifier", requester.Meta);
                            }
                        }

                        ParseInherit();
                    }
                }

                _stack.Push(new StructDecl(name, mask, inherits, new ParsedMeta(requester.Meta, requester)));
            }
            else
            {
                Eroro.MakeEroro($"Expected '(', got '{Current().Value}'", requester.Meta);
            }
        }
        else
        {
            Eroro.MakeEroro("Expected identifier", requester.Meta);
        }
    }

    public void ParseAs()
    {
        var requester = Current();
        Next(requester);
        var source = _stack.DPop(requester.Meta);
        var type = ParseType();
        _stack.Push(new AsOperator(new ParsedMeta(requester.Meta, requester),
            source, type));
        TryParseExtension(true);
    }

    public void ParseIs()
    {
        var requester = Current();
        Next(requester);
        var source = _stack.DPop(requester.Meta);
        var type = ParseType();
        _stack.Push(new IsOperator(new ParsedMeta(requester.Meta, requester),
            source, type));
        TryParseExtension(true);
    }

    public void ParseLambda()
    {
        var requester = Current();
        Next(requester);

        if (Is(Current(), TokenType.Bracket, "("))
        {
            DType type = SimpleDType.ANY;
            List<(string Id, DType type)> mask = [];
            Next(requester);
            for (; Until(TokenType.Bracket, ")"); Next(requester))
            {
                if (!Is(Current(), TokenType.Operator, ","))
                {
                    mask.Add(ParseIdType());
                }
            }
            Next(requester);
            if (Is(Current(), TokenType.Operator, ":"))
            {
                Next(requester);
                type = ParseType();
                Next(requester);
            }

            if (Is(Current(), TokenType.Operator, "="))
            {
                Next(requester);
                ParseAny();
                var expression = _stack.DPop(requester.Meta);

                _stack.Push(new LambdaExpression(mask, type, expression, new ParsedMeta(requester.Meta, requester)));
            }
            else
            {
                Eroro.MakeEroro($"Expected '=', got '{Current().Value}'", requester.Meta);
                throw new Exception();
            }
        }
        else
        {
            ParseAny();
            var expr = _stack.DPop(requester.Meta);

            _stack.Push(new LambdaExpression([], SimpleDType.ANY, expr, new ParsedMeta(requester.Meta, requester)));
        }
        TryParseExtension(true);
    }

    public void ParseFun()
    {
        var requester = Current();
        Next(requester);
        if (Is(Current(), TokenType.Operator) ||
            Is(Current(), TokenType.Identifier))
        {
            var name = Current().Value;
            DType type = SimpleDType.ANY;
            List<(string Id, DType type)> mask = [];
            Next(requester);

            if (Is(Current(), TokenType.Bracket, "("))
            {
                Next(requester);
                for (; Until(TokenType.Bracket, ")"); Next(requester))
                {
                    if (!Is(Current(), TokenType.Operator, ","))
                    {
                        mask.Add(ParseIdType());
                    }
                }
                Next(requester);
                if (Is(Current(), TokenType.Operator, ":"))
                {
                    Next(requester);
                    type = ParseType();
                    Next(requester);
                }

                if (Is(Current(), TokenType.Operator, "="))
                {
                    Next(requester);
                    ParseAny();
                    var expression = _stack.DPop(requester.Meta);

                    _stack.Push(new FunctionExpression(mask, name, type, expression, new ParsedMeta(requester.Meta, requester), ModuleName));
                }
                else
                {
                    Eroro.MakeEroro($"Expected '=', got '{Current().Value}'", requester.Meta);
                    throw new Exception();
                }
            }
            else
            {
                Eroro.MakeEroro($"Expected '(', got '{Current().Value}'", requester.Meta);
            }
        }
        else
        {
            Eroro.MakeEroro("Expected operator or identifier", requester.Meta);
        }
    }

    public void ParseNumber(bool tryOp)
    {
        var requester = Current();

        switch (requester.TokenType)
        {
            case TokenType.Number:
                int val = int.Parse(requester.Value);
                _stack.Push(new AtomExpression(new ParsedMeta(requester.Meta, requester), new DValue(val, SimpleDType.INT)));
                break;
            default:
                string transformed = requester.Value.Replace('.', ',');
                switch (requester.TokenType)
                {
                    case TokenType.Decimal:
                        decimal dec = decimal.Parse(transformed);
                        _stack.Push(new AtomExpression(new ParsedMeta(requester.Meta, requester), new DValue(dec, SimpleDType.DECIMAL)));
                        break;
                    case TokenType.Float:
                        float flo = float.Parse(transformed);
                        _stack.Push(new AtomExpression(new ParsedMeta(requester.Meta, requester), new DValue(flo, SimpleDType.FLOAT)));
                        break;
                    case TokenType.Double:
                        double dou = double.Parse(transformed);
                        _stack.Push(new AtomExpression(new ParsedMeta(requester.Meta, requester), new DValue(dou, SimpleDType.DOUBLE)));
                        break;
                }
                break;
        }

        TryParseExtension(tryOp);
    }

    public List<IExpression> ParseEverything()
    {
        List<IExpression> lst = [];

        while (_idx < tokens.Count)
        {
            ParseAny();
            _idx++;
        }

        while (_stack.Count > 0)
        {
            lst.Add(_stack.Pop());
        }

        lst.Reverse();

        return lst;
    }

    public void ParseWhile()
    {
        var requester = Current();
        Next(requester);
        ParseAny();
        var cond = _stack.DPop(requester.Meta);
        Next(requester);
        ParseAny();
        var body = _stack.DPop(requester.Meta);

        _stack.Push(new WhileLoop(new ParsedMeta(requester.Meta, requester),
            cond, body));
    }

    public void ParseForeach()
    {
        var requester = Current();
        Next(requester);
        if (!Is(Current(), TokenType.Identifier))
        {
            Eroro.MakeEroro("Identifier expected", Current().Meta);
        }

        var varName = Current().Value;
        Next(requester);
        if (!Is(Current(), TokenType.Keyword, "in"))
        {
            Eroro.MakeEroro($"Expected 'in', got '{Current().Value}'", Current().Meta);
        }
        Next(requester);
        ParseAny();
        var list = _stack.DPop(requester.Meta);
        Next(requester);
        ParseAny();
        var body = _stack.DPop(requester.Meta);

        _stack.Push(new ForeachLoop(new ParsedMeta(requester.Meta, requester), list, varName, body));
    }

    public void ParseFor()
    {
        var requester = Current();
        Next(requester);
        if (!Is(Current(), TokenType.Bracket, "("))
        {
            Eroro.MakeEroro("Expected 'for' condition expression, got nothing", requester.Meta);
        }
        Next(requester);
        var before = ParseLine();
        Next(requester);
        var condition = ParseLine();
        Next(requester);
        for (; Until(TokenType.Bracket, ")"); Next(requester))
        {
            ParseAny();
        }

        var after = _stack.Count == 0
            ? new VoidExpression(new ParsedMeta(requester.Meta, requester))
            : _stack.DPop(requester.Meta);

        Next(requester);
        ParseAny();
        var body = _stack.DPop(requester.Meta);

        _stack.Push(new ForLoop(new ParsedMeta(requester.Meta, requester), before, condition, after, body));
    }

    public void ParseBreak()
    {
        var requester = Current();
        _stack.Push(new Break(new ParsedMeta(requester.Meta, requester)));
    }

    public void ParseDict()
    {
        var requester = Current();
        Next(requester);
        if (!Is(Current(), TokenType.Bracket, "{"))
        {
            Eroro.MakeEroro($"Expected '{'{'}', got '{Current().Value}'", requester.Meta);
        }
        Next(requester);

        List<(string key, IExpression val)> vals = [];

        for (; Until(TokenType.Bracket, "}"); Next(requester))
        {
            if (Current().Value != ",")
            {
                var key = Current().Value;
                Next(requester);
                if (!Is(Current(), TokenType.Operator, ":"))
                {
                    Eroro.MakeEroro($"Expected ':', got '{Current().Value}'", requester.Meta);
                }
                Next(requester);
                ParseAny();
                var val = _stack.DPop(requester.Meta);
                vals.Add((key, val));
            }
        }

        _stack.Push(new DictExpression(new ParsedMeta(requester.Meta, requester), vals));
    }

    public void ParseRef()
    {
        var requester = Current();
        Next(requester);
        ParseAny(false);
        var exp = _stack.DPop(requester.Meta);
        if (exp is IAccess access)
        {
            _stack.Push(new RefExpression(new ParsedMeta(requester.Meta, requester), access));
        }
        else
        {
            Eroro.MakeEroro("Identifier is non-accessible", requester.Meta);
        }
    }

    public void ParseExec()
    {
        var requester = Current();
        Next(requester);
        ParseAny(false);
        var exp = _stack.DPop(requester.Meta);
        _stack.Push(new ExecExpression(new ParsedMeta(requester.Meta, requester), exp));
    }

    public void ParseContinue()
    {
        var requester = Current();
        _stack.Push(new Continue(new ParsedMeta(requester.Meta, requester)));
    }

    public void ParseReturn()
    {
        var requester = Current();
        Next(requester);
        ParseAny();
        var toReturn = _stack.DPop(requester.Meta);
        _stack.Push(new ReturnExpression(new ParsedMeta(requester.Meta, requester), toReturn));
    }

    public void ParseAny(bool tryOp = true)
    {
        switch (Current().TokenType)
        {
            case TokenType.Eol:
                break;
            case TokenType.Bracket:
                switch (Current().Value)
                {
                    case "{":
                        ParseBrackets();
                        break;
                    case "(":
                        ParseRounds(tryOp);
                        break;
                    case "[":
                        ParseList(tryOp);
                        break;
                }
                break;
            case TokenType.String:
                ParseStr(tryOp);
                break;
            case TokenType.Lambda:
                ParseLambda();
                break;
            case TokenType.Identifier:
                ParseId(tryOp);
                break;
            case TokenType.Number:
                ParseNumber(tryOp);
                break;
            case TokenType.Float: goto case TokenType.Number;
            case TokenType.Decimal: goto case TokenType.Number;
            case TokenType.Double: goto case TokenType.Number;
            case TokenType.Keyword:
                switch (Current().Value)
                {
                    case "let":
                        ParseLet();
                        break;
                    case "dict":
                        ParseDict();
                        break;
                    case "ref":
                        ParseRef();
                        break;
                    case "break":
                        ParseBreak();
                        break;
                    case "continue":
                        ParseContinue();
                        break;
                    case "return":
                        ParseReturn();
                        break;
                    case "for":
                        ParseFor();
                        break;
                    case "while":
                        ParseWhile();
                        break;
                    case "foreach":
                        ParseForeach();
                        break;
                    case "if":
                        ParseIf();
                        break;
                    case "module":
                        Next(Current());
                        ModuleName = Current().Value;
                        break;
                    case "struct":
                        ParseStruct();
                        break;
                    case "import":
                        ParseImport();
                        break;
                    case "exec":
                        ParseExec();
                        break;
                    case "fun":
                        ParseFun();
                        break;
                    case "this":
                        ParseCBrackets();
                        break;
                    case "true":
                        _stack.Push(new AtomExpression(new(Current().Meta, Current()),
                            new DValue(true, SimpleDType.BOOL)));
                        break;
                    case "null":
                        _stack.Push(new AtomExpression(new(Current().Meta, Current()),
                            new DValue(null, SimpleDType.NULL)));
                        break;
                    case "_":
                        _stack.Push(new VoidExpression(new ParsedMeta(Current().Meta, Current())));
                        break;
                    case "false":
                        _stack.Push(new AtomExpression(new(Current().Meta, Current()),
                            new DValue(false, SimpleDType.BOOL)));
                        break;
                }
                break;
            case TokenType.Operator:
                if (Current().Value == "=")
                {
                    ParseAssign();
                    break;
                }
                ParseUnary();
                break;
        }
    }
}
