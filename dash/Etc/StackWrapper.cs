using dash.Lexing;

namespace dash.Etc;

public static class StackWrapper
{
    public static T DPop<T>(this Stack<T> stack, Meta meta)
    {
        if (stack.Count == 0)
        {
            Eroro.MakeEroro("Stack empty [most likely language's problem]", meta);
            throw new Exception();
        }

        return stack.Pop();
    }
    
    public static T DPeek<T>(this Stack<T> stack, Meta meta)
    {
        if (stack.Count == 0)
        {
            Eroro.MakeEroro("Stack empty [most likely language's problem]", meta);
            throw new Exception();
        }

        return stack.Peek();
    }
}