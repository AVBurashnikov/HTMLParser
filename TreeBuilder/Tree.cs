using System.Text;
using YellowOak.HTMLLexicAnalisys;

namespace YellowOak.TreeBuilder
{
    internal class Tree
    {
        private readonly List<Node> _tree = [];
        private readonly Stack<Node> _stack = [];
        public void Grow(List<Token> tokens)
        {
            var i = 0;
            
            while (i < tokens.Count)
            {
                Node node;
                var token = tokens[i];
                var parent = _stack.Count > 0 ? _stack.Peek() : null;
                
                switch (token.Kind)
                {
                    case SyntaxKind.Doctype:
                        _tree.Add(new Node(token.Kind, 
                                            null, 
                                            null, 
                                            null, 
                                            null, 
                                            null));
                        break;
                    
                    case SyntaxKind.OpenTag:
                        node = new Node(token.Kind,
                                            token.TagName,
                                            token.Attributes,
                                            null,
                                            parent,
                                            []);
                        _stack.Push(node);
                        parent?.Children?.Add(node);
                        break;
                    
                    case SyntaxKind.AutoClosingTag:
                        node = new Node(token.Kind,
                            token.TagName,
                            token.Attributes,
                            null,
                            parent,
                            null);
                        parent?.Children?.Add(node);
                        break;
                    
                    case SyntaxKind.Content:
                    case SyntaxKind.Comment:
                        node = new Node(token.Kind,
                                        null,
                                        null,
                                        token.Text,
                                        parent,
                                        null);
                        _tree.Add(node);
                        parent?.Children?.Add(node);
                        break;
                    case SyntaxKind.ClosingTag:
                        _tree.Add(_stack.Pop());
                        break;
                }

                i++;
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var node in _tree)
            {
                sb.Append(node.ToString());
            }

            return sb.ToString();
        }
    }
}
