using dash.Execution.Units;
using dash.Lexing;

namespace dash.Execution.Structures;

public class Struct
{
    public Dictionary<string, DValue> _values { get; set; } = [];
    public Dictionary<string, DType> _types { get; set; } = [];
    public string Name = "";

    public virtual DValue Get(string name, ParsedMeta meta)
    {
        if (_values.TryGetValue(name, out var value))
        {
            return value;
        }
        Eroro.MakeEroro($"Field '{name}' doesn't exist", meta);
        throw new Exception();
    }

    public virtual void Set(string name, DValue value, ParsedMeta meta)
    {
        if (_values.ContainsKey(name))
        {
            if(_types[name].IsCompatibleWith(value.GetDType()))
            {
                _values[name] = value;
                return;
            }
            
            Eroro.MakeEroro($"Field '{name}' is of type '{_types[name].GetName()}' but '{value.GetDType().GetName()}' was given", meta);
            throw new Exception();
        }

        Eroro.MakeEroro($"Field '{name}' doesn't exist", meta);
        throw new Exception();
    }
}