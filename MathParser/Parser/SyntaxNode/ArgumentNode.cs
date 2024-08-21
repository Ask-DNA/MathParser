namespace MathParser;

internal class ArgumentNode(Keyword keyword) : SyntaxNode(keyword)
{
    public override ValueDomain? GetOutputType(ArgumentTypizationHandler handler)
    {
        return handler.GetArgumentType(_keyword.Word);
    }

    public override void SetOutputType(ValueDomain type, ArgumentTypizationHandler handler)
    {
        handler.RequireArgumentType(_keyword.Word, type);
    }
}
