using dash.Execution.Units;

namespace dash.Etc;

public class CastUtil
{
    public static DValue CastNum(decimal c, string target)
    {
        switch (target)
        {
            case "int": return new DValue(Convert.ToInt32(c), SimpleDType.INT);
            case "double": return new DValue(Convert.ToDouble(c), SimpleDType.DOUBLE);
            case "float": return new DValue((float)c, SimpleDType.FLOAT);
            default: return new DValue(Convert.ToDecimal(c), SimpleDType.DECIMAL);
        }
    }
}