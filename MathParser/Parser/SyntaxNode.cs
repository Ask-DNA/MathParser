namespace MathParser;

internal class SyntaxNode
{
    private readonly Keyword _keyword;
    // signature[0] is return type; signature[1 ..] is input types
    private List<ValueDomain[]> _possibleSignatures;
    private readonly SyntaxNode[] _children;

    public SyntaxNode(Keyword keyword, List<ValueDomain[]> possibleSignatures, SyntaxNode[] children)
    {
        _keyword = keyword;
        _possibleSignatures = new List<ValueDomain[]>(possibleSignatures.Count);
        for (int i = 0; i < possibleSignatures.Count; i++)
        {
            _possibleSignatures.Add(new ValueDomain[possibleSignatures[i].Length]);
            possibleSignatures[i].CopyTo(_possibleSignatures[i], 0);
        }
        _children = new SyntaxNode[children.Length];
        children.CopyTo(_children, 0);
    }

    public static SyntaxNode ArgumentNode(Keyword keyword)
    {
        return new(keyword, [], []);
    }

    public static SyntaxNode LiteralNode(Keyword keyword, double value)
    {
        if (value != 0 && value != 1)
            return new(keyword, [[ValueDomain.Double]], []);
        else
            return new(keyword, [[ValueDomain.Double], [ValueDomain.Boolean]], []);
    }

    public static SyntaxNode ConstantNode(Keyword keyword, ValueDomain outputType)
    {
        return new(keyword, [[outputType]], []);
    }

    public static SyntaxNode FunctionNode(Keyword keyword, ValueDomain[] signature, SyntaxNode[] children)
    {
        return new(keyword, [signature], children);
    }

    public static SyntaxNode OperatorNode(Keyword keyword, List<ValueDomain[]> possibleSignatures, SyntaxNode[] children)
    {
        return new(keyword, possibleSignatures, children);
    }

    public bool Validate(ArgumentTypizationHandler handler, out ArgumentException[] exceptions)
    {
        if (_keyword.Subtype != KeywordSubtype.Function && _keyword.Type != KeywordType.Operator)
        {
            exceptions = [];
            return true;
        }

        ValueDomain?[] childrenTypes = GetChildrenTypes(handler);
        FilterPossibleSignatures([null, .. childrenTypes]);

        ArgumentException? curNodeException = null;
        if (_possibleSignatures.Count == 0)
        {
            if (_keyword.Type == KeywordType.Operator)
                curNodeException = ExceptionBuilder.OperatorTypizationErrorException(_keyword.Word, _keyword.OriginalPosition);
            else
                curNodeException = ExceptionBuilder.FunctionTypizationErrorException(_keyword.Word, _keyword.OriginalPosition);
        }
        if (_possibleSignatures.Count == 1)
            SetChildrenTypes(_possibleSignatures[0][1..], handler);

        if (!ValidateChildren(handler, out ArgumentException[] childrenExceptions))
        {
            if (curNodeException is not null)
                exceptions = [curNodeException, .. childrenExceptions];
            else
                exceptions = [.. childrenExceptions];
            return false;
        }
        if (curNodeException is not null)
            exceptions = [curNodeException];
        else
            exceptions = [];
        return exceptions.Length == 0;
    }

    private void FilterPossibleSignatures(ValueDomain?[] filter)
    {
        List<ValueDomain[]> possibleSignatures = [];
        bool fits;
        for (int i = 0; i < _possibleSignatures.Count; i++)
        {
            fits = true;
            for (int j = 0; j < _possibleSignatures[i].Length; j++)
            {
                if (filter[j].HasValue && filter[j] != _possibleSignatures[i][j])
                    fits = false;
            }
            if (fits)
                possibleSignatures.Add(_possibleSignatures[i]);
        }
        _possibleSignatures = possibleSignatures;
    }

    private ValueDomain?[] GetChildrenTypes(ArgumentTypizationHandler handler)
    {
        ValueDomain?[] childrenTypes = new ValueDomain?[_children.Length];
        for (int i = 0; i < _children.Length; i++)
            childrenTypes[i] = _children[i].GetOutputType(handler);
        return childrenTypes;
    }

    private void SetChildrenTypes(ValueDomain[] types, ArgumentTypizationHandler handler)
    {
        for (int i = 0; i < _children.Length; i++)
            _children[i].SetOutputType(types[i], handler);
    }

    private bool ValidateChildren(ArgumentTypizationHandler handler, out ArgumentException[] exceptions)
    {
        List<ArgumentException> exceptionsTmp = [];
        foreach (SyntaxNode child in _children)
            if (!child.Validate(handler, out ArgumentException[] childExceptionsTmp))
                exceptionsTmp.AddRange(childExceptionsTmp);
        exceptions = [.. exceptionsTmp];
        return exceptions.Length == 0;
    }

    public ValueDomain? GetOutputType(ArgumentTypizationHandler handler)
    {
        if (_keyword.Subtype == KeywordSubtype.Argument)
            return handler.GetArgumentType(_keyword.Word);

        ValueDomain? type = _possibleSignatures[0][0];
        foreach (ValueDomain[] signature in _possibleSignatures)
        {
            if (type.HasValue && type != signature[0])
            {
                type = null;
                break;
            }
        }
        return type;
    }

    private void SetOutputType(ValueDomain type, ArgumentTypizationHandler handler)
    {
        if (_keyword.Subtype == KeywordSubtype.Argument)
            handler.RequireArgumentType(_keyword.Word, type);

        else if (_keyword.Type == KeywordType.Operator || _keyword.Subtype == KeywordSubtype.Function)
        {
            ValueDomain?[] filter = new ValueDomain?[_possibleSignatures[0].Length];
            Array.Fill(filter, null);
            filter[0] = type;
            FilterPossibleSignatures(filter);
        }
        return;
    }
}
