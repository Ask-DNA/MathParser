namespace MathParser;

internal class OperationNode : SyntaxNode
{
    // signature[0] is return type; signature[1 ..] is input types
    private List<ValueDomain[]> _possibleSignatures;
    private readonly SyntaxNode[] _children;

    public OperationNode(Keyword keyword, List<ValueDomain[]> possibleSignatures, SyntaxNode[] children) : base(keyword)
    {
        _possibleSignatures = new List<ValueDomain[]>(possibleSignatures.Count);
        for (int i = 0; i < possibleSignatures.Count; i++)
        {
            _possibleSignatures.Add(new ValueDomain[possibleSignatures[i].Length]);
            possibleSignatures[i].CopyTo(_possibleSignatures[i], 0);
        }
        _children = new SyntaxNode[children.Length];
        children.CopyTo(_children, 0);
    }

    public override bool Validate(ArgumentTypizationHandler handler, out ArgumentException[] exceptions)
    {
        ValueDomain?[] childrenTypes = GetChildrenTypes(handler);
        FilterPossibleSignatures([null, .. childrenTypes]);

        ArgumentException? curNodeException = null;
        if (_possibleSignatures.Count == 0)
        {
            if (_keyword.Type == KeywordType.Operator && _keyword.OriginalPosition >= 0)
                curNodeException = ExceptionBuilder.OperatorTypizationErrorException(_keyword.Word, _keyword.OriginalPosition);
            else if (_keyword.Type == KeywordType.Name)
                curNodeException = ExceptionBuilder.FunctionTypizationErrorException(_keyword.Word, _keyword.OriginalPosition);
            else
                curNodeException = ExceptionBuilder.InsertedMultiplicationTypizationErrorException();
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

    public override ValueDomain? GetOutputType(ArgumentTypizationHandler handler)
    {
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
}
