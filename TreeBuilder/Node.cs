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
        public string Tag {  get; set; }
        public AttributeList Attributes { get; set; }
        public Node Parent { get; set; }
        public List<Node> Children { get; set; }
    
        public string InnerText()
        {
            return InnerHTML();
        }

        public string InnerHTML()
        {
            return InnerText();
        }
    }
}
