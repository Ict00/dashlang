using dash.Lexing;

namespace dash.Execution;

public class DashState
{
    public string mainModule = string.Empty;
    public Dictionary<string, Scope> Modules = [];
    public List<string> ParsedModules = [];

    public void AddModule(string name, Scope scope, ParsedMeta meta)
    {
        if (Modules.ContainsKey(name))
        {
            return;
        }

        Modules[name] = scope;
    }

    public Scope GetModule(string name, ParsedMeta meta)
    {
        if (Modules.TryGetValue(name, out var module))
        {
            return module;
        }
        else
        {
            Eroro.MakeEroro($"Module with name '{name}' not found", meta);
            throw new Exception();
        }
    }
}