namespace TclParser.Tests;

using Xunit;


public class TokenizerTests
{
    #region ctor
    
    [Fact]
    public void SourceReaderIsRequiredWhenCreated()
    {
#pragma warning disable CS8625
        Assert.Throws<ArgumentNullException>(() => new Tokenizer(null));
#pragma warning restore CS8625
    }

    [Fact]
    public void TokenizerCallsNextCharWhenCreated()
    {
        var reader = new StringSourceReader("test");
        
        Assert.True(reader.CurrentChar < 0);
        
        _ = new Tokenizer(reader);
        
        Assert.Equal('t', reader.CurrentChar);
    }

    [Fact]
    public void CurrentTokenIsSetWhenCreated()
    {
        var tokenizer = new Tokenizer(new StringSourceReader("test"));
        
        Assert.NotNull(tokenizer.CurrentToken);
    }
    
    [Fact]
    public void CurrentTokenIsEoFWhenCreated()
    {
        var tokenizer = new Tokenizer(new StringSourceReader("test"));
        
        Assert.Equal(TokenCode.EoF, tokenizer.CurrentToken.Code);
    }
    
    #endregion
    
}
