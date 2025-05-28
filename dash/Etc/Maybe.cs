using dash.Lexing;


public abstract class Maybe<_>;

public class Just<T>(T obj) : Maybe<T>
{
    public T Get() => obj;
}

public class Nothing<T>(string? error = null) : Maybe<T>
{
    public string? GetError() => error;
}

