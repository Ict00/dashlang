using dash.Etc;
using dash.Execution;
using dash.Execution.Units;
using dash.Lexing;
using dash.Parsing;

namespace dash.Standart.Modules;

public static class ModuleProvider
{
    public static void AddModule(string name, DashState currentState, ParsedMeta meta)
    {
        if (StandartModules.fun.ContainsKey(name))
        {
            var scope = new Scope();
            foreach (var i in StandartModules.fun[name])
            {
                scope.Set(i.name, new DValue(i.callable, SimpleDType.CALLABLE));
            }

            currentState.AddModule(name, scope,meta);
        }
        else
        {
            if (File.Exists(name))
            {
                var tokens = Lexer.Tokenize(File.ReadAllText(name), name);
                
                var parser = new Parser(name, tokens, currentState);

                var scope = new Scope();
                var e = new ExecCtx
                {
                    CurrentScope = scope,
                    CallTrace = new CallTrace(),
                    CurrentState = currentState
                };
                
                var expressions = parser.ParseEverything();
                
                foreach (var i in expressions)
                {
                    i.Do(ref e);
                }
                
                currentState.AddModule(parser.ModuleName, scope, meta);
            }
            else
            {
                Eroro.MakeEroro($"File with path '{name}' not found", meta);
                throw new Exception();
            }
        }
    }
}