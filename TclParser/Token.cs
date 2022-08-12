namespace TclParser;

using System.Text;


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
