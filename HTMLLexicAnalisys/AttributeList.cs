using System.Collections;

namespace YellowOak.HTMLLexicAnalisys
{
    interface IAttributeList
    {
        public int Count { get; }
        public List<Attribute> GetAttributes { get; }
        public void Add(Attribute attribute);
        public void Clear();
        public List<Attribute> GetAttribute(string name);

    }

    internal class AttributeList: IAttributeList, IEnumerable<Attribute>
    {
        private readonly List<Attribute> _attributes = [];

        public List<Attribute> GetAttributes => _attributes;

        public int Count => _attributes.Count;

        public void Add(Attribute attribute)
        {
                _attributes.Add(attribute);
        }

        public void Clear()
        {
            _attributes.Clear();
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

        public IEnumerator<Attribute> GetEnumerator()
        {
            return _attributes.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator) GetEnumerator();
        }
    }
}
