namespace MathParser;

internal abstract class ValueNode(Keyword keyword) : SyntaxNode(keyword)
{
    protected ValueDomain? _type = null;
}
