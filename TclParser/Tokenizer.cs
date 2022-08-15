namespace TclParser;

using System.Text;


public interface ITokenizer
{
    IToken CurrentToken { get; }
    IResult<IToken> NextToken();
}


public class Tokenizer : ITokenizer
{
    public IToken CurrentToken { get; private set;}


    public Tokenizer(ISourceReader reader)
    {
        _reader = new NewLineEscapingSourceReader(
                reader ?? throw new ArgumentNullException(nameof(reader)));
        CurrentToken = Token.EofToken();

        _ = _reader.NextChar();
    }


    public IResult<IToken> NextToken()
    {
        var buffer = new StringBuilder();
       
        while (true)
        {
            var c = _reader.CurrentChar;
            if (IsEoF(c))
            {
                return Result<IToken>.Ok(
                    CurrentToken = GenerateToken(buffer, Token.EofToken)
                );
            }
            
            if (IsWhiteSpace(c))
            {
                if (buffer.Length > 0)
                {
                    // A white space ends the current word.
                    return Result<IToken>.Ok(
                        CurrentToken = Token.WordToken(buffer.ToString())
                    );
                }

                SkipWhitespace();    

                continue;
            }

            if (IsCommentStart(c))
            {
                // Not in the middle of a word?
                if (buffer.Length == 0)
                {
                    SkipComment();

                    continue;
                }
            }
            else if (IsCommandsSeparator(c))
            {
                return Result<IToken>.Ok(
                    CurrentToken = GenerateToken(buffer, () =>
                    {
                        // Consume the command separator.
                         _ = _reader.NextChar();

                        return Token.CommandSeparatorToken();
                    })
                );
            }
            else if (c == '{')
            {
                // Not in the middle of a word?
                if (buffer.Length == 0)
                {
                    return CollectBracketedWord();
                }
            }
            
            buffer.Append((char)c);

            _ = _reader.NextChar();
        }
    }


    private readonly ISourceReader _reader;


    /// <summary>
    /// Generates a token from the collected chars buffer.
    /// </summary>
    /// <param name="buffer">A chars buffer, that contains so far collected chars, that will be returned as the Text token.</param>
    /// <param name="whenEmpty">An action, that will be executed, when nothing was collected so far.</param>
    /// <returns>A token.</returns>
    private IToken GenerateToken(StringBuilder buffer, Func<IToken> whenEmpty)
    {
        return (buffer.Length > 0)
            ? Token.WordToken(buffer.ToString())
            : whenEmpty();
    }


    private static bool IsEoF(int c)
        => c < 0;


    private static bool IsCommentStart(int c)
        => c is '#';


    private static bool IsCommentEnd(int c)
        => c == '\n' || IsEoF(c);


    private static bool IsCommandsSeparator(int c)
        => c is '\n' or ';';


    private static bool IsWhiteSpace(int c)
        => IsCommandsSeparator(c) == false && char.IsWhiteSpace((char)c);

    
    private static bool IsWordEnd(int c)
        => IsEoF(c) || IsCommandsSeparator(c) || IsWhiteSpace(c);


    private void SkipWhitespace()
    {
        while (IsWhiteSpace(_reader.NextChar()))
        {
        }
    }


    private void SkipComment()
    {
        // Consume the comment start char...
        var c = _reader.NextChar();

        // and skip all chars till the nearest EoLN or EoF.
        while (true)
        {
            if (IsCommentEnd(c))
            {
                // Eat the comment end.
                _ = _reader.NextChar();

                break;
            }

            c = _reader.NextChar();
        }
    }
    
    
    private IResult<IToken>CollectBracketedWord()
    {
        // Consume the bracketed word start char...
        var c = _reader.NextChar();
        
        // and consume all chars till the nearest '}' or EoF.
        var buffer = new StringBuilder();
        var level = 1;
        while (true)
        {
            if (IsEoF(c))
            {
                break;
            }
            
            if (c == '{')
            {
                level++;
            }
            else if (c == '}')
            {
                level--;
                if (level == 0)
                {
                    // Eat the bracketed word end.
                    if (IsWordEnd(_reader.NextChar()) == false)
                    {
                        return Result<IToken>.Error("An EoF, words or commands separator after the '}' bracketed word end expected.");
                    }

                    break;
                }
            }
            else if (c == '\\')
            {
                c = _reader.NextChar();
                if (c != '{' && c != '}')
                {
                    buffer.Append('\\');
                }
            }

            buffer.Append((char)c);

            c = _reader.NextChar();
        }

        return (level > 0)
            ? Result<IToken>.Error("Too many opening '{' brackets in a bracketed word definition.")
            : Result<IToken>.Ok(CurrentToken = Token.WordToken(buffer.ToString()));
    }
}
