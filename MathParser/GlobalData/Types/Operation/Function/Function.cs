namespace MathParser;

public class Function : Operation
{
    public string Name { get; init; }

    #region BuiltInFunctions

    internal static Function Abs
    {
        get => ArithmeticalFunctionOfOneArgument("Abs", FunctionSource.Abs);
    }

    internal static Function Sqrt
    {
        get => ArithmeticalFunctionOfOneArgument("Sqrt", FunctionSource.Sqrt);
    }

    internal static Function Cbrt
    {
        get => ArithmeticalFunctionOfOneArgument("Cbrt", FunctionSource.Cbrt);
    }

    internal static Function Exp
    {
        get => ArithmeticalFunctionOfOneArgument("Exp", FunctionSource.Exp);
    }

    internal static Function Pow
    {
        get => ArithmeticalFunctionOfTwoArguments("Pow", FunctionSource.Pow);
    }

    internal static Function Log
    {
        get => ArithmeticalFunctionOfTwoArguments("Log", FunctionSource.Log);
    }

    internal static Function Log2
    {
        get => ArithmeticalFunctionOfOneArgument("Log2", FunctionSource.Log2);
    }

    internal static Function Log10
    {
        get => ArithmeticalFunctionOfOneArgument("Log10", FunctionSource.Log10);
    }

    internal static Function Ln
    {
        get => ArithmeticalFunctionOfOneArgument("Ln", FunctionSource.Ln);
    }

    internal static Function Sin
    {
        get => ArithmeticalFunctionOfOneArgument("Sin", FunctionSource.Sin);
    }

    internal static Function Cos
    {
        get => ArithmeticalFunctionOfOneArgument("Cos", FunctionSource.Cos);
    }

    internal static Function Tan
    {
        get => ArithmeticalFunctionOfOneArgument("Tan", FunctionSource.Tan);
    }

    internal static Function Asin
    {
        get => ArithmeticalFunctionOfOneArgument("Asin", FunctionSource.Asin);
    }

    internal static Function Acos
    {
        get => ArithmeticalFunctionOfOneArgument("Acos", FunctionSource.Acos);
    }

    internal static Function Atan
    {
        get => ArithmeticalFunctionOfOneArgument("Atan", FunctionSource.Atan);
    }

    internal static Function Sinh
    {
        get => ArithmeticalFunctionOfOneArgument("Sinh", FunctionSource.Sinh);
    }

    internal static Function Cosh
    {
        get => ArithmeticalFunctionOfOneArgument("Cosh", FunctionSource.Cosh);
    }

    internal static Function Tanh
    {
        get => ArithmeticalFunctionOfOneArgument("Tanh", FunctionSource.Tanh);
    }

    internal static Function Asinh
    {
        get => ArithmeticalFunctionOfOneArgument("Asinh", FunctionSource.Asinh);
    }

    internal static Function Acosh
    {
        get => ArithmeticalFunctionOfOneArgument("Acosh", FunctionSource.Acosh);
    }

    internal static Function Atanh
    {
        get => ArithmeticalFunctionOfOneArgument("Atanh", FunctionSource.Atanh);
    }

    internal static Function Not
    {
        get => LogicalFunctionOfOneArgument("Not", FunctionSource.Not);
    }

    internal static Function And
    {
        get => LogicalFunctionOfTwoArguments("And", FunctionSource.And);
    }

    internal static Function Or
    {
        get => LogicalFunctionOfTwoArguments("Or", FunctionSource.Or);
    }

    internal static Function Eqv
    {
        get => LogicalFunctionOfTwoArguments("Eqv", FunctionSource.Eqv);
    }

    internal static Function Imp
    {
        get => LogicalFunctionOfTwoArguments("Imp", FunctionSource.Imp);
    }

    internal static Function Xor
    {
        get => LogicalFunctionOfTwoArguments("Xor", FunctionSource.Xor);
    }

    internal static Function Nand
    {
        get => LogicalFunctionOfTwoArguments("Nand", FunctionSource.Nand);
    }

    internal static Function Nor
    {
        get => LogicalFunctionOfTwoArguments("Nor", FunctionSource.Nor);
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
