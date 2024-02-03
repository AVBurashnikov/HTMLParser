using System.Text;

namespace YellowOak.LexicAnalisys
{
    interface IAttributeable
    {
        public string Name { get; }
        public string Value { get; }
        public void ToName(char c);
        public void ToValue(char c);
    }

    internal class Attribute : IAttributeable
    {
        private readonly StringBuilder name = new ("");
        private readonly StringBuilder value = new ("");
        public string Name => name.ToString();
        public string Value => value.ToString();

        public void ToName(char c)
        {
            name.Append(c);
        }

        public void ToValue(char c)
        {
            value.Append(c);
        }
    }
}
