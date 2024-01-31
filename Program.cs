using System.Data;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic;
using static System.Net.Mime.MediaTypeNames;

namespace htmlParser
{
    class Program
    {
        public static void Main()
        {
            var sr = new StreamReader("file.html");
            var data = sr.ReadToEnd();
            var tokenizer = new Tokenizer();
            var tokens = tokenizer.Parse(data);
            foreach(var token in tokens)
                Console.WriteLine(token);
        }
    }

    enum TokenKind
    {
        OpenTagToken,
        SelfClosingTagToken,
        CommentToken,
        BogusCommentToken,
        DoctypeToken,
        ClosingTagToken,
        ContentToken,
        SpecialCharToken,
        CDATAToken
    }

    class Token(TokenKind kind, string? tagName, Dictionary<string, string[]>? attrs, string text, int position)
    {
        public TokenKind Kind { get; } = kind;
        public string? TagName { get; } = tagName;
        public Dictionary<string, string[]>? Attributes { get; } = attrs;
        public string Text { get; } = text;
        public int Position { get; } = position;

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.Append($"{Kind}: {TagName}, position={position}\n");

            if (Attributes is not null)
            {
                sb.Append($"Attributes:\n");
                foreach(KeyValuePair<string, string[]> attr in Attributes)
                {
                    sb.Append($"\t{attr.Key.ToString()} = ");

                    if (attr.Key == "class")
                    {
                        sb.Append("[ ");
                        foreach (string attrValue in attr.Value)
                            sb.Append($"{attrValue}, ");
                        sb.Append(']');
                    }
                    else
                        sb.Append(attr.Value[0].ToString() + '\n');
                }
            }

            sb.Append('\n');

            return sb.ToString();
        }
    }

    class Tokenizer
    {
        private string _markup = string.Empty;
        private int _start = 0;
        private int _position = 0;
        private bool _pauseParsing = false;

        private List<Token> _tokens = [];
        private Dictionary<string, string[]> _attrs = [];
        private List<char> _openTagName = [];
        private List<char> _closingTagName = [];
        private List<char> _attrName = [];
        private List<char> _attrValue = [];

        private char Current 
        {
            get 
            {
                if (_position >= _markup.Length) 
                {
                    return '\0';
                }
                return _markup[_position];
            }
        }

        private int Next() => _position++;

        private string Next(int offset)
        {
            if (_position + offset < _markup.Length)
                return _markup.Substring(_position, offset);
            else
                return _markup.Substring(_position);
        }

        public string Stringify(List<char> chars)
        {
            if (chars.Count == 0)
                return "";

            var sb = new StringBuilder();
            foreach (char c in chars)
                sb.Append(c);
            return sb.ToString();
        }

        private Dictionary<string, string[]> DictCopy(Dictionary<string, string[]> dict)
        {
            var newDict = new Dictionary<string, string[]>();

            if (dict.Count == 0)
                return null;

            foreach(KeyValuePair<string, string[]> pair in dict)
            {
                newDict.Add(pair.Key, pair.Value);
            }
            return newDict;
        }

        public List<Token> Parse(string markup) 
        {
            if (!string.IsNullOrEmpty(markup))
                _markup = markup;
            else
                return [];

            while (_position < markup.Length)
            {
                DataState();
            }

            return _tokens;
        }

        private void DataState()
        {
            if (Current == '\n' || Current == '\r' || Current == '\t' || Current == ' ' || Current == '\0')
            {
                Next();
                DataState();
            }
            else if (!_pauseParsing && Current == '<') // <...
            {
                _start = _position;
                BeforeTagState();
            }
            else if (_pauseParsing && Current == '<')
            {
                Next();
                if (Current == '/')
                {
                    _pauseParsing = false;
                    BeforeClosingTagState();
                }
            }
            else // Any text content
            {
                _start = _position;
                ContentState();
            }
        }

        private void ContentState()
        {
            Next();

            if (Current == '<')
            {
                if (!_pauseParsing)
                {
                    var text = _markup.Substring(_start, _position - _start);
                    _tokens.Add(new Token(TokenKind.ContentToken, "text element", null, text.Trim(), _start));
                    
                    DataState();
                }
                else
                {
                    Next();

                    if (Current == '/')
                    {
                        var text = _markup.Substring(_start, _position - _start - 1);
                        _tokens.Add(new Token(TokenKind.ContentToken, "text element", null, text.Trim(), _start));

                        _start = _position - 1;
                        BeforeClosingTagState();
                    }
                    else
                        ContentState();
                }
                    
            }
            else
                ContentState();
        }

        private void BeforeTagState()
        {
            Next();

            if (Current == '!') // <!--... , <!DOCTYPE..., <!...  
            {
                ExclamationMarkTagState();
            }
            else if (Current == '/') // </...
            {
                BeforeClosingTagState();
            }
            else if (char.IsLetter(Current)) // <a...
            {
                _openTagName.Clear();
                _openTagName.Add(Current);  
                TagNameState();
            }
            else
            {
                ParseError($"Invalid character '{Current}' in this context! {_position}");
            }
        }

        private void ExclamationMarkTagState()
        {
            Next();
            if (Current == '-') // <!-...
            {
                Next();
                if (Current == '-') // <!--...
                    BeforeCommentState();
                else
                    BogusCommentState();
            }
            else if (Current == 'D') // <!D...
                DoctypeState();
            else
                BogusCommentState();
        }

        private void BeforeClosingTagState()
        {
            Next();

            if (Current == ' ')
                BeforeClosingTagState();
            else if (Current == '>')
                ParseError($"Invalid closing tag! {_position}");
            else if (char.IsLetter(Current) || char.IsDigit(Current))
            {
                _closingTagName.Clear();
                _closingTagName.Add(Current);
                ClosingTagNameState();
            }
        }

        private void ClosingTagNameState()
        {
            Next();

            if (char.IsLetter(Current) || char.IsDigit(Current))
            {
                _closingTagName.Add(Current);
                ClosingTagNameState();
            }
            else if (Current == '>')
            {
                var text = _markup.Substring(_start, _position - _start + 1);
                var tagName = Stringify(_closingTagName);
                
                _tokens.Add(new Token(TokenKind.ClosingTagToken, tagName, null, text, _start));
                Next();
                
                if (tagName == "script" || tagName == "style")
                    _pauseParsing = false;
            }
        }        

        private void DoctypeState() 
        //  <!DOCTYPE html>
        //  <!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.01//EN" "http://www.w3.org/TR/html4/strict.dtd">
        {
            string text = Next(7);
            if (text == "DOCTYPE")
            {
                _position += "DOCTYPE".Length;
                
                if (Current == ' ')
                    DoctypeValueState();
                else
                    ParseError("Invalid DOCTYPE section!");
            }
            else
                ParseError("Invalid DOCTYPE section!");

        }

        private void DoctypeValueState()
        {
            Next();
            if (Current == ' ')
                DoctypeValueState();
            else if (Current == '>')
            {
                var text = _markup.Substring(_start, _position - _start + 1);
                _tokens.Add(new Token(TokenKind.DoctypeToken, "DOCTYPE", null, text, _start));
                Next();
            }
            else
                DoctypeValueState();
        }

        private void BogusCommentState()
        {
            Next();

            if (Current == '>')
            {
                var text = _markup.Substring(_start, _position - _start + 1);
                _tokens.Add(new Token(TokenKind.BogusCommentToken, "wrong comment", null, text, _start));
                Next();
            }
        }

        private void BeforeCommentState()
        {
            Next();
            if (Current == ' ')
                BeforeCommentState();
            else if (Current == '-')
                AfterCommentState();
            else
                CommentState();
        }

        private void CommentState()
        {
            Next();

            if (Current == '-')
                AfterCommentState();
            else
                CommentState();
        }

        private void AfterCommentState()
        {
            Next();

            if (Current == '-')
                AfterCommentState();
            else if (Current == '>')
            {
                var text = _markup.Substring(_start, _position - _start + 1);
                _tokens.Add(new Token(TokenKind.CommentToken, "text comment", null, text, _start));
                Next();
            }
            else
                CommentState();
        }

        private void TagNameState()
        {
            Next();

            if (char.IsLetter(Current) || char.IsDigit(Current) || Current == '-') // Letters and minus sign(for custom html-elements)
            {
                _openTagName.Add(Current);
                TagNameState();
            }
            else if (Current == '>') // Create open tag token
            {
                var text = _markup.Substring(_start, _position - _start + 1);
                var tagName = Stringify(_openTagName);

                _tokens.Add(new Token(TokenKind.OpenTagToken, tagName, DictCopy(_attrs), text, _start));
                Next();
                _attrs.Clear();

                if (tagName == "script" || tagName == "style")
                    _pauseParsing = true;
            }

            else if (Current == ' ') // If tag has attributes
                AfterTagNameState();

            else if (Current == '/') // Looks like self-closing
                AfterSelfClosingTagState();

            else
                ParseError($"Bad token name: char '{Current}' must not contains in tag name! {_position}");
        }

        private void AfterTagNameState()
        {
            Next();

            if (Current == ' ') // One or more spaces after tag name
                AfterTagNameState();
            else if (Current == '>') // Create open tag token
            {
                var text = _markup.Substring(_start, _position - _start + 1);
                var tagName = Stringify(_openTagName);

                _tokens.Add(new Token(TokenKind.OpenTagToken, tagName, DictCopy(_attrs), text, _start));
                Next();
                _attrs.Clear();

                if (tagName == "script" || tagName == "style")
                    _pauseParsing = true;
            }
            else if (char.IsLetter(Current))
            {
                _attrName.Add(Current);
                AttrNameState();
            }
            else if (Current == '/')
                AfterSelfClosingTagState();
            else
                ParseError($"Invalid character '{Current}' in this context! {_position}");
        }

        private void AttrNameState()
        {
            Next();
            if (Current == '-' || char.IsLetter(Current)) // Допскаем что имя атрибута может содержать тире
            {
                _attrName.Add(Current);
                AttrNameState();
            }
            else if (Current == ' ') // Считаем что после имени атрибута может быть один или несколько пробелов
                AfterAttrNameState();
            else if (Current == '=') // Имя аттрибута закончилось
                BeforeAttrValueState();
            else
                ParseError($"Invalid character '{Current}' in attr-name! {_position}");
        }

        private void AfterAttrNameState()
        {
            Next();

            if (Current == ' ') // Считаем что после имени атрибута может быть один или несколько пробелов, пропускаем их
                AfterAttrNameState();
            else if (Current == '/' || char.IsLetter(Current)) // Если "/" - значит самозакрывающийся тэг, если символ - значит начало имени след атрибута
            {
                _attrs.Add(Stringify(_attrName), ["True".ToString()]);
                _attrName.Clear();
                _attrName.Add(Current);

                if (Current == '/')
                    AfterSelfClosingTagState();
                else
                    AttrNameState();
            }
            else if (Current == '=') // Считаем что имя атрибута закончено
                BeforeAttrValueState();
            else
                ParseError($"Invalid character '{Current}' before attr-name! {_position}");
        }

        private void BeforeAttrValueState()
        {
            Next();
            if (Current == ' ') // Считаем что перед значением атрибута может быть один или несколько пробелов, пропускаем их
                BeforeAttrValueState();
            else if (Current == '"') // Ждем значения атрибута в двойных кавычках
                DoubleQuotedAttrValueState();
            else if (Current == '\'') // Ждем значения атрибута в одинарных кавычках
                SingleQuotedAttrValueState();
            else if (char.IsLetter(Current)) // Ждем значения атрибута без кавычек
                UnquotedAttrValueState();
            else
                ParseError($"Invalid character '{Current}' before attr-value! {_position}");
        }

        private void UnquotedAttrValueState()
        {
            Next();

            if (Current == ' ')
                AfterUnquotedAttrValueState();
            else
            {
                _attrValue.Add(Current);
                UnquotedAttrValueState();
            }
        }

        private void AfterUnquotedAttrValueState()
        {
            throw new NotImplementedException();
        }

        private void SingleQuotedAttrValueState()
        {
            Next();

            if (Current == '\'')
            {
                var attrName = Stringify(_attrName);
                var attrValue = Stringify(_attrValue);

                if (attrName == "class")
                    _attrs.Add(attrName, attrValue.Split());
                else
                    _attrs.Add(attrName, [attrValue]);
                _attrName.Clear();
                _attrValue.Clear();
                AfterTagNameState();
            }
            else
            {
                _attrValue.Add(Current);
                SingleQuotedAttrValueState();
            }
        }

        private void DoubleQuotedAttrValueState()
        {
            Next();

            if (Current == '"')
            {
                var attrName = Stringify(_attrName);
                var attrValue = Stringify(_attrValue);

                if (attrName == "class")
                    _attrs.Add(attrName, attrValue.Split());
                else
                    _attrs.Add(attrName, [attrValue]);
                _attrName.Clear();
                _attrValue.Clear();
                AfterTagNameState();
            }
            else
            {
                _attrValue.Add(Current);
                DoubleQuotedAttrValueState();
            }
        }

        private void AfterSelfClosingTagState()
        {
            Next();

            if (Current == ' ')
                AfterSelfClosingTagState();
            else if (Current == '>')
            {
                var text = _markup.Substring(_start, _position - _start + 1);
                _tokens.Add(new Token(TokenKind.SelfClosingTagToken, Stringify(_openTagName), DictCopy(_attrs), text, _start));
                _attrs.Clear();
                Next();
            }
            else
                ParseError($"Invalid character '{Current}' after '/'!");
        }

        private void ParseError(string message)
        {
            Console.WriteLine(message);
            Console.ReadKey();
            throw new Exception(message);
        }

    }
}
