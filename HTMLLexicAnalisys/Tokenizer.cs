using System.Text;

namespace YellowOak.HTMLLexicAnalisys
{

    internal class Tokenizer
    {
        // Parsed markup
        private string _markup = string.Empty;

        // List of self-closing tags. Due to the peculiarities
        // of markup parsing, auto-closing tags are initially
        // classified as opening tags; this introduces an error
        // at the stage of constructing a node tree. Therefore,
        // later in the code, when creating an opening tag, a
        // check is made to see if this tag is in the list of
        // auto-closing tags.
        // For example: <input type="text"> will be recognized
        // as SyntaxKind.OpenTag, and the tree builder will assign
        // it as the parent element for subsequent opening tags,
        // and due to the fact that the input tag does not have a
        // closing tag, this will result in an error when building
        // the node tree .
        private readonly string[] _unpairedTags = [ "meta",
            "img", "link", "br", "hr", "input", 
            "area", "param", "col", "base" ];

        // Some auxiliary variables to make the code below easier to read.
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

        // Pointer to the start of the lexical token. Set during
        // markup analysis when certain character sequences are detected.
        private int _start = 0;

        // Current pointer position number.
        private int _position = 0;

        // An array for storing diagnostic information obtained
        // during lexical parsing of markup.
        //
        // TODO: The part of the application responsible
        // for processing warnings and errors needs
        // to be improved.
        private readonly List<string> _diagnostics = [];

        // An array for storing lexemes obtained during
        // the parsing process.
        private readonly List<Token> _tokens = [];

        // Variable for storing a list of attributes
        // of a given tag
        private AttributeList _attributes = [];

        // Auxiliary variables for obtaining the
        // attribute name and value during markup
        // lexical parsing. They are filled in during
        // the process of parsing the markup into
        // tokens and immediately after defining
        // the tag, they are saved to the list of
        // attributes, cleared and ready to be filled
        // with new values.
        private readonly StringBuilder _attributeName = new();
        private readonly StringBuilder _attributeValue = new();

        /// <summary>
        ///     Start lexical analysis until the 
        ///     end-of-file character is received.
        /// </summary>
        /// <param name="markup"> Parsed markup. </param>
        /// <returns> 
        ///     An array for storing lexemes 
        ///     obtained during the parsing process. 
        /// </returns>
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

        /// <summary>
        ///     Retrieving a list of parsing errors 
        ///     found during lexical parsing.
        ///     
        ///     TODO: The part of the application 
        ///     responsible for processing warnings 
        ///     and errors needs to be improved.
        /// </summary>
        public List<string> Diagnostics { get => _diagnostics; }

        /// <summary>
        ///     Creating a syntactic token 
        ///     instance based on lexical parsing.
        /// </summary>
        /// <param name="kind"> Syntactic type of lexical token. </param>
        private void CommitToken(SyntaxKind kind)
        {
            var tagName = "";
            var text = _markup.Substring(_start, _position - _start + 1);

            // Temporary variable to store
            // given syntactic token kind
            SyntaxKind tKind = kind;

            SyntaxKind[] syntaxKindsHavingTagName = [SyntaxKind.OpenTag, 
                                                     SyntaxKind.ClosingTag, 
                                                     SyntaxKind.AutoClosingTag];

            if (syntaxKindsHavingTagName.Contains(kind))
            {
                tagName = CropTagName(text);
            }

            if (_unpairedTags.Contains(tagName) &&
                SyntaxKind.OpenTag == kind)
            {
                tKind = SyntaxKind.AutoClosingTag;
            }
            else
            {
                tKind = kind;
            }

            // Instantiating a lexical token based on the
            // type received from the lexer
            switch (tKind)
            {
                case SyntaxKind.Doctype:
                    _tokens.Add(new Token(tKind, null, null, text));
                    break;
                case SyntaxKind.OpenTag:
                    _tokens.Add(new Token(tKind, tagName, _attributes, text));
                    _attributes = [];
                    break;
                case SyntaxKind.ClosingTag:
                    _tokens.Add(new Token(tKind, tagName, null, text));
                    break;
                case SyntaxKind.AutoClosingTag:
                    _tokens.Add(new Token(tKind, tagName, _attributes, text));
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

        /// <summary>
        ///     Retrieving the tag name from a 
        ///     piece of markup received from 
        ///     the lexical analyzer.
        /// </summary>
        /// <param name="text"> 
        ///     Markup received 
        ///     from the lexical analyzer. 
        /// </param>
        /// <returns> Cleaned tag name. </returns>
        private static string CropTagName(string text) => text.Trim('<').Trim('>').Trim('/').Split()[0];

        /// <summary>
        ///     Creating an instance of the Attribute 
        ///     class and adding a token to the list 
        ///     of attributes
        /// </summary>
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

        /// <summary>
        ///     Markup element at the current pointer position
        /// </summary>
        private char Current
        {
            get
            {
                if (_position >= _markup.Length)
                    return EOF;

                return _markup[_position];
            }
        }

        /// <summary>
        ///     Markup element at the next pointer position
        /// </summary>
        private char Next
        {
            get
            {
                if (_position + 1 >= _markup.Length)
                    return EOF;

                return _markup[_position + 1];
            }
        }

        /// <summary>
        ///     Position pointer increment
        /// </summary>
        private void StepBack() => _position--;

        /// <summary>
        ///     Position pointer decrement
        /// </summary>
        private void StepForward() => _position++;

        /// <summary>
        ///     Cutting out a section of marking with 
        ///     code that is incorrect according to 
        ///     the analyzer
        ///     
        ///     TODO: The part of the application 
        ///     responsible for processing warnings 
        ///     and errors needs to be improved.
        /// </summary>
        /// <returns></returns>
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
                    {
                        BeforeTagState();
                    }
                    else
                    {
                        StepForward();
                        BeforeClosingTagState();
                    }
                    break;
                }
                else
                {
                    StepForward();
                }
            }
        }

        private void BeforeTagState()
        {
            StepForward();

            if (Current == EXCLAMATION_MARK)
            {
                ExclamationMarkTagState();
            }
            else if (Current == F_SLASH)
            {
                BeforeClosingTagState();
            }
            else if (char.IsLetter(Current))
            {
                TagNameState();
            }
            else
            {
                _diagnostics.Add($"Error: '{PlaceOfErrorCut()}'");
            }
        }

        private void ExclamationMarkTagState()
        {
            StepForward();

            if (Current == DASH)
            {
                StepForward();
                if (Current == DASH)
                {
                    BeforeCommentState();
                }
                else
                {
                    BogusCommentState();
                }
            }
            else if ("dD".Contains(Current))
            {
                DoctypeState();
            }
            else
            {
                BogusCommentState();
            }
        }

        private void BeforeClosingTagState()
        {
            StepForward();

            if (char.IsWhiteSpace(Current))
            {
                BeforeClosingTagState();
            }
            else if (char.IsLetterOrDigit(Current))
            {
                ClosingTagNameState();
            }
            else if (Current == GT)
            {
                _diagnostics.Add($"Error: '{PlaceOfErrorCut()}'");
            }
        }

        private void ClosingTagNameState()
        {
            StepForward();

            if (char.IsLetterOrDigit(Current))
            {
                ClosingTagNameState();
            }
            else if (Current == GT)
            {
                CommitToken(SyntaxKind.ClosingTag);
            }
        }

        private void DoctypeState()
        {
            string part = _markup.Substring(_position, DOCTYPE.Length).ToUpper();

            if (part.Equals(DOCTYPE))
            {
                _position += DOCTYPE.Length;

                if (char.IsWhiteSpace(Current))
                {
                    DoctypeValueState();
                }
                else
                {
                    _diagnostics.Add($"Error: '{PlaceOfErrorCut()}'");
                }
            }
            else
            {
                _diagnostics.Add($"Error: '{PlaceOfErrorCut()}'");
            }
        }

        private void DoctypeValueState()
        {
            StepForward();

            if (char.IsWhiteSpace(Current))
            {
                DoctypeValueState();
            }
            else if (Current == GT)
            {
                CommitToken(SyntaxKind.Doctype);
            }
            else
            {
                DoctypeValueState();
            }
        }

        private void BogusCommentState()
        {
            StepForward();

            if (Current == GT)
            {
                CommitToken(SyntaxKind.BogusComment);
            }
        }

        private void BeforeCommentState()
        {
            StepForward();
            if (char.IsWhiteSpace(Current))
            {
                BeforeCommentState();
            }
            else if (Current == DASH)
            {
                AfterCommentState();
            }
            else
            {
                CommentState();
            }
        }

        private void CommentState()
        {
            StepForward();

            if (Current == DASH)
            {
                AfterCommentState();
            }
            else
            {
                CommentState();
            }
        }

        private void AfterCommentState()
        {
            StepForward();

            if (Current == DASH)
            {
                AfterCommentState();
            }
            else if (Current == GT)
            {
                CommitToken(SyntaxKind.Comment);
            }
            else
            {
                CommentState();
            }
        }

        private void TagNameState()
        {
            StepForward();

            if (char.IsLetterOrDigit(Current) || Current == DASH)
            {
                TagNameState();
            }
            else if (Current == GT)
            {
                CommitToken(SyntaxKind.OpenTag);
            }
            else if (char.IsWhiteSpace(Current))
            {
                AfterTagNameState();
            }
            else if (Current == F_SLASH)
            {
                AfterSelfClosingTagState();
            }
            else
            {
                _diagnostics.Add($"Error: '{PlaceOfErrorCut()}'");
            }
        }

        private void AfterTagNameState()
        {
            StepForward();

            if (char.IsWhiteSpace(Current))
            {
                AfterTagNameState();
            }
            else if (Current == GT)
            {
                CommitToken(SyntaxKind.OpenTag);
            }
            else if (char.IsLetter(Current))
            {
                _attributeName.Append(Current);
                AttrNameState();
            }
            else if (Current == F_SLASH)
            {
                AfterSelfClosingTagState();
            }
            else
            {
                _diagnostics.Add($"Error: '{PlaceOfErrorCut()}'");
            }
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
            {
                BeforeAttrValueState();
            }
            else
            {
                _diagnostics.Add($"Error: '{PlaceOfErrorCut()}'");
            }
        }

            private void AfterAttrNameState()
        {
            StepForward();

            if (char.IsWhiteSpace(Current))
            {
                AfterAttrNameState();
            }
            else if (Current == F_SLASH || 
                     char.IsLetter(Current) || 
                     Current == GT)
            {
                CommitAttribute();

                if (Current == F_SLASH)
                {
                    AfterSelfClosingTagState();
                }
                else
                {
                    StepBack();
                    AttrNameState();
                }
            }
            else if (Current == EQUALS)
            {
                BeforeAttrValueState();
            }
            else
            {
                _diagnostics.Add($"Error: '{PlaceOfErrorCut()}'");
            }
        }

        private void BeforeAttrValueState()
        {
            StepForward();
            if (char.IsWhiteSpace(Current))
            {
                BeforeAttrValueState();
            }
            else if (Current == DOUBLE_QUOTE)
            {
                DoubleQuotedAttrValueState();
            }
            else if (Current == SINGLE_QUOTE)
            {
                SingleQuotedAttrValueState();
            }
            else if (char.IsLetterOrDigit(Current) ||
                     Current == SHARP ||
                     Current == F_SLASH)
            {
                _attributeValue.Append(Current);
                UnquotedAttrValueState();
            }
            else
            {
                _diagnostics.Add($"Error: '{PlaceOfErrorCut()}'");
            }
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
            {
                AfterSelfClosingTagState();
            }
            else if (Current == GT)
            {
                CommitToken(SyntaxKind.AutoClosingTag);
            }
            else
            {
                _diagnostics.Add($"Error: '{PlaceOfErrorCut()}'");
            }
        }
    }
}
