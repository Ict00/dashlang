using dash.Execution.Units;
using dash.Lexing;

namespace dash.Execution;

public delegate ADValue ToDoCall(List<DValue> dValues, ParsedMeta meta, ref ExecCtx execCtx);

public class BDFunction(ToDoCall toDoCall, int argCount) : ICallable
{
    public bool isUnary { get; set; } = false;
    
    public bool IsCompatibleWith(List<DValue> dValues, ParsedMeta meta, ref ExecCtx ctx)
    {
        if (argCount == -1) return true;
        return argCount == dValues.Count;
    }

    public ADValue DoCall(List<DValue> dValues, ParsedMeta meta, ref ExecCtx execCtx) => toDoCall(dValues, meta, ref execCtx);
}