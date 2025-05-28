using dash.Execution.Expressions;

namespace dash.Execution.Units;

public abstract class DType
{
    public abstract bool IsCompatibleWith(DType other);
    public abstract string GetName();
}

public class PredicateType(IExpression checker) : DType
{
    public override string GetName() => "<predicate>";

    public override bool IsCompatibleWith(DType other)
    {
        return true;
    }

    public bool Check(ref ExecCtx ctx, DValue value)
    {
        var child = ctx.Child();
        child.CurrentScope.Set("self", value);
        
        var result = checker.Do(ref child);
        return result.AsD(checker.Meta).AsBool(checker.Meta);
    }
}


/*
 * callable
 * int
 * double
 * float
 * decimal
 * str
 * list
 * ref
 */

public class AnyDType : DType
{
    public override bool IsCompatibleWith(DType _) => true;
    public override string GetName() => "any";
}

public class NotDType(DType a) : DType
{
    public override bool IsCompatibleWith(DType other) => !a.IsCompatibleWith(other);

    public override string GetName() => $"!{a.GetName()}";
}

public class AndDType(DType a, DType b) : DType
{
    public override bool IsCompatibleWith(DType other) => a.IsCompatibleWith(other) && b.IsCompatibleWith(other);

    public override string GetName() => $"{a.GetName()} & {b.GetName()}";
}

public class EitherDType(DType a, DType b) : DType
{
    public override bool IsCompatibleWith(DType other) => a.IsCompatibleWith(other) || b.IsCompatibleWith(other);

    public override string GetName() => $"{a.GetName()} | {b.GetName()}";
}

public class SimpleDType(string typeName) : DType
{
    public static SimpleDType REF         = new("ref");
    public static SimpleDType DICT        = new("dict");
    public static SimpleDType NULL        = new("null");
    public static SimpleDType LIST        = new("list");
    public static SimpleDType BOOL        = new("bool");
    public static SimpleDType FLOAT       = new("float");
    public static SimpleDType DOUBLE      = new("double");
    public static SimpleDType DECIMAL     = new("decimal");
    public static SimpleDType CALLABLE    = new("callable");
    public static SimpleDType CONSTRUCTOR = new("constructor");
    public static SimpleDType STR         = new("str");
    public static SimpleDType INT         = new("int");
    public static AnyDType    ANY         = new();
    
    public override bool IsCompatibleWith(DType other)
    {
        
        if (other is SimpleDType simpleType)
        {
            return GetName() == other.GetName();
        }

        return false;
    }

    public override string GetName()
    {
        return typeName;
    }
}

public class StructDType(string structName, List<string> inheritedFrom) : DType
{
    public List<string> GetInherited => inheritedFrom;
    
    public override bool IsCompatibleWith(DType other)
    {
        if (other is StructDType structType)
        {
            if (other.GetName() == structName)
            {
                return true;
            }
            foreach (var inherited in structType.GetInherited)
            {
                if (GetName() == inherited)
                {
                    return true;
                }
            }

            return false;
        }
        return false;
    }

    public override string GetName()
    {
        return structName;
    }
}