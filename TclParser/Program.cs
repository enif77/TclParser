using System.Text;


Console.WriteLine("TCL Parser v1.0.0");

var source = new NewLineEscapingSourceReader(new StringSourceReader("  puts    {Hello, World!#};\\\n   puts $v\n # pwd\n set a 123"));
var tokenizer = new Tokenizer(source);

while (true)
{
    var getNextTokenResult = tokenizer.NextToken();    
    if (getNextTokenResult.IsSuccess == false)
    {
        Console.WriteLine("E: {0}", getNextTokenResult.Message);

        break;
    }

    var tok = getNextTokenResult.Data!;

    Console.WriteLine("T: {0}", tok);

    if (tok.Code == TokenCode.EoF)
    {
        break;
    }    
}


#region tokenizer

public class Tokenizer : ITokenizer
{
    public IToken CurrentToken { get; private set;}


    public Tokenizer(ISourceReader reader)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
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
                    CurrentToken = GenerateToken(buffer, () => Token.EofToken())
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
            
            if (IsCommandsSeparator(c))
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
            
            if (c == '{')
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

        // TODO: Add support for "\{" and multiple levels of {aa {ee} bb}.

        // and consume all chars till the nearest '}' or EoF.
        var buffer = new StringBuilder();
        while (true)
        {
            if (c == '}')
            {
                // Eat the bracketed word end.
                _ = _reader.NextChar();

                break;
            }
            
            if (IsEoF(c))
            {
                return Result<IToken>.Error("The '}' bracketed word end is missing.");
            }

            buffer.Append((char)c);

            c = _reader.NextChar();
        }

        return Result<IToken>.Ok(
            CurrentToken = Token.WordToken(buffer.ToString())
        );
    }
}

#endregion


#region tokens

public enum TokenCode
{
    Unknown,
    EoF,
    CommandSeparator,
    Word,
    Text,
    VariableSubstitution,
    CommandSubstitution
}


public interface IToken
{
    TokenCode Code { get; }
    string Data { get; }
    IList<IToken> Children { get; }
}


public class Token : IToken
{
    public TokenCode Code { get; }
    public string Data { get; }
    public IList<IToken> Children { get; }


    private Token(TokenCode tokenCode, string stringValue = "")
    {
        Code = tokenCode;
        Data = stringValue;
        Children = new List<IToken>();
    }


    public static IToken EofToken()
        => new Token(TokenCode.EoF);
    
    
    public static IToken CommandSeparatorToken()
    => new Token(TokenCode.CommandSeparator);
    
    
    public static IToken TextToken(string text)
        => new Token(TokenCode.Text, text);
    
    
    public static IToken WordToken()
        => new Token(TokenCode.Word, "word");

    
    public static IToken WordToken(string text)
        => WordToken(TextToken(text));

    
    private static IToken WordToken(IToken child)
    {
        var tok = WordToken();
        
        tok.Children.Add(child);

        return tok;
    }
    
    
    public static IToken CommandSubstitutionToken(string commandSubstitution)
        => new Token(TokenCode.CommandSubstitution, commandSubstitution);
    
    
    public static IToken VariableSubstitutionToken(string variableName)
        => new Token(TokenCode.VariableSubstitution, variableName);


    public override string ToString()
    {
        var tokName = Code.ToString();
        var tokDescription = string.IsNullOrEmpty(Data)
            ? string.Empty
            : $" '{Data}'";

        if (Children.Any())
        {
            var childrenListSb = new StringBuilder();
            foreach (var child in Children)
            {
                childrenListSb.Append(child);
                childrenListSb.Append(", ");
            }

            childrenListSb.Append('*');
            childrenListSb.Replace(", *", string.Empty);

            tokDescription = $" [{childrenListSb}]";
        }

        return $"{tokName}{tokDescription}";
    }
}


public interface ITokenizer
{
    IToken CurrentToken { get; }
    IResult<IToken> NextToken();
}

#endregion


#region source reader

public interface ISourceReader
{
    int CurrentChar { get; }
    int NextChar();
}


public class StringSourceReader : ISourceReader
{
    public int CurrentChar { get; private set; }


    public StringSourceReader(string src)
    {
        _src = src ?? throw new ArgumentNullException(nameof(src));
        _sourcePosition = -1;
        CurrentChar = -1;
    }


    public int NextChar()
    {
        _sourcePosition++;
        if (_sourcePosition < _src.Length)
        {
            return CurrentChar = _src[_sourcePosition];
        }
        
        _sourcePosition = _src.Length;

        return CurrentChar = -1;
    }


    private readonly string _src;
    private int _sourcePosition;
}


public class NewLineEscapingSourceReader : ISourceReader
{
    public int CurrentChar { get; private set; }


    public NewLineEscapingSourceReader(ISourceReader sourceReader)
    {
        _sourceReader = sourceReader ?? throw new ArgumentNullException(nameof(sourceReader));
        CurrentChar = _sourceReader.CurrentChar;
    }


    public int NextChar()
    {
        if (_nextCharBuffer >= 0)
        {
            var nextChar = _nextCharBuffer;
            _nextCharBuffer = -1;

            return CurrentChar = nextChar;
        }

        var c = _sourceReader.NextChar();
        if (c != '\\')
        {
            return CurrentChar = c;
        }

        c = _sourceReader.NextChar();
        if (c != '\n')
        {
            _nextCharBuffer = c;

            return CurrentChar = '\\';
        }

        c = _sourceReader.NextChar();
        while (IsWhiteSpace(c))
        {
            c = _sourceReader.NextChar();
        }

        _nextCharBuffer = c;

        return CurrentChar = ' ';
    }

    
    private readonly ISourceReader _sourceReader;
    private int _nextCharBuffer = -1;
    
    
    private static bool IsWhiteSpace(int c)
        => IsWordsSeparator(c) == false && char.IsWhiteSpace((char)c);

   
    private static bool IsWordsSeparator(int c)
        => c is '\n' or ';';
}

#endregion


#region results

public interface IResult
{
    bool IsSuccess { get; }
    string Message { get; }
}


public interface IResult<out T> : IResult
{
    T? Data { get; }
}


public class Result<T> : IResult<T>
{
    private const string OkMessage = "Ok";
    private const string ErrorMessage = "Error";
    
    public bool IsSuccess { get; }
    public string Message { get; }
    public T? Data { get; }

   
    private Result(bool isSuccess, T? data = default, string message = "")
    {
        IsSuccess = isSuccess;
        Data = data;
        Message = message;
    }
    

    public static IResult<T> Ok(string? message = default)
        => new Result<T>(true, default, message ?? OkMessage);
    

    public static IResult<T> Ok(T? data, string? message = default)
        => new Result<T>(true, data, message ?? OkMessage);
        

    public static IResult<T> Error(string? message = default)
        => new Result<T>(false, default, message ?? ErrorMessage);
    

    public static IResult<T> Error(T? data, string? message = default)
        => new Result<T>(false, data, message ?? ErrorMessage);
        
    
    public static IResult<Exception> Error(Exception ex, string? message = default)
        => new Result<Exception>(false, ex, message ?? ex.Message);
}


public static class SimpleResult
{
    public static IResult Ok(string? message = default)
        => Result<object>.Ok(message);
    

    public static IResult Ok(object? data, string? message = default)
        => Result<object>.Ok(data, message);


    public static IResult Error(string? message = default)
        => Result<object>.Error(message);
    

    public static IResult Error(object? data, string? message = default)
        => Result<object>.Error(data, message);


    public static IResult Error(Exception ex, string? message = default)
        => Result<object>.Error(ex, message);
    

    public static IResult FromBoolean(bool state, string? message = default)
        => state
            ? Ok(message)
            : Error(message);
}

#endregion
