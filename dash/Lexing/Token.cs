namespace dash.Lexing;

public record Token(string Value, TokenType TokenType, Meta Meta);
public record Meta(int Line, string File, string WholeLine);
public record ParsedMeta(Meta Meta, Token Origin);

public enum TokenType
{
    Bracket,
    Identifier,
    Float,
    Decimal,
    Double,
    Number,
    Lambda,
    Eol,
    String,
    Operator,
    Keyword
}