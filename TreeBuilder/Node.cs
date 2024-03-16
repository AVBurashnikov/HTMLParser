using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YellowOak.HTMLLexicAnalisys;

namespace YellowOak.TreeBuilder
{
    internal class Node
    {
        public SyntaxKind Kind { get; set; }
        public string? Tag {  get; set; }
        public AttributeList Attributes { get; set; }
        public string? Text { get; set; }
        public Node? Parent { get; set; }
        public List<Node> Children { get; set; }

        public Node(SyntaxKind kind, string? tag, 
                    AttributeList attributes, string? text, 
                    Node? parent, List<Node> children)
        {
            Kind = kind; // SyntaxKind.Doctype
            Tag = tag; // None|div
            Attributes = attributes; // None|class='one'
            Text = text; // some text
            Parent = parent; // Node(...)
            Children = children; // [Node(...), Node(...), ..., Node(...)]
        }
    
        public string InnerText()
        {
            return this.InnerHTML();
        }

        public string InnerHTML()
        {
            return this.InnerText();
        }

        public List<Node> GetChilds()
        {
            return Children;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{Tag}");
            sb.AppendLine($"{Attributes}");
            sb.AppendLine();
            return sb.ToString();
        }
    }
}
