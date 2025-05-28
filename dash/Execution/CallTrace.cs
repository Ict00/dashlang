namespace dash.Execution;

public class CallTrace
{
    private List<Call> _calls = [];

    public void NewCall(Call call)
    {
        _calls.Add(call);
    }

    public void EndCall()
    {
        _calls.RemoveAt(_calls.Count-1);
    }

    public bool IsIn(Call callType)
    {
        for (int i = _calls.Count - 1; i >= 0; i--)
        {
            if (_calls[i] == callType)
            {
                return true;
            }
        }

        return false;
    }
}

public enum Call
{
    EXPR,
    FUNCTION,
    LOOP
}