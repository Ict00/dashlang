using dash.Execution.Units;
using dash.Lexing;

namespace dash.Execution;

public interface ICallable
{
    protected bool IsCompatibleWith(List<DValue> dValues);
    
    public ADValue Call(List<ADValue> values, ParsedMeta invoker)
    {
        List<DValue> dValues = [];
        foreach (var i in values)
        {
            dValues.Add(i.AsD(invoker));
        }

        if (IsCompatibleWith(dValues))
        {
            return DoCall(dValues, invoker);
        }
        
        Eroro.MakeEroro("Wrong arguments given", invoker);
        throw new Exception();
    }

    public ADValue DoCall(List<DValue> dValues, ParsedMeta meta);
}