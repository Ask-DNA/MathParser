namespace MathParser;

internal class LiteralNode : ValueNode
{
    public LiteralNode(Keyword keyword, double value) : base(keyword)
    {
        if (!ParserGlobal.CheckIfValueBelongsToType(value, ValueDomain.Boolean))
            _type = ValueDomain.Double;
        else
            _type = null;
    }

    public override ValueDomain? GetOutputType(ArgumentTypizationHandler handler)
    {
        return _type;
    }
}
