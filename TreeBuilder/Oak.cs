using YellowOak.Utils;
using YellowOak.HTMLLexicAnalisys; 
    
namespace YellowOak.TreeBuilder
{
    /// <summary>
    ///  Dispatcher class for loading HTML markup and creating a tag tree.
    /// </summary>
    internal class Oak
    {
        // We store an instance of the oak
        // class to implement the singleton pattern
        private static Oak? _instance = null;

        // Tag tree, processing tree nodes obtained
        // from lexical parsing tokens are stored here.
        private readonly Tree _tree = new();

        // Variable for storing markup obtained from
        // a file or URL address
        private string _markup = "";

        // Hiding the constructor
        private Oak()
        {}

        /// <summary>
        ///     Creating an instance of the Oak class
        /// </summary>
        /// <returns></returns>
        public static Oak GetInstance()
        {
            if (_instance == null)
            {
                _instance = new Oak();
            }
            return _instance;
        }

        // Needs improvement
        //
        /// <summary>
        ///     Pretty print for tree of html tags
        /// </summary>
        /// <returns></returns>
        public void Print()
        {
            Console.WriteLine(_tree.ToString());
        }

        // Needs improvement
        //
        /// <summary>
        ///     Load markup from Url
        /// </summary>
        /// <param name="url"></param>
        public void Load(FromUrl url)
        {
            
        }

        // Needs improvement
        //
        /// <summary>
        ///     Loading markup from a local file
        /// </summary>
        /// <param name="filename"></param>
        public void Load(FromFile filename)
        {
            var sr = new StreamReader(filename.Path);
            _markup = sr.ReadToEnd();
        }

        /// <summary>
        ///     Method for starting parsing lexemes 
        ///     into tag tree nodes
        /// </summary>
        public void Grow()
        {
            var tokenizer = new Tokenizer();
            var tokens = tokenizer.Parse(_markup);

            _tree.Grow(tokens);
        }

        public Node? GetNodeWithTag(string tag)
        {
            foreach (Node node in _tree.GetTree)
            {
                if (node.Tag == tag)
                {
                    return node;
                }
            }
            return null;
        }

        public List<Node> GetNodesWithTag(string tag)
        {
            List<Node> nodes = [];
            foreach (Node node in _tree.GetTree)
            {
                if (node.Tag == tag)
                {
                    nodes.Add(node);
                }
            }
            return nodes;
        }

        public Node GetNodeWithAttr(string name, string value)
        {
            foreach (Node node in _tree.GetTree)
            {
                if (node.Attributes.Count == 0)
                {
                    continue;
                }
                else
                {
                    foreach(YellowOak.HTMLLexicAnalisys.Attribute attribute in node.Attributes)
                    {
                        if ( attribute.Name == name && attribute.Value == value)
                        {
                            return node;
                        }
                    }
                }
            }
            return null;
        }

        public List<Node> GetNodesWithAttr(string name, string value)
        {
            List<Node> nodes = [];

            foreach (Node node in _tree.GetTree)
            {
                if (node.Attributes.Count == 0)
                {
                    continue;
                }
                else
                {
                    foreach (HTMLLexicAnalisys.Attribute attribute in node.Attributes)
                    {
                        if (attribute.Name == name && attribute.Value == value)
                        {
                            nodes.Add(node);
                        }
                    }
                }
            }
            return nodes;
        }
    }
}
