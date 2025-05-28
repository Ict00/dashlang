using dash;
using dash.Lexing;


public static class Eroro
{
    public static void MakeEroro(string message, Meta meta)
    {
        Console.WriteLine($"\x1b[1mDash eroro\x1b[0m\n* {message}");
        Console.WriteLine($"\x1b[1m{meta.File}\x1b[0m\n{meta.Line} | \x1b[1;15;38;5;196m{meta.WholeLine}\x1b[0m");
        if(!RunDash.IsRepl)
            Environment.Exit(0);
    }

    public static void MakeEroro(string message, ParsedMeta meta)
    {
        Console.WriteLine($"\x1b[1mDash eroro\x1b[0m\n* {message}");
        Console.WriteLine($"\x1b[1m{meta.Meta.File}\x1b[0m\n{meta.Meta.Line} | \x1b[1;15;38;5;196m{meta.Meta.WholeLine}\x1b[0m");
        if(!RunDash.IsRepl)
            Environment.Exit(0);
    }
}