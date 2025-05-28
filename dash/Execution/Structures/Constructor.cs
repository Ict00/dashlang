using dash.Execution.Units;
using dash.Lexing;

namespace dash.Execution.Structures;

public class Constructor(List<(string Id, DType type)> mask, string name, List<string> inheritedFrom) : ICallable
{
    public bool isUnary { get; set; } = mask.Count == 1;
    
    public bool IsCompatibleWith(List<DValue> dValues, ParsedMeta meta, ref ExecCtx ctx)
    {

        if (mask.Count == dValues.Count)
        {
            for (int i = 0; i < mask.Count; i++)
            {
                if (mask[i].type is PredicateType t)
                {
                    var res = t.Check(ref ctx, dValues[i]);
                    if(!res) return false;
                }
                
                if (!mask[i].type.IsCompatibleWith(dValues[i].GetDType()))
                {
                    return false;
                }
            }

            return true;
        }
        Eroro.MakeEroro("Argument count mismatch", meta);
        throw new Exception();
    }

    public ADValue DoCall(List<DValue> dValues, ParsedMeta meta, ref ExecCtx execCtx)
    {
        Dictionary<string, DValue> fields = [];
        Dictionary<string, DType> types = [];

        for (int i = 0; i < dValues.Count; i++)
        {
            fields[mask[i].Id] = dValues[i];
        }

        for (int i = 0; i < mask.Count; i++)
        {
            types[mask[i].Id] = mask[i].type;
        }
        

        var structure = new Struct()
        {
            _values = fields,
            _types = types,
            Name = name
        };
        
        return new DValue(structure, new StructDType(name, inheritedFrom));
    }
}