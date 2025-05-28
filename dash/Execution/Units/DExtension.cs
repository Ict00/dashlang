using dash.Lexing;

namespace dash.Execution.Units;

public static class DExtension
{
    public static DValue AsD(this ADValue adValue, ParsedMeta meta)
    {
        if (adValue is DValue dValue)
        {
            return dValue;
        }
        
        Eroro.MakeEroro("Tried to use non-value as value", meta);
        throw new Exception();
    }
}