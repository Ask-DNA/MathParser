using LinqExpressions = System.Linq.Expressions;

namespace MathParser;

public class Parser
{
    private readonly MathCollection _mathCollection;

    public ValueDomain DefaultType { get; set; } = ValueDomain.Double;

    public bool EnableTypization { get; set; } = true;

    public bool MultiplicationSignInsertion { get; set; } = true;

    public MathCollection MathCollection
    {
        get { return _mathCollection.Clone(); }
    }

    public Parser(MathCollection? mathCollection = null)
    {
        _mathCollection = mathCollection == null ? MathCollection.Default() : mathCollection.Clone();
    }

    public Expression Parse(string expressionString)
    {
        if (!Process(expressionString, out Expression? result, out ArgumentException[] exceptions))
        {
            if (exceptions.Length == 1)
                throw exceptions[0];
            else
                throw ExceptionBuilder.SeveralErrorsWhileParsingException(expressionString, exceptions);
        }
        return result!;
    }

    public bool TryParse(string expressionString, out Expression? expression)
    {
        return Process(expressionString, out expression, out _);
    }

    private bool Process(string expressionString, out Expression? expression, out ArgumentException[] exceptions)
    {
        expression = null;

        SyntaxAnalyzer analyzer = CreateAnalyzer();
        if (!analyzer.Run(expressionString, out Keyword[] expressionKeywords, out string[] argumentNames,
                          out string formattedExpressionString, out exceptions))
            return false;

        LinqExpressions.ParameterExpression parameter = LinqExpressions.Expression.Parameter(typeof(double[]));
        LinqExpressions.Expression? root = Build(expressionKeywords, parameter, argumentNames, out SyntaxNode? syntaxTree);
        if (root is null)
        {
            ArgumentException e = new("Unmanaged error.");
            exceptions = [e];
            return false;
        }
        Func<double[], double> source = Compile(root!, parameter);

        ValueDomain[] argumentTypes;
        ValueDomain outputType;
        if (EnableTypization)
        {
            if (!Typization(syntaxTree!, argumentNames, out argumentTypes, out outputType, out exceptions))
                return false;
        }
        else
        {
            argumentTypes = new ValueDomain[argumentNames.Length];
            Array.Fill(argumentTypes, DefaultType);
            outputType = DefaultType;
        }

        expression = new(formattedExpressionString, source, argumentNames, argumentTypes, outputType);
        return true;
    }

    private SyntaxAnalyzer CreateAnalyzer()
    {
        return new(_mathCollection.Constants, _mathCollection.Functions, _mathCollection.PrefixOperatorSymbols,
                   _mathCollection.InfixOperatorSymbols, _mathCollection.PostfixOperatorSymbols, MultiplicationSignInsertion);
    }

    #region ExpressionBuilding

    private LinqExpressions.Expression? Build(
        Keyword[] expressionKeywords,
        LinqExpressions.ParameterExpression parameter,
        string[] argumentNames,
        out SyntaxNode? syntaxTree)
    {
        expressionKeywords = RemoveOuterBrackets(expressionKeywords);
        if (expressionKeywords.Length == 1)
            return BuildValueNode(expressionKeywords[0], parameter, argumentNames, out syntaxTree);
        if (expressionKeywords.Length > 1)
            return BuildOperationNode(expressionKeywords, parameter, argumentNames, out syntaxTree);
        syntaxTree = null;
        return null;
            
    }

    private static Keyword[] RemoveOuterBrackets(Keyword[] expressionKeywords)
    {
        Keyword[] curResult;
        Keyword[] nextResult = expressionKeywords;

        do
        {
            curResult = nextResult;
            if (nextResult.Length <= 2)
                break;
            if (nextResult[0].Type != KeywordType.LeftParenthesis || nextResult[^1].Type != KeywordType.RightParenthesis)
                break;
            nextResult = nextResult[1..^1];
        }
        while (CheckParenthesis(nextResult));

        return curResult;
    }

    private static bool CheckParenthesis(Keyword[] expressionKeywords)
    {
        int parenthesisDepth = 0;
        for (int i = 0; i < expressionKeywords.Length; i++)
        {
            if (expressionKeywords[i].Type == KeywordType.LeftParenthesis)
                parenthesisDepth++;
            else if (expressionKeywords[i].Type == KeywordType.RightParenthesis)
                if (--parenthesisDepth < 0)
                    break;
        }
        return parenthesisDepth == 0;
    }

    private LinqExpressions.Expression? BuildValueNode(
        Keyword keyword,
        LinqExpressions.ParameterExpression parameter,
        string[] argumentNames,
        out SyntaxNode? syntaxTree)
    {
        syntaxTree = null;
        return keyword.Type switch
        {
            KeywordType.Literal => BuildLiteralNode(keyword, out syntaxTree),
            KeywordType.Name when keyword.Subtype == KeywordSubtype.Constant => BuildConstantNode(keyword, out syntaxTree),
            KeywordType.Name when keyword.Subtype == KeywordSubtype.Argument => BuildArgumentNode(keyword, parameter, argumentNames, out syntaxTree),
            _ => null
        };
    }

    private static LinqExpressions.ConstantExpression BuildLiteralNode(Keyword keyword, out SyntaxNode syntaxTree)
    {
        double value = ParserGlobal.ParseLiteral(keyword.Word);
        syntaxTree = SyntaxNode.LiteralNode(keyword, value);
        return LinqExpressions.Expression.Constant(value);
    }

    private LinqExpressions.ConstantExpression BuildConstantNode(Keyword keyword, out SyntaxNode syntaxTree)
    {
        Constant c = _mathCollection.Constants.First(constant => constant.Name == keyword.Word);
        syntaxTree = SyntaxNode.ConstantNode(keyword, c.Type);
        return LinqExpressions.Expression.Constant(c.Value);
    }

    private static LinqExpressions.BinaryExpression BuildArgumentNode(
        Keyword keyword,
        LinqExpressions.ParameterExpression parameter,
        string[] argumentNames,
        out SyntaxNode syntaxTree)
    {
        int index = Array.IndexOf(argumentNames, keyword.Word);
        LinqExpressions.ConstantExpression indexExpression;
        indexExpression = LinqExpressions.Expression.Constant(index);

        syntaxTree = SyntaxNode.ArgumentNode(keyword);
        return LinqExpressions.Expression.ArrayIndex(parameter, indexExpression);
    }

    private LinqExpressions.InvocationExpression? BuildOperationNode(
        Keyword[] expressionKeywords,
        LinqExpressions.ParameterExpression parameter,
        string[] argumentNames,
        out SyntaxNode? syntaxTree)
    {
        if (FindSplittingPoint(expressionKeywords, out int splittingPoint))
            return BuildOperatorNode(expressionKeywords, splittingPoint, parameter, argumentNames, out syntaxTree);
        return BuildFunctionNode(expressionKeywords, parameter, argumentNames, out syntaxTree);
    }

    private bool FindSplittingPoint(Keyword[] expressionKeywords, out int splittingPoint)
    {
        splittingPoint = -1;
        int minPriority = int.MaxValue;
        Operator curOperator;
        int curPriority;
        int curDepth = 0;
        for (int i = expressionKeywords.Length - 1; i >= 0; i--)
        {
            switch (expressionKeywords[i].Type)
            {
                case KeywordType.RightParenthesis:
                    curDepth++;
                    break;
                case KeywordType.LeftParenthesis:
                    curDepth--;
                    break;
                case KeywordType.Operator when curDepth == 0:
                    curPriority = int.MaxValue;
                    switch (expressionKeywords[i].Subtype)
                    {
                        case KeywordSubtype.PrefixOperator:
                            curOperator = _mathCollection.PrefixOperators.First(o => o.Symbol == expressionKeywords[i].Word);
                            curPriority = (int)curOperator.Category;
                            break;
                        case KeywordSubtype.InfixOperator:
                            curOperator = _mathCollection.InfixOperators.First(o => o.Symbol == expressionKeywords[i].Word);
                            curPriority = (int)curOperator.Category;
                            break;
                        case KeywordSubtype.PostfixOperator:
                            curOperator = _mathCollection.PostfixOperators.First(o => o.Symbol == expressionKeywords[i].Word);
                            curPriority = (int)curOperator.Category;
                            break;
                    }
                    if (curPriority < minPriority)
                    {
                        minPriority = curPriority;
                        splittingPoint = i;
                    }
                    else if (curPriority == minPriority && expressionKeywords[i].Subtype == KeywordSubtype.PrefixOperator)
                        splittingPoint = i;
                    break;
            }
        }
        return splittingPoint >= 0;
    }

    private LinqExpressions.InvocationExpression? BuildOperatorNode(
        Keyword[] expressionKeywords,
        int operatorPosition,
        LinqExpressions.ParameterExpression parameter,
        string[] argumentNames,
        out SyntaxNode? syntaxTree)
    {
        if (operatorPosition == 0)
            return BuildUnaryOperatorNode(expressionKeywords[0],
                                          expressionKeywords[1..],
                                          _mathCollection.PrefixOperators,
                                          parameter,
                                          argumentNames,
                                          out syntaxTree);
        else if (operatorPosition == expressionKeywords.Length - 1)
            return BuildUnaryOperatorNode(expressionKeywords[^1],
                                          expressionKeywords[..^1],
                                          _mathCollection.PostfixOperators,
                                          parameter,
                                          argumentNames,
                                          out syntaxTree);
        else
            return BuildBinaryOperatorNode(expressionKeywords[operatorPosition],
                                           expressionKeywords[..operatorPosition],
                                           expressionKeywords[(operatorPosition + 1)..],
                                           _mathCollection.InfixOperators,
                                           parameter,
                                           argumentNames,
                                           out syntaxTree);
    }

    private LinqExpressions.InvocationExpression? BuildUnaryOperatorNode(
        Keyword operatorKeyword,
        Keyword[] operandKeywords,
        Operator[] unaryOperators,
        LinqExpressions.ParameterExpression parameter,
        string[] argumentNames,
        out SyntaxNode? syntaxTree)
    {
        Operator[] possibleOperators = unaryOperators.Where(o => o.Symbol == operatorKeyword.Word).ToArray();
        List<ValueDomain[]> possibleSignatures = [];
        foreach (Operator o in possibleOperators)
        {
            ValueDomain[] signature = new ValueDomain[o.Arity + 1];
            signature[0] = o.OutputType;
            for (int i = 1; i < o.Arity + 1; i++)
                signature[i] = o.ArgumentTypes[i - 1];
            possibleSignatures.Add(signature);
        }

        LinqExpressions.Expression? operand = Build(operandKeywords, parameter, argumentNames, out SyntaxNode? operandSyntaxTree);
        if (operand is null)
        {
            syntaxTree = null;
            return null;
        }
        LinqExpressions.InvocationExpression result = possibleOperators[0].AsLinqExpression(operand);
        syntaxTree = SyntaxNode.OperatorNode(operatorKeyword, possibleSignatures, [operandSyntaxTree!]);
        return result;
    }

    private LinqExpressions.InvocationExpression? BuildBinaryOperatorNode(
        Keyword operatorKeyword,
        Keyword[] firstOperandKeywords,
        Keyword[] secondOperandKeywords,
        Operator[] binaryOperators,
        LinqExpressions.ParameterExpression parameter,
        string[] argumentNames,
        out SyntaxNode? syntaxTree)
    {
        Operator[] possibleOperators = binaryOperators.Where(o => o.Symbol == operatorKeyword.Word).ToArray();
        List<ValueDomain[]> possibleSignatures = [];
        foreach (Operator o in possibleOperators)
        {
            ValueDomain[] signature = new ValueDomain[o.Arity + 1];
            signature[0] = o.OutputType;
            for (int i = 1; i < o.Arity + 1; i++)
                signature[i] = o.ArgumentTypes[i - 1];
            possibleSignatures.Add(signature);
        }

        LinqExpressions.Expression? firstOperand = Build(firstOperandKeywords, parameter, argumentNames, out SyntaxNode? firstOperandSyntaxTree);
        LinqExpressions.Expression? secondOperand = Build(secondOperandKeywords, parameter, argumentNames, out SyntaxNode? secondOperandSyntaxTree);
        if (firstOperand is null || secondOperand is null)
        {
            syntaxTree = null;
            return null;
        }
        LinqExpressions.InvocationExpression result = possibleOperators[0].AsLinqExpression(firstOperand, secondOperand);
        syntaxTree = SyntaxNode.OperatorNode(operatorKeyword, possibleSignatures, [firstOperandSyntaxTree!, secondOperandSyntaxTree!]);
        return result;
    }

    private LinqExpressions.InvocationExpression? BuildFunctionNode(
        Keyword[] expressionKeywords,
        LinqExpressions.ParameterExpression parameter,
        string[] argumentNames,
        out SyntaxNode? syntaxTree)
    {
        List<Keyword[]> nestedExpressions = GetNestedExpressions(expressionKeywords);
        Function function = _mathCollection.Functions.First(f => f.Name == expressionKeywords[0].Word && f.Arity == nestedExpressions.Count);

        ValueDomain[] signature = new ValueDomain[function.Arity + 1];
        signature[0] = function.OutputType;
        for (int i = 1; i < function.Arity + 1; i++)
            signature[i] = function.ArgumentTypes[i - 1];

        List<LinqExpressions.Expression> functionArgumentExpressions = [];
        List<SyntaxNode> functionArgumentSyntaxTrees = [];
        LinqExpressions.Expression? tmpExpression;
        SyntaxNode? tmpSyntaxTree;
        for (int i = 0; i < nestedExpressions.Count; i++)
        {
            tmpExpression = Build(nestedExpressions[i], parameter, argumentNames, out tmpSyntaxTree);
            if (tmpExpression is null)
            {
                syntaxTree = null;
                return null;
            }
            functionArgumentExpressions.Add(tmpExpression);
            functionArgumentSyntaxTrees.Add(tmpSyntaxTree!);
        }

        LinqExpressions.InvocationExpression result = function.AsLinqExpression([.. functionArgumentExpressions]);
        syntaxTree = SyntaxNode.FunctionNode(expressionKeywords[0], signature, [.. functionArgumentSyntaxTrees]);
        return result;
    }

    private static List<Keyword[]> GetNestedExpressions(Keyword[] expressionKeywords)
    {
        if (expressionKeywords[2].Type == KeywordType.RightParenthesis)
            return [];

        List<Keyword[]> result = [];
        List<Keyword> tmp = [];
        int relativeDepth = 0;
        for (int i = 2; i < expressionKeywords.Length && relativeDepth >= 0; i++)
        {
            switch (expressionKeywords[i].Type)
            {
                case KeywordType.LeftParenthesis:
                    ++relativeDepth;
                    tmp.Add(expressionKeywords[i]);
                    break;
                case KeywordType.RightParenthesis:
                    if (--relativeDepth < 0)
                        continue;
                    else
                        tmp.Add(expressionKeywords[i]);
                    break;
                case KeywordType.ArgumentSeparator when relativeDepth == 0:
                    result.Add([.. tmp]);
                    tmp.Clear();
                    break;
                default:
                    tmp.Add(expressionKeywords[i]);
                    break;
            }
        }
        if (tmp.Count > 0)
            result.Add([.. tmp]);

        return result;
    }

    #endregion

    private bool Typization(
        SyntaxNode syntaxTree,
        string[] argumentNames,
        out ValueDomain[] argumentTypes,
        out ValueDomain outputType,
        out ArgumentException[] exceptions)
    {
        List<ArgumentException> exceptionListTmp = [];
        ArgumentTypizationHandler handler = new(argumentNames);
        bool stop;
        do
        {
            if (!syntaxTree.Validate(handler, out ArgumentException[] exceptionsTmp))
            {
                exceptionListTmp.AddRange(exceptionsTmp);
                stop = true;
            }
            else
            {
                if (!handler.RefreshArgumentTypes(out bool typesChanged, out exceptionsTmp))
                {
                    exceptionListTmp.AddRange(exceptionsTmp);
                    stop = true;
                }
                else
                {
                    stop = !typesChanged;
                }
            }
        }
        while (!stop);
        
        argumentTypes = new ValueDomain[argumentNames.Length];
        for (int i = 0; i < argumentNames.Length; i++)
            argumentTypes[i] = handler.GetArgumentType(argumentNames[i]) ?? DefaultType;

        exceptions = [.. exceptionListTmp];
        if (exceptions.Length == 0)
            outputType = syntaxTree.GetOutputType(handler) ?? DefaultType;
        else
            outputType = DefaultType;
        return exceptions.Length == 0;
    }

    private static Func<double[], double> Compile(LinqExpressions.Expression root, LinqExpressions.ParameterExpression parameters)
    {
        LinqExpressions.Expression<Func<double[], double>> lambdaExpression;
        lambdaExpression = LinqExpressions.Expression.Lambda<Func<double[], double>>(root, parameters);
        return lambdaExpression.Compile();
    }
}
