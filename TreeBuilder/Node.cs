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
        public AttributeList? Attributes { get; set; }
        public string? Text { get; set; }
        public Node? Parent { get; set; }
        public List<Node>? Children { get; set; }

        public Node(SyntaxKind kind, string? tag, 
                    AttributeList? attributes, string? text, 
                    Node? parent, List<Node>? children)
        {
            Kind = kind;
            Tag = tag;
            Attributes = attributes;
            Text = text;
            Parent = parent;
            Children = children;
        }
    
        public string InnerText()
        {
            return this.InnerHTML();
        }

        public string InnerHTML()
        {
            return this.InnerText();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[ tag = {Tag} ");
            sb.Append($"  parent = {Parent?.ToString()}");
            sb.AppendLine();
            sb.AppendLine();
            return sb.ToString();
        }
    }
}
