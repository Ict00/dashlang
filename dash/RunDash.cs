using dash.Execution;
using dash.Execution.Units;
using dash.Lexing;
using dash.Parsing;

namespace dash;

public static class RunDash
{
    public static bool IsRepl = false;

    public static void Repl()
    {
        IsRepl = true;
        Console.WriteLine("Welcome to Dash REPL");
        var run = true;

        var state = new DashState();
        state.AddModule("main", new(), TokenExt.FakePMeta("..."));

        while (run)
        {
            Console.Write("> ");
            var line = Console.ReadLine() ?? "";

            switch (line)
            {
                case ":q":
                    run = false;
                    break;
                case ":r":
                    state = new();
                    state.AddModule("main", new(), TokenExt.FakePMeta("..."));
                    Console.WriteLine("Repl reset");
                    break;
                default:
                    try
                    {
                        var tokens = Lexer.Tokenize(line, "REPL");
                        var parser = new Parser("REPL", tokens, state);

                        parser.ParseAny();
                        if (parser._stack.Count > 0)
                        {
                            var expr = parser._stack.Pop();
                            var exec = new ExecCtx()
                            {
                                CallTrace = new(),
                                CurrentScope = state.Modules["main"],
                                CurrentState = state
                            };
                            try
                            {
                                var a = expr.Do(ref exec);

                                if (a is DValue val)
                                {
                                    if (!val.IsNull())
                                    {
                                        Console.WriteLine(val.Show());
                                    }
                                }
                            }
                            catch
                            {
                                /* Ignore */
                            }
                        }
                    }
                    catch { /* Ignore */}

                    break;
            }
        }
    }

    public static void FromFile(string fileName)
    {
        if (File.Exists(fileName))
        {
            var state = new DashState();
            var ext = TokenExt.FakePMeta("Main Parser");
            var tokens = Lexer.Tokenize(File.ReadAllText(fileName), fileName);
            var parser = new Parser(fileName, tokens, state);

            var parsed = parser.ParseEverything();

            var scope = new Scope();
            var e = new ExecCtx
            {
                CurrentScope = scope,
                CallTrace = new CallTrace(),
                CurrentState = state
            };

            foreach (var i in parsed)
            {
                i.Do(ref e);
            }

            state.mainModule = parser.ModuleName;

            state.AddModule(parser.ModuleName, scope, ext);

            if (state.Modules[state.mainModule].Exists("main"))
            {
                var newExec = new ExecCtx()
                {
                    CallTrace = new CallTrace(),
                    CurrentState = state,
                    CurrentScope = state.GetModule(state.mainModule, ext)
                };

                state.Modules[state.mainModule].Get("main", ext).AsCallable(ext).Call([], ext, ref newExec);
            }
        }
        else
        {
            Console.WriteLine($"File '{fileName}' not found");
        }
    }
}
