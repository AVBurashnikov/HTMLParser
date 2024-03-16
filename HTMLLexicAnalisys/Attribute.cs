using System.Text;

namespace YellowOak.HTMLLexicAnalisys
{
    internal record Attribute(string key, string value)
    {
        public string Key = key;
        public string Value = value;

        public override string ToString()
        {
            return $"\t{Key}='{Value}'\n";
        }
    }
}
