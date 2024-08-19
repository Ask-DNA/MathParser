namespace MathParser;

public class Function : Operation
{
    public string Name { get; init; }

    #region BuiltInFunctions

    internal static Function Abs
    {
        get => ArithmeticalFunctionOfOneArgument("Abs", MathSource.Abs);
    }

    internal static Function Sqrt
    {
        get => ArithmeticalFunctionOfOneArgument("Sqrt", MathSource.Sqrt);
    }

    internal static Function Cbrt
    {
        get => ArithmeticalFunctionOfOneArgument("Cbrt", MathSource.Cbrt);
    }

    internal static Function Exp
    {
        get => ArithmeticalFunctionOfOneArgument("Exp", MathSource.Exp);
    }

    internal static Function Pow
    {
        get => ArithmeticalFunctionOfTwoArguments("Pow", MathSource.Pow);
    }

    internal static Function Log
    {
        get => ArithmeticalFunctionOfTwoArguments("Log", MathSource.Log);
    }

    internal static Function Log2
    {
        get => ArithmeticalFunctionOfOneArgument("Log2", MathSource.Log2);
    }

    internal static Function Log10
    {
        get => ArithmeticalFunctionOfOneArgument("Log10", MathSource.Log10);
    }

    internal static Function Ln
    {
        get => ArithmeticalFunctionOfOneArgument("Ln", MathSource.Ln);
    }

    internal static Function Sin
    {
        get => ArithmeticalFunctionOfOneArgument("Sin", MathSource.Sin);
    }

    internal static Function Cos
    {
        get => ArithmeticalFunctionOfOneArgument("Cos", MathSource.Cos);
    }

    internal static Function Tan
    {
        get => ArithmeticalFunctionOfOneArgument("Tan", MathSource.Tan);
    }

    internal static Function Asin
    {
        get => ArithmeticalFunctionOfOneArgument("Asin", MathSource.Asin);
    }

    internal static Function Acos
    {
        get => ArithmeticalFunctionOfOneArgument("Acos", MathSource.Acos);
    }

    internal static Function Atan
    {
        get => ArithmeticalFunctionOfOneArgument("Atan", MathSource.Atan);
    }

    internal static Function Sinh
    {
        get => ArithmeticalFunctionOfOneArgument("Sinh", MathSource.Sinh);
    }

    internal static Function Cosh
    {
        get => ArithmeticalFunctionOfOneArgument("Cosh", MathSource.Cosh);
    }

    internal static Function Tanh
    {
        get => ArithmeticalFunctionOfOneArgument("Tanh", MathSource.Tanh);
    }

    internal static Function Asinh
    {
        get => ArithmeticalFunctionOfOneArgument("Asinh", MathSource.Asinh);
    }

    internal static Function Acosh
    {
        get => ArithmeticalFunctionOfOneArgument("Acosh", MathSource.Acosh);
    }

    internal static Function Atanh
    {
        get => ArithmeticalFunctionOfOneArgument("Atanh", MathSource.Atanh);
    }

    internal static Function Not
    {
        get => LogicalFunctionOfOneArgument("Not", MathSource.Not);
    }

    internal static Function And
    {
        get => LogicalFunctionOfTwoArguments("And", MathSource.And);
    }

    internal static Function Or
    {
        get => LogicalFunctionOfTwoArguments("Or", MathSource.Or);
    }

    internal static Function Eqv
    {
        get => LogicalFunctionOfTwoArguments("Eqv", MathSource.Eqv);
    }

    internal static Function Imp
    {
        get => LogicalFunctionOfTwoArguments("Imp", MathSource.Imp);
    }

    internal static Function Xor
    {
        get => LogicalFunctionOfTwoArguments("Xor", MathSource.Xor);
    }

    internal static Function Nand
    {
        get => LogicalFunctionOfTwoArguments("Nand", MathSource.Nand);
    }

    internal static Function Nor
    {
        get => LogicalFunctionOfTwoArguments("Nor", MathSource.Nor);
    }

    #endregion

    internal Function(
        string name,
        Func<double[], double> source,
        string[] arguments,
        ValueDomain[] argumentTypes,
        ValueDomain outputType)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw ExceptionBuilder.NameIsNullOrWhitespaceOnObjectCreationException();
        if (!ParserGlobal.ValidateName(name))
            throw ExceptionBuilder.InvalidNamingOnObjectCreationException(name);
        SetSourceIfValid(source, arguments, argumentTypes, outputType);
        Name = name;
    }

    internal Function(
        string name,
        Func<double[], double> source,
        string argument,
        ValueDomain argumentType,
        ValueDomain outputType) : this(name, source, [argument], [argumentType], outputType) { }

    public Function(string name, string expressionString, Parser? parser = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw ExceptionBuilder.NameIsNullOrWhitespaceOnObjectCreationException();
        if (!ParserGlobal.ValidateName(name))
            throw ExceptionBuilder.InvalidNamingOnObjectCreationException(name);

        Expression tmp;
        parser ??= new();
        try
        {
            tmp = parser.Parse(expressionString);
        }
        catch (ArgumentException e)
        {
            throw ExceptionBuilder.ErrorOccuredWhileParsingException(expressionString, e);
        }
        catch (AggregateException e)
        {
            throw ExceptionBuilder.ErrorOccuredWhileParsingException(expressionString, e);
        }
        SetSource(tmp);
        Name = name;
    }

    public Function(string name, Expression expression)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw ExceptionBuilder.NameIsNullOrWhitespaceOnObjectCreationException();
        if (!ParserGlobal.ValidateName(name))
            throw ExceptionBuilder.InvalidNamingOnObjectCreationException(name);
        SetSource(expression);
        Name = name;
    }

    #region FunctionPresets

    private static Function ArithmeticalFunctionOfOneArgument(string name, Func<double[], double> source, string argument = "value")
    {
        return new(name, source, argument, ValueDomain.Double, ValueDomain.Double);
    }

    private static Function ArithmeticalFunctionOfTwoArguments(string name, Func<double[], double> source, string firstArgument = "x", string secondArgument = "y")
    {
        return new(name, source, [firstArgument, secondArgument], [ValueDomain.Double, ValueDomain.Double], ValueDomain.Double);
    }

    private static Function LogicalFunctionOfOneArgument(string name, Func<double[], double> source, string argument = "value")
    {
        return new(name, source, argument, ValueDomain.Boolean, ValueDomain.Boolean);
    }

    private static Function LogicalFunctionOfTwoArguments(string name, Func<double[], double> source, string firstArgument = "x", string secondArgument = "y")
    {
        return new(name, source, [firstArgument, secondArgument], [ValueDomain.Boolean, ValueDomain.Boolean], ValueDomain.Boolean);
    }

    #endregion
}
