using dash.Execution.Units;
using dash.Lexing;

namespace dash.Execution;

public interface ICallable
{
    protected bool IsCompatibleWith(List<DValue> dValues, ParsedMeta meta, ref ExecCtx ctx);

    public bool isUnary { get; set; }
    
    public ADValue Call(List<ADValue> values, ParsedMeta invoker,
        ref ExecCtx execCtx)
    {
        List<DValue> dValues = [];
        foreach (var i in values)
        {
            dValues.Add(i.AsD(invoker));
        }

        if (IsCompatibleWith(dValues, invoker, ref execCtx))
        {
            try
            {
                return DoCall(dValues, invoker, ref execCtx);
            }
            catch (StackOverflowException ex)
            {
                Eroro.MakeEroro("Stack overflow", invoker);
                throw new Exception();
            }
        }
        
        Eroro.MakeEroro("Wrong arguments given", invoker);
        throw new Exception();
    }

    public ADValue DoCall(List<DValue> dValues, ParsedMeta meta, ref ExecCtx execCtx);
}