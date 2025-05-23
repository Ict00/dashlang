namespace SaltLang.Execution.Types;

public abstract class DType
{
    public abstract bool IsCompatibleWith(DType other);
}


/*
 * callable
 * i64
 * i32
 * i16
 * i8
 * str
 * list
 * ref
 */

public class SimpleDType(string typeName) : DType
{
    public string getTypeName => typeName;
    
    public override bool IsCompatibleWith(DType other)
    {
        if (other is SimpleDType simpleType)
        {
            return simpleType.getTypeName == typeName;
        }

        return false;
    }
}

public class StructDType
{
    // TODO: Implement StructDType
}