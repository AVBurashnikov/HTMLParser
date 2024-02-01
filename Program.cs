using System.Text;

namespace htmlParser
{
    class Program
    {
        public static void Main()
        {
            var startTime = Environment.TickCount;
            var sr = new StreamReader("file.html");
            var data = sr.ReadToEnd();
            var tokenizer = new Tokenizer();
            var tokens = tokenizer.Parse(data);

            foreach(var token in tokens)
                Console.WriteLine(token);
            Console.WriteLine("-------------------------");
            Console.WriteLine($"Program timing is: {Environment.TickCount - startTime}ms");
            Console.ReadKey();
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
            var builder = new StringBuilder();

            builder.Append($"{Kind}: {TagName}, position={Position}\n");

            if (Attributes is not null)
            {
                builder.Append($"Attributes:\n");
                foreach(KeyValuePair<string, string[]> attr in Attributes)
                {
                    builder.Append($"\t{attr.Key.ToString()} = ");

                    if (attr.Key == "class")
                    {
                        builder.Append("[ ");
                        foreach (string attrValue in attr.Value)
                            builder.Append($"'{attrValue}', ");
                        builder.Append("]\n");
                    }
                    else
                        builder.Append($"'{attr.Value[0]}'\n");
                }
            }

            builder.Append('\n');

            return builder.ToString();
        }
    }

    class Tokenizer
    {
        private string _markup = string.Empty;
        private int _start = 0;
        private int _position = 0;
        private bool _stopParsing = false;

        private List<Token> _tokens = [];
        private Dictionary<string, string[]> _attrs = [];
        private StringBuilder _openTagName = new ();
        private StringBuilder _closingTagName = new();
        private StringBuilder _attrName = new();
        private StringBuilder _attrValue = new();

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

        private int NextChar() => _position++;

        private int PrevChar() => _position--;

        private string NextChar(int offset)
        {
            if (_position + offset < _markup.Length)
                return _markup.Substring(_position, offset);
            else
                return _markup.Substring(_position);
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

            while (true)
            {
                if (Current == '\0')
                    break;
                DataState();
            }

            return _tokens;
        }

        private void DataState()
        {
            if (Current == '\n' || Current == '\r' || Current == '\t' || Current == ' ')
            {
                NextChar();
                DataState();
            }
            else if (!_stopParsing && Current == '<') // <...
            {
                _start = _position;
                BeforeTagState();
            }
            //else if (_stopParsing && Current == '<')
            //{
            //    NextChar();
            //    if (Current == '/')
            //    {
            //        _stopParsing = false;
            //        BeforeClosingTagState();
            //    }
            //}
            else // Any text content
            {
                _start = _position;
                ContentState();
            }
        }

        private void ContentState()
        {
            NextChar();

            while (true)
            {
                if (Current == '\0')
                    break;

                if (Current == '<')
                {
                    NextChar();

                    if (Current == '/')
                    {
                        var text = _markup.Substring(_start, _position - _start - 1);
                        _tokens.Add(new Token(TokenKind.ContentToken, "text element", null, text.Trim(), _start));

                        _start = _position - 1;
                        BeforeClosingTagState();
                        break;
                    }
                }
                NextChar();
            }
        }

        private void BeforeTagState()
        {
            NextChar();

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
                _openTagName.Append(Current);  
                TagNameState();
            }
            else
            {
                ParseError($"Error tag parsing '{_markup.Substring(_position-10, 20)}'");
            }
        }

        private void ExclamationMarkTagState()
        {
            NextChar();
            if (Current == '-') // <!-...
            {
                NextChar();
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
            NextChar();

            if (Current == ' ')
                BeforeClosingTagState();
            else if (Current == '>')
                ParseError($"Invalid closing tag '{_markup.Substring(_position - 10, 20)}'!");
            else if (char.IsLetter(Current) || char.IsDigit(Current))
            {
                _closingTagName.Clear();
                _closingTagName.Append(Current);
                ClosingTagNameState();
            }
        }

        private void ClosingTagNameState()
        {
            NextChar();

            if (char.IsLetter(Current) || char.IsDigit(Current))
            {
                _closingTagName.Append(Current);
                ClosingTagNameState();
            }
            else if (Current == '>')
            {
                var text = _markup.Substring(_start, _position - _start + 1);
                
                _tokens.Add(new Token(TokenKind.ClosingTagToken, _closingTagName.ToString(), null, text, _start));
                NextChar();
            }
        }        

        private void DoctypeState() 
        //  <!DOCTYPE html>
        //  <!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.01//EN" "http://www.w3.org/TR/html4/strict.dtd">
        {
            string text = NextChar(7);
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
            NextChar();
            if (Current == ' ')
                DoctypeValueState();
            else if (Current == '>')
            {
                var text = _markup.Substring(_start, _position - _start + 1);
                _tokens.Add(new Token(TokenKind.DoctypeToken, "DOCTYPE", null, text, _start));
                NextChar();
            }
            else
                DoctypeValueState();
        }

        private void BogusCommentState()
        {
            NextChar();

            if (Current == '>')
            {
                var text = _markup.Substring(_start, _position - _start + 1);
                _tokens.Add(new Token(TokenKind.BogusCommentToken, "wrong comment", null, text, _start));
                NextChar();
            }
        }

        private void BeforeCommentState()
        {
            NextChar();
            if (Current == ' ')
                BeforeCommentState();
            else if (Current == '-')
                AfterCommentState();
            else
                CommentState();
        }

        private void CommentState()
        {
            NextChar();

            if (Current == '-')
                AfterCommentState();
            else
                CommentState();
        }

        private void AfterCommentState()
        {
            NextChar();

            if (Current == '-')
                AfterCommentState();
            else if (Current == '>')
            {
                var text = _markup.Substring(_start, _position - _start + 1);
                _tokens.Add(new Token(TokenKind.CommentToken, "text comment", null, text, _start));
                NextChar();
            }
            else
                CommentState();
        }

        private void TagNameState()
        {
            NextChar();

            if (char.IsLetter(Current) || char.IsDigit(Current) || Current == '-') // Letters and minus sign(for custom html-elements)
            {
                _openTagName.Append(Current);
                TagNameState();
            }
            else if (Current == '>') // Create open tag token
            {
                var text = _markup.Substring(_start, _position - _start + 1);

                _tokens.Add(new Token(TokenKind.OpenTagToken, _openTagName.ToString(), DictCopy(_attrs), text, _start));
                NextChar();
                _attrs.Clear();

                //if (tagName == "script" || tagName == "style")
                //    _stopParsing = true;
            }

            else if (Current == ' ' || Current == '\n' || Current == '\r' || Current == '\t') // If tag has attributes
                AfterTagNameState();

            else if (Current == '/') // Looks like self-closing
                AfterSelfClosingTagState();

            else
                ParseError($"Error tag name parsing '{_markup.Substring(_position - 10, 20)}'");
        }

        private void AfterTagNameState()
        {
            NextChar();

            if (Current == ' ' || Current == '\n' || Current == '\t' || Current == '\r') // One or more spaces after tag name
                AfterTagNameState();
            else if (Current == '>') // Create open tag token
            {
                var text = _markup.Substring(_start, _position - _start + 1);

                _tokens.Add(new Token(TokenKind.OpenTagToken, _openTagName.ToString(), DictCopy(_attrs), text, _start));
                NextChar();
                _attrs.Clear();

                //if (tagName == "script" || tagName == "style")
                //    _stopParsing = true;
            }
            else if (char.IsLetter(Current))
            {
                _attrName.Append(Current);
                AttrNameState();
            }
            else if (Current == '/')
                AfterSelfClosingTagState();
            else
                ParseError($"Error after tag parsing '{_markup.Substring(_position - 10, 20)}'!");
        }

        private void AttrNameState()
        {
            NextChar();
            if (Current == '-' || char.IsLetter(Current) || Current == '_' || Current == ':') // Допскаем что имя атрибута может содержать тире
            {
                _attrName.Append(Current);
                AttrNameState();
            }
            else if (Current == '>')
            {
                var text = _markup.Substring(_start, _position - _start + 1);

                _attrs.Add(_attrName.ToString(), ["true"]);
                _tokens.Add(new Token(TokenKind.OpenTagToken, _openTagName.ToString(), DictCopy(_attrs), text, _start));
                _attrName.Clear();
                _attrs.Clear();
                NextChar();
            }
            else if (Current == ' ' || Current == '\n' || Current == '\r' || Current == '\t' || Current == '/')
            {
                PrevChar();
                AfterAttrNameState();
            }
            else if (Current == '=') // Имя аттрибута закончилось
                BeforeAttrValueState();
            else
                ParseError($"Error attr name parsing '{_markup.Substring(_position - 10, 20)}'!");
        }

        private void AfterAttrNameState()
        {
            NextChar();

            if (Current == ' ' || Current == '\n' || Current == '\r' || Current == '\t')
                AfterAttrNameState();
            else if (Current == '/' || char.IsLetter(Current) || Current == '>')
            {
                _attrs.Add(_attrName.ToString(), ["true"]);
                _attrName.Clear();
                _attrName.Append(Current);

                if (Current == '/')
                    AfterSelfClosingTagState();
                else
                {
                    PrevChar();
                    AttrNameState();
                }
            }
            else if (Current == '=') // Считаем что имя атрибута закончено
                BeforeAttrValueState();
            else
                ParseError($"Error after attr name parsing '{_markup.Substring(_position - 10, 20)}'! {_position}");
        }

        private void BeforeAttrValueState()
        {
            NextChar();
            if (Current == ' ') // Считаем что перед значением атрибута может быть один или несколько пробелов, пропускаем их
                BeforeAttrValueState();
            else if (Current == '"') // Ждем значения атрибута в двойных кавычках
                DoubleQuotedAttrValueState();
            else if (Current == '\'') // Ждем значения атрибута в одинарных кавычках
                SingleQuotedAttrValueState();
            else if (char.IsLetterOrDigit(Current) || Current == '#' || Current == '/') // Ждем значения атрибута без кавычек
            {
                _attrValue.Append(Current);
                UnquotedAttrValueState();
            }
            else
                ParseError($"Error before attr name parsing '{_markup.Substring(_position - 10, 20)}'!");
        }

        private void UnquotedAttrValueState()
        {
            NextChar();

            if (Current == ' ' || Current == '\n' || Current == '\r' || Current == '\t')
            {
                _attrs.Add(_attrName.ToString(), [_attrValue.ToString()]);
                _attrName.Clear();
                _attrValue.Clear();
                AfterTagNameState();
            }
            else if (Current == '>')
            {
                _attrs.Add(_attrName.ToString(), [_attrValue.ToString()]);
                _attrName.Clear();
                _attrValue.Clear();

                var text = _markup.Substring(_start, _position - _start + 1);
                _tokens.Add(new Token(TokenKind.OpenTagToken, _openTagName.ToString(), DictCopy(_attrs), text, _start));
                _attrs.Clear();
                NextChar();
            }
            else
            {
                _attrValue.Append(Current);
                UnquotedAttrValueState();
            }
        }

        private void SingleQuotedAttrValueState()
        {
            NextChar();

            if (Current == '\'')
            {
                var attrName = _attrName.ToString();
                var attrValue = _attrValue.ToString();

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
                _attrValue.Append(Current);
                SingleQuotedAttrValueState();
            }
        }

        private void DoubleQuotedAttrValueState()
        {
            NextChar();

            if (Current == '"')
            {
                var attrName = _attrName.ToString();
                var attrValue = _attrValue.ToString();

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
                _attrValue.Append(Current);
                DoubleQuotedAttrValueState();
            }
        }

        private void AfterSelfClosingTagState()
        {
            NextChar();

            if (Current == ' ')
                AfterSelfClosingTagState();
            else if (Current == '>')
            {
                var text = _markup.Substring(_start, _position - _start + 1);
                _tokens.Add(new Token(TokenKind.SelfClosingTagToken, _openTagName.ToString(), DictCopy(_attrs), text, _start));
                _attrs.Clear();
                NextChar();
            }
            else
                ParseError($"Error selfclosing tag parsing '{_markup.Substring(_position-10, 20)}'!");
        }

        private void ParseError(string message)
        {
            Console.WriteLine(message);
            Console.ReadKey();
            throw new Exception(message);
        }

    }
}
