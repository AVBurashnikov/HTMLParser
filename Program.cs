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

            builder.Append($"{Kind}: {TagName}, position='{Position}' text='{Text}'\n");

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
        private readonly string DOCTYPE = "DOCTYPE";
        private readonly char EOF = '\0';
        private readonly char GT = '>';
        private readonly char LT = '<';
        private readonly char EXCLAMATION_MARK = '!';
        private readonly char EQUALS = '=';
        private readonly char SINGLE_QUOTE = '\'';
        private readonly char DOUBLE_QUOTE = '"';
        private readonly char UNDERSCORE = '_';
        private readonly char F_SLASH = '/';
        private readonly char B_SLASH = '\\';
        private readonly char DASH = '-';
        private readonly char COLON = ':';
        private readonly char SHARP = '#';

        private string _markup = string.Empty;
        private int _start = 0;
        private int _position = 0;
        private List<Token> _tokens = [];
        private Dictionary<string, string[]> _attrs = [];
        private StringBuilder _openTagName = new ();
        private StringBuilder _closingTagName = new();
        private StringBuilder _attrName = new();
        private StringBuilder _attrValue = new();

        public List<Token> Parse(string markup) 
        {
            if (string.IsNullOrEmpty(markup))
                return [];
            
            _markup = markup;

            while (Current != EOF)
            {
                DataState();
            }

            return _tokens;
        }

        private char Current 
        {
            get 
            {
                if (_position >= _markup.Length) 
                    return EOF;
                
                return _markup[_position];
            }
        }

        private void PrevChar() => _position--;

        private void NextChar() => _position++;

        private string NextChar(int offset)
        {
            if (_position + offset < _markup.Length)
                return _markup.Substring(_position, offset);
            else
                return _markup.Substring(_position);
        }

        private static Dictionary<string, string[]>? DictCopy(Dictionary<string, string[]> dict)
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

        private void DataState()
        {
            if (char.IsWhiteSpace(Current))
            {
                NextChar();
                if (Current != EOF)
                    DataState();
            }
            else if (Current == LT)
            {
                _start = _position;
                BeforeTagState();
            }
            else
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
                if (Current == EOF)
                {
                    var text = _markup.Substring(_start, _position - _start);
                    _tokens.Add(new Token(TokenKind.ContentToken, "text element", null, text.Trim(), _start));
                    break;
                }
                if (Current == LT)
                {
                    NextChar();

                    if (Current == F_SLASH)
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

            if (Current == EXCLAMATION_MARK) // <!--... , <!DOCTYPE..., <!...  
            {
                ExclamationMarkTagState();
            }
            else if (Current == F_SLASH) // </...
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
            if (Current == DASH) // <!-...
            {
                NextChar();
                if (Current == DASH) // <!--...
                    BeforeCommentState();
                else
                    BogusCommentState();
            }
            else if ("dD".Contains(Current)) // <!D... || <!d...
                DoctypeState();
            else
                BogusCommentState();
        }

        private void BeforeClosingTagState()
        {
            NextChar();

            if (char.IsWhiteSpace(Current))
                BeforeClosingTagState();
            else if (Current == GT)
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

            if (char.IsLetterOrDigit(Current))
            {
                _closingTagName.Append(Current);
                ClosingTagNameState();
            }
            else if (Current == GT)
            {
                var text = _markup.Substring(_start, _position - _start + 1);
                
                _tokens.Add(new Token(TokenKind.ClosingTagToken, _closingTagName.ToString(), null, text, _start));
                NextChar();
            }
        }        

        private void DoctypeState() 
        {
            string part = NextChar(DOCTYPE.Length).ToUpper();

            if (part.Equals(DOCTYPE))
            {
                _position += DOCTYPE.Length;
                
                if (char.IsWhiteSpace(Current))
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
            if (char.IsWhiteSpace(Current))
                DoctypeValueState();
            else if (Current == GT)
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

            if (Current == GT)
            {
                var text = _markup.Substring(_start, _position - _start + 1);
                _tokens.Add(new Token(TokenKind.BogusCommentToken, "wrong comment", null, text, _start));
                NextChar();
            }
        }

        private void BeforeCommentState()
        {
            NextChar();
            if (char.IsWhiteSpace(Current))
                BeforeCommentState();
            else if (Current == DASH)
                AfterCommentState();
            else
                CommentState();
        }

        private void CommentState()
        {
            NextChar();

            if (Current == DASH)
                AfterCommentState();
            else
                CommentState();
        }

        private void AfterCommentState()
        {
            NextChar();

            if (Current == DASH)
                AfterCommentState();
            else if (Current == GT)
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

            if (char.IsLetterOrDigit(Current) || Current == DASH) // Letters and minus sign(for custom html-elements)
            {
                _openTagName.Append(Current);
                TagNameState();
            }
            else if (Current == GT) // Create open tag token
            {
                var text = _markup.Substring(_start, _position - _start + 1);

                _tokens.Add(new Token(TokenKind.OpenTagToken, _openTagName.ToString(), DictCopy(_attrs), text, _start));
                NextChar();
                _attrs.Clear();
            }

            else if (char.IsWhiteSpace(Current)) // If tag has attributes
                AfterTagNameState();

            else if (Current == F_SLASH) // Looks like self-closing
                AfterSelfClosingTagState();

            else
                ParseError($"Error tag name parsing '{_markup.Substring(_position - 10, 20)}'");
        }

        private void AfterTagNameState()
        {
            NextChar();

            if (char.IsWhiteSpace(Current)) // One or more spaces after tag name
                AfterTagNameState();
            else if (Current == GT) // Create open tag token
            {
                var text = _markup.Substring(_start, _position - _start + 1);

                _tokens.Add(new Token(TokenKind.OpenTagToken, _openTagName.ToString(), DictCopy(_attrs), text, _start));
                NextChar();
                _attrs.Clear();
            }
            else if (char.IsLetter(Current))
            {
                _attrName.Append(Current);
                AttrNameState();
            }
            else if (Current == F_SLASH)
                AfterSelfClosingTagState();
            else
                ParseError($"Error after tag parsing '{_markup.Substring(_position - 10, 20)}'!");
        }

        private void AttrNameState()
        {
            NextChar();
            if (char.IsLetterOrDigit(Current) || Current == DASH || Current == UNDERSCORE || Current == COLON)
            {
                _attrName.Append(Current);
                AttrNameState();
            }
            else if (Current == GT)
            {
                var text = _markup.Substring(_start, _position - _start + 1);

                _attrs.Add(_attrName.ToString(), ["true"]);
                _tokens.Add(new Token(TokenKind.OpenTagToken, _openTagName.ToString(), DictCopy(_attrs), text, _start));
                _attrName.Clear();
                _attrs.Clear();
                NextChar();
            }
            else if (char.IsWhiteSpace(Current) || Current == F_SLASH)
            {
                PrevChar();
                AfterAttrNameState();
            }
            else if (Current == EQUALS) // Имя аттрибута закончилось
                BeforeAttrValueState();
            else
                ParseError($"Error attr name parsing '{_markup.Substring(_position - 10, 20)}'!");
        }

        private void AfterAttrNameState()
        {
            NextChar();

            if (char.IsWhiteSpace(Current))
                AfterAttrNameState();
            else if (Current == F_SLASH || char.IsLetter(Current) || Current == GT)
            {
                _attrs.Add(_attrName.ToString(), ["true"]);
                _attrName.Clear();
                _attrName.Append(Current);

                if (Current == F_SLASH)
                    AfterSelfClosingTagState();
                else
                {
                    PrevChar();
                    AttrNameState();
                }
            }
            else if (Current == EQUALS) // Считаем что имя атрибута закончено
                BeforeAttrValueState();
            else
                ParseError($"Error after attr name parsing '{_markup.Substring(_position - 10, 20)}'! {_position}");
        }

        private void BeforeAttrValueState()
        {
            NextChar();
            if (char.IsWhiteSpace(Current)) // Считаем что перед значением атрибута может быть один или несколько пробелов, пропускаем их
                BeforeAttrValueState();
            else if (Current == DOUBLE_QUOTE) // Ждем значения атрибута в двойных кавычках
                DoubleQuotedAttrValueState();
            else if (Current == SINGLE_QUOTE) // Ждем значения атрибута в одинарных кавычках
                SingleQuotedAttrValueState();
            else if (char.IsLetterOrDigit(Current) || Current == SHARP || Current == F_SLASH) // Ждем значения атрибута без кавычек
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

            if (char.IsWhiteSpace(Current))
            {
                _attrs.Add(_attrName.ToString(), [_attrValue.ToString()]);
                _attrName.Clear();
                _attrValue.Clear();
                AfterTagNameState();
            }
            else if (Current == GT)
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

        // ToDo: stack overflow issue
        private void SingleQuotedAttrValueState()
        {
            NextChar();

            while (Current != SINGLE_QUOTE)
            {
                _attrValue.Append(Current);
                NextChar();
            }

            var attrName = _attrName.ToString();
            var attrValue = _attrValue.ToString();

            if (attrName == "class")
                _attrs.Add(attrName, attrValue.Split());
            else
                _attrs.Add(attrName, [attrValue]);
            _attrName.Clear();
            _attrValue.Clear();
            AfterTagNameState();

            /*if (Current == SINGLE_QUOTE)
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
            }*/
        }

        // ToDo: stack overflow issue
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

            if (char.IsWhiteSpace(Current))
                AfterSelfClosingTagState();
            else if (Current == '>')
            {
                var text = _markup.Substring(_start, _position - _start + 1);
                _tokens.Add(new Token(TokenKind.SelfClosingTagToken, 
                                      _openTagName.ToString(), 
                                      DictCopy(_attrs), 
                                      text, 
                                      _start));
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
