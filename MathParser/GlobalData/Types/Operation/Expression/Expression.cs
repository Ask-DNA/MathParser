namespace MathParser;

public sealed class Expression : Operation
{
    public string ExpressionString { get; init; }

    internal Expression(
        string expressionString,
        Func<double[], double> source,
        string[] arguments,
        ValueDomain[] argumentTypes,
        ValueDomain outputType)
    {
        SetSourceIfValid(source, arguments, argumentTypes, outputType);
        ExpressionString = expressionString;
    }

    public Expression(string expressionString, Parser? parser = null)
    {
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
        ExpressionString = tmp.ExpressionString;
    }

    public double Calculate(params double[] arguments)
    {
        if (arguments.Length != Arity)
            throw ExceptionBuilder.ArityMismatchException(Arity);

        string[] argumentNames = Arguments;
        ValueDomain[] argumentTypes = ArgumentTypes;

        for (int i = 0; i < arguments.Length; i++)
            if (!ParserGlobal.CheckIfValueBelongsToType(arguments[i], argumentTypes[i]))
                throw ExceptionBuilder.SignatureMismatchException(arguments, argumentTypes);

        return Source(arguments);
    }

    public bool TryCalculate(out double result, params double[] arguments)
    {
        result = 0;

        if (arguments.Length != Arity)
            return false;

        string[] argumentNames = Arguments;
        ValueDomain[] argumentTypes = ArgumentTypes;

        for (int i = 0; i < arguments.Length; i++)
            if (!ParserGlobal.CheckIfValueBelongsToType(arguments[i], argumentTypes[i]))
                return false;

        result = Source(arguments);
        return true;
    }
}
