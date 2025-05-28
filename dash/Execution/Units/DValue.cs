using dash.Execution.Structures;
using dash.Execution.Units;
using dash.Lexing;

namespace dash.Execution.Units;

public abstract class ADValue();

public class DBreak : ADValue;

public class DContinue : ADValue;

public class DReturn(ADValue ret) : ADValue
{
    public ADValue Get() => ret;
}

public class DValue(object? raw, DType type) : ADValue
{
    public DType GetDType() => type;
    public bool IsNull() => raw == null;

    public string Show()
    {
        if (raw == null) return "null";
        
        if (raw is List<DValue> isList)
        {
            string built = "[";
            bool has = false;

            foreach (var i in isList)
            {
                has = true;
                built += $"{i.Show()}, ";
            }
            if (has)
                built = built.Substring(0, built.Length - 2);
            built += "]";
            return built;
        }

        if (raw is Dictionary<string, DValue> isDict)
        {
            string built = "{";
            bool has = false;

            foreach (var i in isDict)
            {
                has = true;
                built += $" \"{i.Key}\": {i.Value.Show()}, ";
            }
            if (has)
                built = built.Substring(0, built.Length - 2);
            built += "}";

            return built;
        }

        if (raw is ICallable)
        {
            return type.GetName() == "callable" ? "<callable>" : "<constructor>";
        }

        if (raw is Struct isStruct)
        {
            string built = $"{isStruct.Name} {'{'}";
            bool has = false;

            foreach (var i in isStruct._values)
            {
                has = true;
                built += $"{i.Key}: {i.Value.Show()}, ";
            }

            if (has)
                built = built.Substring(0, built.Length - 2);
            built += "}";
            return built;
        }

        return raw.ToString() ?? "null";
    }

    public Dictionary<string, DValue> AsDict(ParsedMeta meta)
    {
        if (raw is Dictionary<string, DValue> dict)
        {
            return dict;
        }
        Eroro.MakeEroro($"Expected 'refobj', got '{type.GetName()}'", meta);
        throw new Exception();
    }

    public RefObj AsRefObj(ParsedMeta meta)
    {
        if (raw is RefObj obj)
        {
            return obj;
        }
        else
        {
            Eroro.MakeEroro($"Expected 'refobj', got '{type.GetName()}'", meta);
            throw new Exception();
        }
    }

    public object GetRawNonNull(ParsedMeta meta)
    {
        if (raw == null)
        {
            Eroro.MakeEroro("Is null", meta);
            throw new Exception();
        }
        return raw;
    }

    public decimal AnyNumber(ParsedMeta meta)
    {
        List<string> numberTypes = ["int", "decimal", "double", "float"];


        if (!numberTypes.Contains(type.GetName()))
        {
            Eroro.MakeEroro($"Expected number, got '{type.GetName()}'", meta);
            throw new Exception();
        }

        return Convert.ToDecimal(GetRawNonNull(meta));
    }

    public string AsString() => (raw ?? "null").ToString() ?? "null";

    public bool AsBool(ParsedMeta meta)
    {
        if (raw is bool ret)
        {
            return ret;
        }
        Eroro.MakeEroro($"Expected 'bool', got '{type.GetName()}'", meta);
        throw new Exception();
    }

    public ICallable AsCallable(ParsedMeta meta, string e = "callable")
    {
        if (raw is ICallable ret)
        {
            return ret;
        }

        Eroro.MakeEroro($"Expected '{e}', got '{type.GetName()}'", meta);
        throw new Exception();
    }

    public Struct AsStruct(ParsedMeta meta)
    {
        if (raw is Struct ret)
        {
            return ret;
        }
        Eroro.MakeEroro($"Expected struct, got '{type.GetName()}'", meta);
        throw new Exception();
    }

    public ICallable AsConstructor(ParsedMeta meta) => AsCallable(meta, "constructor");

    public List<DValue> AsList(ParsedMeta meta)
    {
        if (raw is List<DValue> ret)
        {
            return ret;
        }

        if (raw is Dictionary<string, DValue> dict)
        {
            List<DValue> dValues = [];

            foreach (var i in dict)
            {
                dValues.Add(new DValue(new Dictionary<string, DValue>()
                {
                    ["key"] = new DValue(i.Key, SimpleDType.STR),
                    ["value"] = i.Value
                }, SimpleDType.DICT));
            }

            return dValues;
        }

        if (raw is string str)
        {
            List<DValue> val = [];
            foreach (var i in str)
            {
                val.Add(new DValue($"{i}", SimpleDType.STR));
            }

            return val;
        }

        Eroro.MakeEroro($"Expected 'list', got '{type.GetName()}'", meta);
        throw new Exception();
    }

    public float AsFloat(ParsedMeta meta)
    {
        if (raw is float ret)
        {
            return ret;
        }
        Eroro.MakeEroro($"Expected 'float', got '{type.GetName()}'", meta);
        throw new Exception();
    }

    public double AsDouble(ParsedMeta meta)
    {
        if (raw is double ret)
        {
            return ret;
        }
        Eroro.MakeEroro($"Expected 'double', got '{type.GetName()}'", meta);
        throw new Exception();
    }

    public decimal AsDecimal(ParsedMeta meta)
    {
        if (raw is decimal ret)
        {
            return ret;
        }
        Eroro.MakeEroro($"Expected 'decimal', got '{type.GetName()}'", meta);
        throw new Exception();
    }

    public int AsInt(ParsedMeta meta)
    {
        if (raw is int ret)
        {
            return ret;
        }
        Eroro.MakeEroro($"Expected 'int', got '{type.GetName()}'", meta);
        throw new Exception();
    }

    // TODO: Implement for ref and callable
}
