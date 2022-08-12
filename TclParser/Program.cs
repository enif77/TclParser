using TclParser;


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
