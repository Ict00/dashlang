using SaltLang.Lexing;

namespace SaltLang.Parsing;

public class Parser(string fileName, List<Token> tokens)
{
    private int _idx = 0;

    void Next(Token requiredBy)
    {
        if (_idx + 1 < tokens.Count)
        {
            _idx++;
        }
        else
        {
            Eroro.MakeEroro($"Expected expression, got nothing", requiredBy.Meta);
        }
    }

    Maybe<Token> CheckNext()
    {
        if (_idx + 1 < tokens.Count)
        {
            return new Just<Token>(tokens[_idx + 1]);
        }
        return new Nothing<Token>();
    }
    
    Token Current() => tokens[_idx];

    bool HasToBe(Token @checked, TokenType reqType)
    {
        return @checked.TokenType == reqType;
    }

    bool HasToBe(Token @checked, string value)
    {
        return @checked.Value.Equals(value);
    }

    bool HasToBe(Token @checked, TokenType reqType, string value)
    {
        return @checked.TokenType == reqType && @checked.Value.Equals(value);
    }
    
    bool Until(TokenType tokenType) => tokenType != tokens[_idx].TokenType;
    
    
    
    public void ParseAll()
    {
        
    }
}