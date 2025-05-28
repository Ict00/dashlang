using System.Net;
using dash;


if (args.Length > 0)
{
    if (args[0] == "repl")
    {
        RunDash.Repl();
    }
    else
    {
        RunDash.FromFile(args[0]);
    }
}
else
{
    Console.WriteLine("Usage: | dash [file]\n       | dash repl");
}
