namespace dash.Lexing;

public static class Lexer
{
    public static readonly List<char> Units = ['+', '^', '-', '*', '!', ':', '/', '=', '.', ',', '>', '<', '|', '&', '\'', '%'];
    public static readonly List<char> Brackets = ['[', ']', '{', '}', '(', ')'];
    public static readonly List<string> Keywords = ["if", "ref", "dict", "this", "predicate", "exec", "break", "continue", "return", "then", "in", "is", "as", "while", "for", "foreach", "true", "false", "fun", "else", "_", "null", "struct", "let", "module", "import"];

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

                    if (c == '\\')
                    {
                        TryConstruct();
                        @return.Add(new Token("\\", TokenType.Lambda, new Meta(line, fileName, GetWholeLine())));
                        break;
                    }

                    if (c == ';')
                    {
                        TryConstruct();
                        @return.Add(new Token(";", TokenType.Eol, new Meta(line, fileName, GetWholeLine())));
                        break;
                    }

                    if (value.EndsWith('.') && !char.IsDigit(c))
                    {
                        value = value.Substring(0, value.Length - 1);
                        TryConstruct();
                        if (Units.Contains(c))
                        {
                            value = $".{c}";
                            writing = WritingCurrently.Operator;
                            continue;
                        }
                        @return.Add(new Token(".", TokenType.Operator, new Meta(line, fileName, GetWholeLine())));
                    }

                    if (Units.Contains(c))
                    {
                        if (c == '.')
                        {
                            bool digits = true;
                            foreach (var i in value)
                            {
                                if (!char.IsDigit(i))
                                {
                                    digits = false;
                                    break;
                                }
                            }

                            if (digits)
                            {
                                value += '.';
                                continue;
                            }
                        }

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
                    if (c == '#') writing = WritingCurrently.Comment;
                    else if (c == '"') writing = WritingCurrently.String;
                    else
                    {
                        writing = WritingCurrently.Identifier;
                        if (!char.IsWhiteSpace(c))
                        {
                            goto case WritingCurrently.Identifier;
                        }
                    }
                    break;

                case WritingCurrently.String:
                    if (c == '"')
                    {
                        if (!value.EndsWith("\\"))
                        {
                            @return.Add(new Token(value, TokenType.String, new Meta(line, fileName, GetWholeLine())));
                            value = "";
                            writing = WritingCurrently.Identifier;
                            break;
                        }
                        else
                        {
                            value = value.Substring(0, value.Length - 1);
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

        List<Token> reMade = [];

        foreach (var i in @return)
        {
            if(i.TokenType != TokenType.Eol) reMade.Add(i);
        }
        
        return reMade;

        void TryConstruct()
        {
            if (value != string.Empty)
            {
                switch (writing)
                {
                    case WritingCurrently.Identifier:

                        if (value.Contains('.') && value != ".")
                        {
                            switch (value[^1])
                            {
                                case 'f':
                                    @return.Add(new Token(value.Substring(0, value.Length - 1), TokenType.Float, new Meta(line, fileName, GetWholeLine())));
                                    break;
                                case 'm':
                                    @return.Add(new Token(value.Substring(0, value.Length - 1), TokenType.Decimal, new Meta(line, fileName, GetWholeLine())));
                                    break;
                                default:
                                    @return.Add(new Token(value, TokenType.Double, new Meta(line, fileName, GetWholeLine())));
                                    break;
                            }

                            value = "";
                            return;
                        }

                        bool digits = true;

                        foreach (var i in value)
                        {
                            if (!char.IsDigit(i))
                            {
                                digits = false;
                                break;
                            }
                        }

                        if (digits)
                        {
                            @return.Add(new Token(value, TokenType.Number, new Meta(line, fileName, GetWholeLine())));
                            value = "";
                            return;
                        }

                        @return.Add(Keywords.Contains(value)
                            ? new Token(value, TokenType.Keyword, new Meta(line, fileName, GetWholeLine()))
                            : new Token(value, TokenType.Identifier, new Meta(line, fileName, GetWholeLine())));

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
