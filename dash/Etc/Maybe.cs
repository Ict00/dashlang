using SaltLang.Lexing;

namespace SaltLang;

public abstract class Maybe<T> { }

public class Just<T>(T obj) : Maybe<T>
{
    public T Get() => obj;
}

public class Nothing<T>(string? error = null) : Maybe<T>
{
    public string? GetError() => error;
}

