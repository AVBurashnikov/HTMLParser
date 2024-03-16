using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YellowOak.TreeBuilder
{
    internal interface IFindable
    {
        public Node Find(string tagName);
        public Node Find(string tagName, string attrKey, string attrValue);
        public List<Node> FindAll(string tagName);
        public List<Node> FindAll(string tagName, string attrKey, string attrValue);


    }
}
