using PL0Resources;

namespace PL0Compiler
{
    public class TokenValue
    {
        public TokenValue()
        {
        }

        public TokenValue(Token token, string name = "", object value = null)
        {
            Token = token;
            Name = name;
            Value = value;
        }

        public Token Token { get; set; }
        public string Name { get; set; }
        public object Value { get; set; }
    }
}
