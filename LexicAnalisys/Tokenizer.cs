using System.Text;

namespace YellowOak.LexicAnalisys
{
    internal class Tokenizer
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
        private List<string> _diagnostics = [];

        private List<Token> _tokens = [];
        private Dictionary<string, string[]> _attrs = [];
        private StringBuilder _openTagName = new();
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
                StepForward();
            }

            return _tokens;
        }

        public List<string> Diagnostics { get { return _diagnostics; } }

        private void CommitToken(SyntaxKind kind)
        {
            var text = _markup.Substring(_start, _position - _start + 1);

            switch (kind)
            {
                case SyntaxKind.Doctype:
                    _tokens.Add(new Token(kind, "DOCTYPE", null, text, _start));
                    break;
                case SyntaxKind.OpenTag:
                    _tokens.Add(new Token(kind, _openTagName.ToString(), Attrs(), text, _start));
                    break;
                case SyntaxKind.ClosingTag:
                    _tokens.Add(new Token(kind, _closingTagName.ToString(), null, text, _start));
                    break;
                case SyntaxKind.SelfClosingTag:
                    _tokens.Add(new Token(kind, _openTagName.ToString(), Attrs(), text, _start));
                    break;
                case SyntaxKind.Content:
                    _tokens.Add(new Token(kind, "text", null, text.Trim(), _start));
                    break;
                case SyntaxKind.Comment:
                    _tokens.Add(new Token(kind, "comment", null, text, _start));
                    break;
                case SyntaxKind.BogusComment:
                    _tokens.Add(new Token(kind, "comment", null, text, _start));
                    break;
            }
        }

        private void CommitAttribute()
        {
            var name = _attrName.ToString();

            var raw_value = _attrValue.ToString();
            var value = string.IsNullOrEmpty(raw_value) ? "true" : raw_value;

            // TODO: duplicate attr names (sometimes )
            if (!_attrs.ContainsKey(name))
            {
                if (name.Equals("class"))
                    _attrs.Add(name, value.Trim().Split());
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

        private string PlaceOfErrorCut()
        {
            if (_position + 10 >= _markup.Length)
                return _markup[(_position - 10)..];
            else if (_position - 10 < 0)
                return _markup[..20];
            return _markup.Substring(_position - 10, 20);
        }

        private Dictionary<string, string[]>? Attrs()
        {
            if (_attrs.Count == 0)
                return null;

            var attrs = new Dictionary<string, string[]>();

            foreach (KeyValuePair<string, string[]> pair in _attrs)
                attrs.Add(pair.Key, pair.Value);

            _attrs.Clear();
            
            return attrs;
        }

        private void DataState()
        {
            if (Current == EOF) 
                return;
            if (char.IsWhiteSpace(Current))
            {
                StepForward();
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
                    CommitToken(SyntaxKind.Content);
                    break;
                }
                else if (Current == LT && Next == F_SLASH)
                {
                    StepBack();
                    CommitToken(SyntaxKind.Content);

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

            if (Current == EXCLAMATION_MARK) 
                ExclamationMarkTagState();
            else if (Current == F_SLASH)
                BeforeClosingTagState();
            else if (char.IsLetter(Current))
            {
                _openTagName.Clear();
                _openTagName.Append(Current);
                TagNameState();
            }
            else
                _diagnostics.Add($"Error: '{PlaceOfErrorCut()}'");
        }

        private void ExclamationMarkTagState()
        {
            StepForward();
            if (Current == DASH)
            {
                StepForward();
                if (Current == DASH)
                    BeforeCommentState();
                else
                    BogusCommentState();
            }
            else if ("dD".Contains(Current))
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
                _diagnostics.Add($"Error: '{PlaceOfErrorCut()}'");
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
                CommitToken(SyntaxKind.ClosingTag);
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
                    _diagnostics.Add($"Error: '{PlaceOfErrorCut()}'");
            }
            else
                _diagnostics.Add($"Error: '{PlaceOfErrorCut()}'");
        }

        private void DoctypeValueState()
        {
            StepForward();

            if (char.IsWhiteSpace(Current))
                DoctypeValueState();
            else if (Current == GT)
                CommitToken(SyntaxKind.Doctype);
            else
                DoctypeValueState();
        }

        private void BogusCommentState()
        {
            StepForward();

            if (Current == GT)
                CommitToken(SyntaxKind.BogusComment);
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
                CommitToken(SyntaxKind.Comment);
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
                CommitToken(SyntaxKind.OpenTag);
            else if (char.IsWhiteSpace(Current))
                AfterTagNameState();
            else if (Current == F_SLASH)
                AfterSelfClosingTagState();
            else
                _diagnostics.Add($"Error: '{PlaceOfErrorCut()}'");
        }

        private void AfterTagNameState()
        {
            StepForward();

            if (char.IsWhiteSpace(Current))
                AfterTagNameState();
            else if (Current == GT)
                CommitToken(SyntaxKind.OpenTag);
            else if (char.IsLetter(Current))
            {
                _attrName.Append(Current);
                AttrNameState();
            }
            else if (Current == F_SLASH)
                AfterSelfClosingTagState();
            else
                _diagnostics.Add($"Error: '{PlaceOfErrorCut()}'");
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
                CommitAttribute();
                CommitToken(SyntaxKind.OpenTag);
            }
            else if (char.IsWhiteSpace(Current) || Current == F_SLASH)
            {
                StepBack();
                AfterAttrNameState();
            }
            else if (Current == EQUALS) // Имя аттрибута закончилось
                BeforeAttrValueState();
            else
                _diagnostics.Add($"Error: '{PlaceOfErrorCut()}'");
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
                _diagnostics.Add($"Error: '{PlaceOfErrorCut()}'");
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
                _diagnostics.Add($"Error: '{PlaceOfErrorCut()}'");
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
                CommitAttribute();
                CommitToken(SyntaxKind.OpenTag);
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
                CommitToken(SyntaxKind.SelfClosingTag);
            else
                _diagnostics.Add($"Error: '{PlaceOfErrorCut()}'");
        }
    }
}
