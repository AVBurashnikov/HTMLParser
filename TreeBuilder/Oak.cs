using YellowOak.Utils;
using YellowOak.HTMLLexicAnalisys; 
    
namespace YellowOak.TreeBuilder
{
    internal class Oak
    {
        private readonly Tree _tree = new Tree();
        private string _markup = "";
    
        public void Load(FromUrl url)
        {
            
        }

        public string PrintTree()
        {
            return _tree.ToString();
        }

        public void Load(FromFile filename)
        {
            var sr = new StreamReader(filename.Path);
            _markup = sr.ReadToEnd();
        }

        public void GrowTree()
        {
            var tokenizer = new Tokenizer();
            var tokens = tokenizer.Parse(_markup);

            _tree.Grow(tokens);
        }
    }
}
