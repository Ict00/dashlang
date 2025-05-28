using dash.Execution.Expressions;
using dash.Execution.Units;
using dash.Lexing;

namespace dash.Execution;

public class RefObj(IAccess access, ExecCtx ctx)
{
    private ExecCtx _ctx = ctx;

    public void Set(DValue value)
    {
        access.Set(value, ref _ctx);
    }

    public DValue Get()
    {
        return access.Get(ref _ctx);
    }
}