namespace SaltLang.Lexing;

public static class Lexer
{
    public static readonly List<char> Units = ['+', '-', '*', ':', '/', '=', '.', ',', '>', '<', '|', '&', '\''];
    public static readonly List<char> Brackets = ['[', ']', '{', '}', '(', ')'];
    public static readonly List<char> Operators = [];
    public static readonly List<string> Keywords = [];
    
    
    private enum WritingCurrently
    {
        String,
        Identifier,
        Comment,
        Operator
    }
    
    public static List<Token> Tokenize(string text, string fileName)
    {
        List<Token> @return = [];
        WritingCurrently writing = WritingCurrently.Identifier;
        string value = string.Empty;
        int line = 1;

        foreach (var c in text)
        {
            if (c == '\n')
            {
                line++;
            }

            switch (writing)
            {
                case WritingCurrently.Identifier:
                    
                    if (char.IsWhiteSpace(c))
                    {
                        TryConstruct();
                        break;
                    }

                    if (c == ';')
                    {
                        TryConstruct();
                        @return.Add(new Token(";", TokenType.Eol, new Meta(line, fileName, GetWholeLine())));
                        break;
                    }

                    if (Units.Contains(c))
                    {
                        TryConstruct();
                        if (c == '\'')
                        {
                            break;
                        }
                        value += c;
                        writing = WritingCurrently.Operator;
                        break;
                    }

                    if (Brackets.Contains(c))
                    {
                        TryConstruct();
                        @return.Add(new Token(c.ToString(), TokenType.Bracket, new Meta(line, fileName, GetWholeLine())));
                        break;
                    }

                    if (c == '"')
                    {
                        TryConstruct();
                        writing = WritingCurrently.String;
                        break;
                    }

                    if (c == '#')
                    {
                        TryConstruct();
                        writing = WritingCurrently.Comment;
                        break;
                    }
                    
                    value += c;
                    
                    break;
                
                case WritingCurrently.Operator:
                    if (Units.Contains(c))
                    {
                        value += c;
                        break;
                    }
                    TryConstruct();
                    if(c == '#') writing = WritingCurrently.Comment;
                    else if(c == '"') writing = WritingCurrently.String;
                    else
                    {
                        writing = WritingCurrently.Identifier;
                        if (!char.IsWhiteSpace(c))
                        {
                            value += c;
                        }
                    }
                    break;
                
                case WritingCurrently.String:
                    if (c == '"')
                    {
                        if(!value.EndsWith("\\"))
                        {
                            @return.Add(new Token(value, TokenType.String, new Meta(line, fileName, GetWholeLine())));
                            value = "";
                            writing = WritingCurrently.Identifier;
                            break;
                        }
                        else
                        {
                            value  = value.Substring(0, value.Length - 1);
                        }
                    }
                    value += c;
                    
                    break;
                
                case WritingCurrently.Comment:
                    if (c == '\n' || c == '#')
                    {
                        writing = WritingCurrently.Identifier;
                    }
                    break;
            }
        }
        
        TryConstruct();
        
        return @return;

        void TryConstruct()
        {
            if (value != string.Empty)
            {
                switch (writing)
                {
                    case WritingCurrently.Identifier:
                        if (Keywords.Contains(value))
                        {
                            @return.Add(new Token(value, TokenType.Keyword, new Meta(line, fileName, GetWholeLine())));
                        }
                        else
                        {
                            @return.Add(new Token(value, TokenType.Identifier, new Meta(line, fileName, GetWholeLine())));
                        }
                        
                        break;
                    
                    case WritingCurrently.Operator:
                        @return.Add(new Token(value, TokenType.Operator, new Meta(line, fileName, GetWholeLine())));
                        break;
                }
                value = "";
            }
        }

        string GetWholeLine() => text.Split('\n')[line - 1];
    }
}