using System.Text;

namespace YellowOak.HTMLLexicAnalisys
{
    internal record Attribute(string name, string value)
    {
        public string Name = name;
        public string Value = value;

        public override string ToString()
        {
            return $"\t{name}='{value}'\n";
        }
    }
}
