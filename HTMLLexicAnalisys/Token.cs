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

        public Token(SyntaxKind kind, string? tagName, AttributeList? attributes, string text, int position)
        {
            if (attributes is not null && attributes.Count > 0)
                Attributes = attributes;
            else
                Attributes = null;
            if (tagName is not null)
                TagName = tagName;

            Kind = kind;
            Text = text;
            Position = position;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{Kind}: '{TagName}', '{Position}', '{Text}'");

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
