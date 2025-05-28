namespace dash.Execution;

public class ExecCtx
{
    public Scope CurrentScope { get; set; } = null!;
    public CallTrace CallTrace { get; set; } = null!;
    public DashState CurrentState { get; set; } = null!;

    public ExecCtx Child()
    {
        return new ExecCtx
        {
            CallTrace = CallTrace,
            CurrentState = CurrentState,
            CurrentScope = CurrentScope.MakeScope()
        };
    }
}