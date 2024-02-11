using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YellowOak.TreeBuilder
{
    internal class FromUrl
    {

    }

    internal class FromFile(string path)
    {
        private string _path = path;
        public string Path 
        {
            get => _path;
            set => _path = value;
        }
    }
}
