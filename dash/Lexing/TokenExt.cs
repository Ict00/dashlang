namespace dash.Lexing;

public static class TokenExt
{
    public static ParsedMeta FakePMeta(string val)
    {
        var meta = new Meta(0, "<fake>", val);
        return new ParsedMeta(meta,
            new Token(val, TokenType.String, meta));
    }
}