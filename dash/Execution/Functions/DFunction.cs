using dash.Execution.Expressions;
using dash.Execution.Units;
using dash.Lexing;

namespace dash.Execution;

public class DFunction(Dictionary<string, DType> mask, DType returned, IExpression execute,
    Scope related) : ICallable
{
    public IExpression Execute => execute;
    public Dictionary<string, DType> Mask => mask;
    public DType Returned => returned;
    public Scope Related => related;
    
    public bool IsCompatibleWith(List<DValue> dValues, ParsedMeta meta, ref ExecCtx ctx)
    {
        var masked = mask.Values.ToList();

        if (masked.Count == dValues.Count)
        {
            for (int i = 0; i < masked.Count; i++)
            {
                if (masked[i] is PredicateType t)
                {
                    var res = t.Check(ref ctx, dValues[i]);
                    if(!res) return false;
                }
                
                if (!masked[i].IsCompatibleWith(dValues[i].GetDType()))
                {
                    return false;
                }
            }

            return true;
        }
        Eroro.MakeEroro("Argument count mismatch", meta);
        throw new Exception();
    }

    public bool isUnary { get; set; } = mask.Count == 1;

    public ADValue DoCall(List<DValue> dValues, ParsedMeta meta, ref ExecCtx execCtx)
    {
        var childScope = related.MakeScope();
        childScope.Set("self", new DValue(this, SimpleDType.CALLABLE));
        
        var trace = new CallTrace();
        
        trace.NewCall(Call.FUNCTION);

        var newExec = new ExecCtx()
        {
            CallTrace = trace,
            CurrentState = execCtx.CurrentState,
            CurrentScope = childScope
        };

        for (int i = 0; i < dValues.Count; i++)
        {
            childScope.Let(mask.Keys.ToList()[i], dValues[i], meta);
        }
        
        var result = execute.Do(ref newExec);
        
        trace.EndCall();

        if (result is DReturn dReturn)
        {
            result = dReturn.Get();
        }

        if (returned.IsCompatibleWith(result.AsD(meta).GetDType()))
        {
            return result;
        }
        
        Eroro.MakeEroro("Returned wrong type", meta);
        throw new Exception();
    }
}