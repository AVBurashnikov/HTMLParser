using System.Text;

namespace YellowOak.HTMLLexicAnalisys
{
    internal class Tokenizer
    {
        private readonly string[] _unpairedTags = [ "meta",
            "img", "link", "br", "hr", "input", "area", "param", "col", "base" ];

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
        private readonly List<string> _diagnostics = [];

        private readonly List<Token> _tokens = [];
        private AttributeList _attributes = [];
        private readonly StringBuilder _attributeName = new();
        private readonly StringBuilder _attributeValue = new();

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

        public List<string> Diagnostics { get => _diagnostics; }

        private void CommitToken(SyntaxKind kind)
        {
            var tag = "";
            var text = _markup.Substring(_start, _position - _start + 1);
            SyntaxKind tKind = kind;
            SyntaxKind[] syntaxTagKinds = [ SyntaxKind.OpenTag, 
                                            SyntaxKind.ClosingTag, 
                                            SyntaxKind.AutoClosingTag ];

            if (syntaxTagKinds.Contains(kind))
            {
                tag = CropTagName(text);
            }

            if (_unpairedTags.Contains(tag) &&
                SyntaxKind.OpenTag == kind)
            {
                tKind = SyntaxKind.AutoClosingTag;
            }
            else
            {
                tKind = kind;
            }

            switch (tKind)
            {
                case SyntaxKind.Doctype:
                    _tokens.Add(new Token(tKind, null, null, text));
                    break;
                case SyntaxKind.OpenTag:
                    _tokens.Add(new Token(tKind, tag, _attributes, text));
                    _attributes = [];
                    break;
                case SyntaxKind.ClosingTag:
                    _tokens.Add(new Token(tKind, tag, null, text));
                    break;
                case SyntaxKind.AutoClosingTag:
                    _tokens.Add(new Token(tKind, tag, _attributes, text));
                    _attributes = [];
                    break;
                case SyntaxKind.Content:
                    _tokens.Add(new Token(tKind, null, null, text.Trim()));
                    break;
                case SyntaxKind.Comment:
                    _tokens.Add(new Token(tKind, null, null, text));
                    break;
                case SyntaxKind.BogusComment:
                    _tokens.Add(new Token(tKind, null, null, text));
                    break;
            }
        }

        private static string CropTagName(string text) => text.Trim('<').Trim('>').Trim('/').Split()[0];

        
        
        private void CommitAttribute()
        {
            var attributeName = _attributeName.ToString();
            var attributeValue = _attributeValue.ToString();

            _attributeName.Clear();
            _attributeValue.Clear();
            
            if (attributeName.Equals("class"))
            {
                foreach (var value in attributeValue.Trim().Split())
                    _attributes.Add(new Attribute(attributeName, value));
            }
            else
                _attributes.Add(new Attribute(attributeName, attributeValue));            
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
                else if (Current == LT && (Next == F_SLASH || char.IsLetter(Next)))
                {
                    StepBack();
                    CommitToken(SyntaxKind.Content);

                    StepForward();
                    _start = _position;

                    if (char.IsLetter(Next))
                        BeforeTagState();
                    else
                    {
                        StepForward();
                        BeforeClosingTagState();
                    }
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
                TagNameState();
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
                ClosingTagNameState();
            else if (Current == GT)
                _diagnostics.Add($"Error: '{PlaceOfErrorCut()}'");
        }

        private void ClosingTagNameState()
        {
            StepForward();

            if (char.IsLetterOrDigit(Current))
                ClosingTagNameState();
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
                TagNameState();
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
                _attributeName.Append(Current);
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
            if (char.IsLetterOrDigit(Current) || 
                Current == DASH || 
                Current == UNDERSCORE || 
                Current == COLON)
            {
                _attributeName.Append(Current);
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
            else if (Current == EQUALS)
                BeforeAttrValueState();
            else
                _diagnostics.Add($"Error: '{PlaceOfErrorCut()}'");
        }

        private void AfterAttrNameState()
        {
            StepForward();

            if (char.IsWhiteSpace(Current))
                AfterAttrNameState();
            else if (Current == F_SLASH || 
                     char.IsLetter(Current) || 
                     Current == GT)
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
            else if (Current == EQUALS)
                BeforeAttrValueState();
            else
                _diagnostics.Add($"Error: '{PlaceOfErrorCut()}'");
        }

        private void BeforeAttrValueState()
        {
            StepForward();
            if (char.IsWhiteSpace(Current))
                BeforeAttrValueState();
            else if (Current == DOUBLE_QUOTE)
                DoubleQuotedAttrValueState();
            else if (Current == SINGLE_QUOTE)
                SingleQuotedAttrValueState();
            else if (char.IsLetterOrDigit(Current) || 
                     Current == SHARP || 
                     Current == F_SLASH)
            {
                _attributeValue.Append(Current);
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
                _attributeValue.Append(Current);
                UnquotedAttrValueState();
            }
        }

        private void SingleQuotedAttrValueState()
        {
            StepForward();

            while (Current != SINGLE_QUOTE)
            {
                _attributeValue.Append(Current);
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
                _attributeValue.Append(Current);
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
                CommitToken(SyntaxKind.AutoClosingTag);
            else
                _diagnostics.Add($"Error: '{PlaceOfErrorCut()}'");
        }
    }
}
