namespace MathParser;

internal sealed class Operator : Operation
{
    public OperatorName Name { get; init; }
    
    public OperatorCategory Category { get; init; }

    public string Symbol { get; init; }

    #region BuiltInArithmeticalUnaryOperators

    internal static Operator UnaryPlus
    {
        get => UnaryArithmeticalPrefixOperator(OperatorName.UnaryPlus, "+", MathSource.UnaryPlus);
    }

    internal static Operator UnaryMinus
    {
        get => UnaryArithmeticalPrefixOperator(OperatorName.UnaryMinus, "-", MathSource.UnaryMinus);
    }

    internal static Operator Factorial
    {
        get => UnaryArithmeticalPostfixOperator(OperatorName.Factorial, "!", MathSource.Factorial);
    }

    #endregion

    #region BuiltInArithmeticalBinaryOperators

    internal static Operator Addition
    {
        get => BinaryArithmeticalOperator(OperatorName.Addition, OperatorCategory.Additive, "+", MathSource.Addition);
    }

    internal static Operator Subtraction
    {
        get => BinaryArithmeticalOperator(OperatorName.Subtraction, OperatorCategory.Additive, "-", MathSource.Subtraction);
    }

    internal static Operator Multiplication
    {
        get => BinaryArithmeticalOperator(OperatorName.Multiplication, OperatorCategory.Multiplicative, "*", MathSource.Multiplication);
    }

    internal static Operator Division
    {
        get => BinaryArithmeticalOperator(OperatorName.Division, OperatorCategory.Multiplicative, "/", MathSource.Division);
    }

    internal static Operator Modulation
    {
        get => BinaryArithmeticalOperator(OperatorName.Modulation, OperatorCategory.Multiplicative, "%", MathSource.Modulation);
    }

    internal static Operator Exponentiation
    {
        get => BinaryArithmeticalOperator(OperatorName.Exponentiation, OperatorCategory.Exponentiation, "^", MathSource.Exponentiation);
    }

    #endregion

    #region BuiltInLogicalUnaryOperators

    internal static Operator LogicNegation1
    {
        get => UnaryLogicalPrefixOperator(OperatorName.LogicNegation, "!", MathSource.LogicNegation);
    }

    internal static Operator LogicNegation2
    {
        get => UnaryLogicalPrefixOperator(OperatorName.LogicNegation, "~", MathSource.LogicNegation);
    }

    #endregion

    #region BuiltInLogicalBinaryOperators

    internal static Operator Conjunction1
    {
        get => BinaryLogicalOperator(OperatorName.Conjunction, OperatorCategory.Conjunction, "&", MathSource.Conjunction);
    }

    internal static Operator Conjunction2
    {
        get => BinaryLogicalOperator(OperatorName.Conjunction, OperatorCategory.Conjunction, "&&", MathSource.Conjunction);
    }

    internal static Operator Disjunction1
    {
        get => BinaryLogicalOperator(OperatorName.Disjunction, OperatorCategory.Disjunction, "|", MathSource.Disjunction);
    }

    internal static Operator Disjunction2
    {
        get => BinaryLogicalOperator(OperatorName.Disjunction, OperatorCategory.Disjunction, "||", MathSource.Disjunction);
    }

    internal static Operator Equivalence
    {
        get => BinaryLogicalOperator(OperatorName.Equivalence, OperatorCategory.Equivalence, "<->", MathSource.Equivalence);
    }

    internal static Operator Implication
    {
        get => BinaryLogicalOperator(OperatorName.Implication, OperatorCategory.Implication, "->", MathSource.Implication);
    }

    internal static Operator ConverseImplication
    {
        get => BinaryLogicalOperator(OperatorName.ConverseImplication, OperatorCategory.Implication, "<-", MathSource.ConverseImplication);
    }

    #endregion

    #region BuiltInComparsionOperators

    internal static Operator DoubleEquality1
    {
        get => BinaryRelation(OperatorName.Equality, OperatorCategory.Equality, "=", MathSource.Equality);
    }

    internal static Operator DoubleEquality2
    {
        get => BinaryRelation(OperatorName.Equality, OperatorCategory.Equality, "==", MathSource.Equality);
    }

    internal static Operator BooleanEquality1
    {
        get => BinaryLogicalOperator(OperatorName.Equality, OperatorCategory.Equality, "=", MathSource.Equality);
    }

    internal static Operator BooleanEquality2
    {
        get => BinaryLogicalOperator(OperatorName.Equality, OperatorCategory.Equality, "==", MathSource.Equality);
    }

    internal static Operator DoubleInequality1
    {
        get => BinaryRelation(OperatorName.Inequality, OperatorCategory.Equality, "!=", MathSource.Inequality);
    }

    internal static Operator DoubleInequality2
    {
        get => BinaryRelation(OperatorName.Inequality, OperatorCategory.Equality, "~=", MathSource.Inequality);
    }

    internal static Operator BooleanInequality1
    {
        get => BinaryLogicalOperator(OperatorName.Inequality, OperatorCategory.Equality, "!=", MathSource.Inequality);
    }

    internal static Operator BooleanInequality2
    {
        get => BinaryLogicalOperator(OperatorName.Inequality, OperatorCategory.Equality, "~=", MathSource.Inequality);
    }

    internal static Operator LessThan
    {
        get => BinaryRelation(OperatorName.LessThan, OperatorCategory.Relational, "<", MathSource.LessThan);
    }

    internal static Operator LessThanOrEqual
    {
        get => BinaryRelation(OperatorName.LessThanOrEqual, OperatorCategory.Relational, "<=", MathSource.LessThanOrEqual);
    }

    internal static Operator GreaterThan
    {
        get => BinaryRelation(OperatorName.GreaterThan, OperatorCategory.Relational, ">", MathSource.GreaterThan);
    }

    internal static Operator GreaterThanOrEqual
    {
        get => BinaryRelation(OperatorName.GreaterThanOrEqual, OperatorCategory.Relational, ">=", MathSource.GreaterThanOrEqual);
    }

    #endregion

    #region CoalesceOperator

    internal static Operator DoubleCoalesce
    {
        get => BinaryArithmeticalOperator(OperatorName.Coalesce, OperatorCategory.Coalesce, "??", MathSource.Coalesce);
    }

    internal static Operator BooleanCoalesce
    {
        get => BinaryLogicalOperator(OperatorName.Coalesce, OperatorCategory.Coalesce, "??", MathSource.Coalesce);
    }

    #endregion

    internal Operator(
        OperatorName name,
        OperatorCategory category,
        string symbol,
        Func<double[], double> source,
        string[] operands,
        ValueDomain[] operandTypes,
        ValueDomain outputType)
    {
        SetSourceIfValid(source, operands, operandTypes, outputType);
        Name = name;
        Category = category;
        Symbol = symbol;
    }

    internal Operator(
        OperatorName name,
        OperatorCategory category,
        string symbol,
        Func<double[], double> source,
        string operand,
        ValueDomain operandType,
        ValueDomain outputType) : this(name, category, symbol, source, [operand], [operandType], outputType) { }

    #region OperatorPresets

    private static Operator UnaryArithmeticalPrefixOperator(OperatorName name, string symbol, Func<double[], double> source, string operand = "value")
    {
        return new(name, OperatorCategory.UnaryPrefix, symbol, source, operand, ValueDomain.Double, ValueDomain.Double);
    }

    private static Operator UnaryArithmeticalPostfixOperator(OperatorName name, string symbol, Func<double[], double> source, string operand = "value")
    {
        return new(name, OperatorCategory.UnaryPostfix, symbol, source, operand, ValueDomain.Double, ValueDomain.Double);
    }

    private static Operator BinaryArithmeticalOperator(OperatorName name, OperatorCategory category, string symbol, Func<double[], double> source, string firstOperand = "a", string secondOperand = "b")
    {
        return new(name, category, symbol, source, [firstOperand, secondOperand], [ValueDomain.Double, ValueDomain.Double], ValueDomain.Double);
    }

    private static Operator UnaryLogicalPrefixOperator(OperatorName name, string symbol, Func<double[], double> source, string operand = "value")
    {
        return new(name, OperatorCategory.UnaryPrefix, symbol, source, operand, ValueDomain.Boolean, ValueDomain.Boolean);
    }

    private static Operator UnaryLogicalPostfixOperator(OperatorName name, string symbol, Func<double[], double> source, string operand = "value")
    {
        return new(name, OperatorCategory.UnaryPostfix, symbol, source, operand, ValueDomain.Boolean, ValueDomain.Boolean);
    }

    private static Operator BinaryLogicalOperator(OperatorName name, OperatorCategory category, string symbol, Func<double[], double> source, string firstOperand = "a", string secondOperand = "b")
    {
        return new(name, category, symbol, source, [firstOperand, secondOperand], [ValueDomain.Boolean, ValueDomain.Boolean], ValueDomain.Boolean);
    }

    private static Operator BinaryRelation(OperatorName name, OperatorCategory category, string symbol, Func<double[], double> source, string firstOperand = "a", string secondOperand = "b")
    {
        return new(name, category, symbol, source, [firstOperand, secondOperand], [ValueDomain.Double, ValueDomain.Double], ValueDomain.Boolean);
    }

    #endregion
}