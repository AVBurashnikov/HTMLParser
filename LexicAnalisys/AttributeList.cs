using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YellowOak.LexicAnalisys
{
    interface IAttributeList
    {
        public List<Attribute> GetAttributes { get; }
        public List<Attribute> GetAttribute(string name);
        public void Add(Attribute attribute);

    }
    internal class AttributeList: IAttributeList
    {
        private readonly List<Attribute> _attributes = [];

        public List<Attribute> GetAttributes => _attributes;

        public void Add(Attribute attribute)
        {
            _attributes.Add(attribute);
        }

        public List<Attribute> GetAttribute(string name)
        {
            List<Attribute> attributes = [];

            foreach (Attribute attribute in _attributes)
            {
                if (attribute.Name == name)
                {
                    attributes.Add(attribute);
                }
            }
            return attributes;
        }
    }
}
