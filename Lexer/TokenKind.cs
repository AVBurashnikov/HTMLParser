namespace htmlparser.Lexer
{
    enum TokenKind
    {
        Doctype,
        OpenTag,
        ClosingTag,
        SelfClosingTag,
        Content,
        Comment,
        BogusComment,
    }
}
