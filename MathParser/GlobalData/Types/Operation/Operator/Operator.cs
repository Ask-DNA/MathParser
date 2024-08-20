namespace MathParser;

internal sealed class Operator : Operation
{
    public OperatorName Name { get; init; }
    
    public OperatorCategory Category { get; init; }

    public string Symbol { get; init; }

    #region BuiltInArithmeticalUnaryOperators

    internal static Operator UnaryPlus
    {
        get => UnaryArithmeticalPrefixOperator(OperatorName.UnaryPlus, "+", OperatorSource.UnaryPlus);
    }

    internal static Operator UnaryMinus
    {
        get => UnaryArithmeticalPrefixOperator(OperatorName.UnaryMinus, "-", OperatorSource.UnaryMinus);
    }

    internal static Operator Factorial
    {
        get => UnaryArithmeticalPostfixOperator(OperatorName.Factorial, "!", OperatorSource.Factorial);
    }

    #endregion

    #region BuiltInArithmeticalBinaryOperators

    internal static Operator Addition
    {
        get => BinaryArithmeticalOperator(OperatorName.Addition, OperatorCategory.Additive, "+", OperatorSource.Addition);
    }

    internal static Operator Subtraction
    {
        get => BinaryArithmeticalOperator(OperatorName.Subtraction, OperatorCategory.Additive, "-", OperatorSource.Subtraction);
    }

    internal static Operator Multiplication
    {
        get => BinaryArithmeticalOperator(OperatorName.Multiplication, OperatorCategory.Multiplicative, "*", OperatorSource.Multiplication);
    }

    internal static Operator Division
    {
        get => BinaryArithmeticalOperator(OperatorName.Division, OperatorCategory.Multiplicative, "/", OperatorSource.Division);
    }

    internal static Operator Modulation
    {
        get => BinaryArithmeticalOperator(OperatorName.Modulation, OperatorCategory.Multiplicative, "%", OperatorSource.Modulation);
    }

    internal static Operator Exponentiation
    {
        get => BinaryArithmeticalOperator(OperatorName.Exponentiation, OperatorCategory.Exponentiation, "^", OperatorSource.Exponentiation);
    }

    #endregion

    #region BuiltInLogicalUnaryOperators

    internal static Operator LogicNegation1
    {
        get => UnaryLogicalPrefixOperator(OperatorName.LogicNegation, "!", OperatorSource.LogicNegation);
    }

    internal static Operator LogicNegation2
    {
        get => UnaryLogicalPrefixOperator(OperatorName.LogicNegation, "~", OperatorSource.LogicNegation);
    }

    #endregion

    #region BuiltInLogicalBinaryOperators

    internal static Operator Conjunction1
    {
        get => BinaryLogicalOperator(OperatorName.Conjunction, OperatorCategory.Conjunction, "&", OperatorSource.Conjunction);
    }

    internal static Operator Conjunction2
    {
        get => BinaryLogicalOperator(OperatorName.Conjunction, OperatorCategory.Conjunction, "&&", OperatorSource.Conjunction);
    }

    internal static Operator Disjunction1
    {
        get => BinaryLogicalOperator(OperatorName.Disjunction, OperatorCategory.Disjunction, "|", OperatorSource.Disjunction);
    }

    internal static Operator Disjunction2
    {
        get => BinaryLogicalOperator(OperatorName.Disjunction, OperatorCategory.Disjunction, "||", OperatorSource.Disjunction);
    }

    internal static Operator Equivalence
    {
        get => BinaryLogicalOperator(OperatorName.Equivalence, OperatorCategory.Equivalence, "<->", OperatorSource.Equivalence);
    }

    internal static Operator Implication
    {
        get => BinaryLogicalOperator(OperatorName.Implication, OperatorCategory.Implication, "->", OperatorSource.Implication);
    }

    internal static Operator ConverseImplication
    {
        get => BinaryLogicalOperator(OperatorName.ConverseImplication, OperatorCategory.Implication, "<-", OperatorSource.ConverseImplication);
    }

    #endregion

    #region BuiltInComparsionOperators

    internal static Operator DoubleEquality1
    {
        get => BinaryRelation(OperatorName.Equality, OperatorCategory.Equality, "=", OperatorSource.Equality);
    }

    internal static Operator DoubleEquality2
    {
        get => BinaryRelation(OperatorName.Equality, OperatorCategory.Equality, "==", OperatorSource.Equality);
    }

    internal static Operator BooleanEquality1
    {
        get => BinaryLogicalOperator(OperatorName.Equality, OperatorCategory.Equality, "=", OperatorSource.Equality);
    }

    internal static Operator BooleanEquality2
    {
        get => BinaryLogicalOperator(OperatorName.Equality, OperatorCategory.Equality, "==", OperatorSource.Equality);
    }

    internal static Operator DoubleInequality1
    {
        get => BinaryRelation(OperatorName.Inequality, OperatorCategory.Equality, "!=", OperatorSource.Inequality);
    }

    internal static Operator DoubleInequality2
    {
        get => BinaryRelation(OperatorName.Inequality, OperatorCategory.Equality, "~=", OperatorSource.Inequality);
    }

    internal static Operator BooleanInequality1
    {
        get => BinaryLogicalOperator(OperatorName.Inequality, OperatorCategory.Equality, "!=", OperatorSource.Inequality);
    }

    internal static Operator BooleanInequality2
    {
        get => BinaryLogicalOperator(OperatorName.Inequality, OperatorCategory.Equality, "~=", OperatorSource.Inequality);
    }

    internal static Operator LessThan
    {
        get => BinaryRelation(OperatorName.LessThan, OperatorCategory.Relational, "<", OperatorSource.LessThan);
    }

    internal static Operator LessThanOrEqual
    {
        get => BinaryRelation(OperatorName.LessThanOrEqual, OperatorCategory.Relational, "<=", OperatorSource.LessThanOrEqual);
    }

    internal static Operator GreaterThan
    {
        get => BinaryRelation(OperatorName.GreaterThan, OperatorCategory.Relational, ">", OperatorSource.GreaterThan);
    }

    internal static Operator GreaterThanOrEqual
    {
        get => BinaryRelation(OperatorName.GreaterThanOrEqual, OperatorCategory.Relational, ">=", OperatorSource.GreaterThanOrEqual);
    }

    #endregion

    #region CoalesceOperator

    internal static Operator DoubleCoalesce
    {
        get => BinaryArithmeticalOperator(OperatorName.Coalesce, OperatorCategory.Coalesce, "??", OperatorSource.Coalesce);
    }

    internal static Operator BooleanCoalesce
    {
        get => BinaryLogicalOperator(OperatorName.Coalesce, OperatorCategory.Coalesce, "??", OperatorSource.Coalesce);
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