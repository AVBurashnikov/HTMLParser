using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YellowOak.TreeBuilder
{
    internal class Branch
    {
        public List<Node> _branch = [];
        public Branch(List<Node> branch) 
        {
            _branch = branch;
        }
    }
}
