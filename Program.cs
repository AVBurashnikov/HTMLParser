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
        DoctypeToken,
        OpenTagToken,
        ClosingTagToken,
        SelfClosingTagToken,
        ContentToken,
        CommentToken,
        BogusCommentToken,
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

        private void CommitToken(TokenKind kind)
        {
            var text = _markup.Substring(_start, _position - _start);

            switch(kind)
            {
                case TokenKind.DoctypeToken:
                    _tokens.Add(new Token(kind, "DOCTYPE", null, text, _start));
                    break;
                case TokenKind.OpenTagToken:
                    _tokens.Add(new Token(kind, _openTagName.ToString(), DictCopy(_attrs), text, _start));
                    _attrs.Clear();
                    break;
                case TokenKind.ClosingTagToken:
                    _tokens.Add(new Token(kind, _closingTagName.ToString(), null, text, _start));
                    break;
                case TokenKind.SelfClosingTagToken:
                    _tokens.Add(new Token(kind, _openTagName.ToString(), DictCopy(_attrs), text, _start));
                    _attrs.Clear();
                    break;
                case TokenKind.ContentToken:
                    _tokens.Add(new Token(kind, "text element", null, text.Trim(), _start));
                    break;
                case TokenKind.CommentToken:
                    _tokens.Add(new Token(kind, "text comment", null, text, _start));
                    break;
                case TokenKind.BogusCommentToken:
                    _tokens.Add(new Token(kind, "wrong comment", null, text, _start));
                    break;
            }
        }

        private void CommitAttribute()
        {
            var name = _attrName.ToString();

            var raw_value = _attrValue.ToString();
            var value = string.IsNullOrEmpty(raw_value) ? "true" : raw_value;

            // TODO: duplicate attr names (sometimes happens)
            if (!_attrs.ContainsKey(name))
            {
                if (name.Equals("class"))
                    _attrs.Add(name, value.Split());
                else
                    _attrs.Add(name, [value]);
            }

            _attrName.Clear();
            _attrValue.Clear();
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

        private char Next
        {
            get
            {
                if (_position + 1 >= _markup.Length)
                    return EOF;

                return _markup[_position + 1];
            }
        }

        private void StepBack() => _position--;

        private void StepForward() => _position++;

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
                StepForward();
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
            StepForward();

            while (true)
            {
                if (Current == EOF)
                {
                    CommitToken(TokenKind.ContentToken);
                    break;
                }
                else if (Current == LT && Next == F_SLASH)
                {
                    StepBack();
                    CommitToken(TokenKind.ContentToken);

                    StepForward();
                    _start = _position;
                    StepForward();
                    BeforeClosingTagState();
                    break;
                }
                else
                    StepForward();
            }
        }

        private void BeforeTagState()
        {
            StepForward();

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
            StepForward();
            if (Current == DASH) // <!-...
            {
                StepForward();
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
            StepForward();

            if (char.IsWhiteSpace(Current))
                BeforeClosingTagState();
            else if (char.IsLetterOrDigit(Current))
            {
                _closingTagName.Clear();
                _closingTagName.Append(Current);
                ClosingTagNameState();
            }
            else if (Current == GT)
                ParseError($"Invalid closing tag '{_markup.Substring(_position - 10, 20)}'!");
        }

        private void ClosingTagNameState()
        {
            StepForward();

            if (char.IsLetterOrDigit(Current))
            {
                _closingTagName.Append(Current);
                ClosingTagNameState();
            }
            else if (Current == GT)
            {
                StepForward();
                CommitToken(TokenKind.ClosingTagToken);
            }
        }        

        private void DoctypeState() 
        {
            string part = _markup.Substring(_position, DOCTYPE.Length).ToUpper();

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
            StepForward();
            if (char.IsWhiteSpace(Current))
                DoctypeValueState();
            else if (Current == GT)
            {
                StepForward();
                CommitToken(TokenKind.DoctypeToken);
                
            }
            else
                DoctypeValueState();
        }

        private void BogusCommentState()
        {
            StepForward();

            if (Current == GT)
            {
                StepForward();
                CommitToken(TokenKind.BogusCommentToken);
            }
        }

        private void BeforeCommentState()
        {
            StepForward();
            if (char.IsWhiteSpace(Current))
                BeforeCommentState();
            else if (Current == DASH)
                AfterCommentState();
            else
                CommentState();
        }

        private void CommentState()
        {
            StepForward();

            if (Current == DASH)
                AfterCommentState();
            else
                CommentState();
        }

        private void AfterCommentState()
        {
            StepForward();

            if (Current == DASH)
                AfterCommentState();
            else if (Current == GT)
            {
                StepForward();
                CommitToken(TokenKind.CommentToken);
            }
            else
                CommentState();
        }

        private void TagNameState()
        {
            StepForward();

            if (char.IsLetterOrDigit(Current) || Current == DASH)
            {
                _openTagName.Append(Current);
                TagNameState();
            }
            else if (Current == GT)
            {
                StepForward();
                CommitToken(TokenKind.OpenTagToken);
            }

            else if (char.IsWhiteSpace(Current))
                AfterTagNameState();

            else if (Current == F_SLASH)
                AfterSelfClosingTagState();

            else
                ParseError($"Error tag name parsing '{_markup.Substring(_position - 10, 20)}'");
        }

        private void AfterTagNameState()
        {
            StepForward();

            if (char.IsWhiteSpace(Current))
                AfterTagNameState();
            else if (Current == GT)
            {
                StepForward();
                CommitToken(TokenKind.OpenTagToken);
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
            StepForward();
            if (char.IsLetterOrDigit(Current) || Current == DASH || Current == UNDERSCORE || Current == COLON)
            {
                _attrName.Append(Current);
                AttrNameState();
            }
            else if (Current == GT)
            {
                StepForward();
                CommitAttribute();
                CommitToken(TokenKind.OpenTagToken);
            }
            else if (char.IsWhiteSpace(Current) || Current == F_SLASH)
            {
                StepBack();
                AfterAttrNameState();
            }
            else if (Current == EQUALS) // Имя аттрибута закончилось
                BeforeAttrValueState();
            else
                ParseError($"Error attr name parsing '{_markup.Substring(_position - 10, 20)}'!");
        }

        private void AfterAttrNameState()
        {
            StepForward();

            if (char.IsWhiteSpace(Current))
                AfterAttrNameState();
            else if (Current == F_SLASH || char.IsLetter(Current) || Current == GT)
            {
                CommitAttribute();

                if (Current == F_SLASH)
                    AfterSelfClosingTagState();
                else
                {
                    StepBack();
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
            StepForward();
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
            StepForward();

            if (char.IsWhiteSpace(Current))
            {
                CommitAttribute();
                AfterTagNameState();
            }
            else if (Current == GT)
            {
                StepForward();
                CommitAttribute();
                CommitToken(TokenKind.OpenTagToken);
            }
            else
            {
                _attrValue.Append(Current);
                UnquotedAttrValueState();
            }
        }

        private void SingleQuotedAttrValueState()
        {
            StepForward();

            while (Current != SINGLE_QUOTE)
            {
                _attrValue.Append(Current);
                StepForward();
            }

            CommitAttribute();
            AfterTagNameState();            
        }

        private void DoubleQuotedAttrValueState()
        {
            StepForward();

            while (Current != DOUBLE_QUOTE)
            {
                _attrValue.Append(Current);
                StepForward();
            }

            CommitAttribute();
            AfterTagNameState();
        }

        private void AfterSelfClosingTagState()
        {
            StepForward();

            if (char.IsWhiteSpace(Current))
                AfterSelfClosingTagState();
            else if (Current == GT)
            {
                StepForward();
                CommitToken(TokenKind.SelfClosingTagToken);
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
