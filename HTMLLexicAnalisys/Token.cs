using System.Text;

namespace YellowOak.HTMLLexicAnalisys
{
    interface IToken
    {
        
    }

    class Token: IToken
    {
        public SyntaxKind Kind { get; }
        public string? TagName { get; }
        public AttributeList? Attributes { get; }
        public string Text { get; }
        public int Position { get; }

        public Token(SyntaxKind kind, string? tagName, AttributeList? attributes, string text)
        {
            if (attributes is not null && attributes.Count > 0)
                Attributes = attributes;
            else
                Attributes = null;

            if (tagName is not null)
                TagName = tagName;

            Kind = kind;
            Text = text;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{Kind}: '{TagName}', '{Text}'");

            if (Attributes is not null)
            {
                sb.AppendLine("Attributes");
                foreach (Attribute attribute in Attributes)
                    sb.AppendLine($"{attribute.Name} = '{attribute.Value}'");
            }

            sb.AppendLine();

            return sb.ToString();
        }
    }
}
