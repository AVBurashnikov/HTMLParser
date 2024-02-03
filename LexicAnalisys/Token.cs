using System.Text;

namespace YellowOak.LexicAnalisys
{
    interface ITokenable
    {
        
    }

    class Token: ITokenable
    {
        public SyntaxKind Kind { get; }
        public string? TagName { get; }
        public Dictionary<string, string[]>? Attributes { get; }
        public string Text { get; }
        public int Position { get; }

        public Token(SyntaxKind kind, string? tagName, Dictionary<string, string[]>? attributes, string text, int position)
        {
            Kind = kind;
            TagName = tagName;
            Attributes = attributes;
            Text = text;
            Position = position;
        }

        public override string ToString()
        {
            var builder = new StringBuilder();

            builder.Append($"{Kind}: '{TagName}', '{Position}', '{Text}'\n");

            if (Attributes is not null)
            {
                builder.Append($"Attributes:\n");
                foreach (KeyValuePair<string, string[]> attr in Attributes)
                {
                    builder.Append($"\t{attr.Key.ToString()} = ");

                    if (attr.Key == "class")
                    {
                        builder.Append("[ ");
                        foreach (string attrValue in attr.Value)
                            builder.Append($"'{attrValue}', ");
                        builder.Append("]\n");
                    }
                    else
                        builder.Append($"'{attr.Value[0]}'\n");
                }
            }

            builder.Append('\n');

            return builder.ToString();
        }
    }
}
