namespace MathParser;

internal abstract class SyntaxNode(Keyword keyword)
{
    protected readonly Keyword _keyword = keyword;

    public static LiteralNode LiteralNode(Keyword keyword, double value) => new(keyword, value);

    public static ConstantNode ConstantNode(Keyword keyword, ValueDomain type) => new(keyword, type);

    public static ArgumentNode ArgumentNode(Keyword keyword) => new(keyword);

    public static OperationNode OperatorNode(Keyword keyword, List<ValueDomain[]> possibleSignatures, SyntaxNode[] children)
    {
        return new(keyword, possibleSignatures, children);
    }

    public static OperationNode FunctionNode(Keyword keyword, ValueDomain[] signature, SyntaxNode[] children)
    {
        return new(keyword, [signature], children);
    }

    public virtual bool Validate(ArgumentTypizationHandler handler, out ArgumentException[] exceptions)
    {
        exceptions = [];
        return true;
    }

    public abstract ValueDomain? GetOutputType(ArgumentTypizationHandler handler);

    public virtual void SetOutputType(ValueDomain type, ArgumentTypizationHandler handler) { }
}
