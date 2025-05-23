using dash.Lexing;

namespace dash.Execution;

public class DashState
{
    public string mainModule = string.Empty;
    public Dictionary<string, Scope> Modules = [];

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