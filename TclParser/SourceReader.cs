namespace TclParser;

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
