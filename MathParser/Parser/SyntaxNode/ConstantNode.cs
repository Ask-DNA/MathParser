namespace MathParser;

internal class ConstantNode : ValueNode
{
    public ConstantNode(Keyword keyword, ValueDomain type) : base(keyword)
    {
        _type = type;
    }

    public override ValueDomain? GetOutputType(ArgumentTypizationHandler handler)
    {
        return _type;
    }
}
