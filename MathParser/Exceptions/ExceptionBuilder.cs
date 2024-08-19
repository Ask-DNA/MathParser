using System.Text;

namespace MathParser;

internal static class ExceptionBuilder
{
    private static string EnumerateStrings(string[] strings)
    {
        if (strings.Length == 0)
            return string.Empty;
        else if (strings.Length == 1)
            return strings[0].ToString();

        StringBuilder builder = new(strings[0]);
        for (int i = 1; i < strings.Length; i++)
            builder.Append($", {strings[i]}");

        return builder.ToString();
    }

    private static string EnumerateNumbers(double[] numbers)
    {
        return EnumerateStrings([.. Array.ConvertAll(numbers, i => i.ToString())]);
    }

    private static string EnumerateIndices(int[] indices)
    {
        return EnumerateNumbers([.. indices.Order()]);
    }

    private static string EnumerateIndicesWithValues(Dictionary<int, string> values)
    {
        int[] orderedIndices = [.. values.Keys.Order()];
        StringBuilder builder = new(EnumerateIndices(orderedIndices));

        builder.Append($" ('{values[orderedIndices[0]]}'");
        for (int i = 1; i < orderedIndices.Length; i++)
            builder.Append($", '{values[orderedIndices[i]]}'");
        builder.Append(')');

        return builder.ToString();
    }

    #region OnObjectCreation

    public static ArgumentException NameIsNullOrWhitespaceOnObjectCreationException()
    {
        return new("Name must be non-null and non-whitespace.");
    }

    public static ArgumentException InvalidNamingOnObjectCreationException(string name)
    {
        return new($"Name '{name}' is invalid.");
    }

    public static ArgumentException ValueTypeMismatchOnConstantCreationException(double value, ValueDomain dataType)
    {
        return new($"Value {value} can not be treated as {dataType}.");
    }

    public static ArgumentException ErrorOccuredWhileParsingException(string expressionStr, Exception innerException)
    {
        return new($"An error occured while parsing '{expressionStr}'.", innerException);
    }

    #endregion

    #region OnCalculating

    public static ArgumentException ArityMismatchException(int arity)
    {
        return new($"The size of the input array must be equal to {arity}.");
    }

    public static ArgumentException SignatureMismatchException(double[] inputArguments, ValueDomain[] expectedArgumentsTypes)
    {
        if (inputArguments.Length != expectedArgumentsTypes.Length || inputArguments.Length == 0)
            return new("Signature mismatch.");
        else if (inputArguments.Length == 1)
            return new($"Signature mismatch: argument {inputArguments[0]} can not be treated as {expectedArgumentsTypes[0]}");
        else
            return new($"Signature mismatch: arguments [{EnumerateNumbers(inputArguments)}] can not be treated as [{EnumerateStrings(Array.ConvertAll(expectedArgumentsTypes, t => t.ToString()))}].");
    }

    #endregion

    #region OnMathCollectionProcessing

    public static AggregateException SeveralErrorsDuringMathCollectionProcessingException(IEnumerable<Exception> innerExceptions)
    {
        string message = $"Several errors ({innerExceptions.Count()}) occured during math collection processing.";
        return new(message, innerExceptions);
    }

    public static ArgumentException ConstantNotFoundException(string constantName)
    {
        return new($"Constant '{constantName}' not found.");
    }

    public static ArgumentException FunctionNotFoundException(string functionName, int arity)
    {
        return new($"Function '{functionName}' with arity '{arity}' not found.");
    }

    public static ArgumentException ConflictOnConstantInsertionException(string constantName)
    {
        return new($"Constant '{constantName}' has already been added.");
    }

    public static ArgumentException ConflictOnFunctionInsertionException(string functionName, int arity)
    {
        return new($"Function '{functionName}' with arity '{arity}' has already been added.");
    }

    public static ArgumentException DuplicateInInputFunctionArray(string functionName, int arity)
    {
        return new($"Duplicate function '{functionName}' with arity '{arity}' in input array.");
    }

    public static ArgumentException DuplicateInInputConstantArray(string constantName)
    {
        return new($"Duplicate constant '{constantName}' in input array.");
    }

    #endregion

    #region OnParsing

    public static AggregateException SeveralErrorsWhileParsingException(string expressionStr, IEnumerable<Exception> innerExceptions)
    {
        string message = $"Several errors ({innerExceptions.Count()}) occured while parsing expression ('{expressionStr}').";
        return new(message, innerExceptions);
    }

    public static ArgumentException ExpressionStringIsNullOrWhitespaceException()
    {
        return new("Expression string must be non-null and non-whitespace.");
    }

    public static ArgumentException ExpressionStringMustContainLiteralsOrNamesException()
    {
        return new("Expression string must contain at least one literal, constant, argument or function calling.");
    }

    public static ArgumentException InvalidCharactersException(int[] invalidCharacterIndices)
    {
        StringBuilder builder;

        if (invalidCharacterIndices.Length == 0)
            return new("Invalid characters.");
        else if (invalidCharacterIndices.Length == 1)
            builder = new("Invalid character at index ");
        else
            builder = new("Invalid characters at indices ");

        builder.Append($"{EnumerateIndices(invalidCharacterIndices)}.");

        return new(builder.ToString());
    }

    public static ArgumentException InvalidParenthesisException()
    {
        return new("Invalid parenthesis.");
    }

    public static ArgumentException InvalidLiteralsException(Dictionary<int, string> invalidLiterals)
    {
        StringBuilder builder;

        if (invalidLiterals.Count == 0)
            return new("Invalid literals.");
        else if (invalidLiterals.Count == 1)
            builder = new("Invalid literal at index ");
        else
            builder = new("Invalid literals at indices ");

        builder.Append($"{EnumerateIndicesWithValues(invalidLiterals)}.");

        return new(builder.ToString());
    }

    public static ArgumentException InvalidNamesException(Dictionary<int, string> InvalidNames)
    {
        StringBuilder builder;

        if (InvalidNames.Count == 0)
            return new ("Invalid naming.");
        else if (InvalidNames.Count == 1)
            builder = new("Invalid name at index ");
        else
            builder = new("Invalid names at indices ");

        builder.Append($"{EnumerateIndicesWithValues(InvalidNames)}.");

        return new(builder.ToString());
    }

    public static ArgumentException UndefinedFunctionsException(Dictionary<int, string> undefinedFunctions)
    {
        StringBuilder builder;

        if (undefinedFunctions.Count == 0)
            return new("Undefined functions.");
        else if (undefinedFunctions.Count == 1)
            builder = new("Undefined function at index ");
        else
            builder = new("Undefined functions at indices ");

        builder.Append($"{EnumerateIndicesWithValues(undefinedFunctions)}.");

        return new(builder.ToString());
    }

    public static ArgumentException UnexpectedArgumentSeparatorUsageException(int[] unexpectedArgumentSeparatorIndices)
    {
        StringBuilder builder;

        if (unexpectedArgumentSeparatorIndices.Length == 0)
            return new("Unexpected argument separator usage.");
        else if (unexpectedArgumentSeparatorIndices.Length == 1)
            builder = new("Unexpected argument separator usage at index ");
        else
            builder = new("Unexpected argument separator usage at indices ");

        builder.Append($"{EnumerateIndices(unexpectedArgumentSeparatorIndices)}.");

        return new(builder.ToString());
    }

    public static ArgumentException InfixOperatorIsRequiredException(string prevElement, int prevElementIndex, string nextElement, int nextElementIndex)
    {
        return new($"Infix operator is required between elements at indices {prevElementIndex} ('{prevElement}') and {nextElementIndex} ('{nextElement}')");
    }

    public static ArgumentException NestedExpressionIsRequiredException(string prevElement, int prevElementIndex, string nextElement, int nextElementIndex)
    {
        return new($"Nested expression is required between elements at indices {prevElementIndex} ('{prevElement}') and {nextElementIndex} ('{nextElement}')");
    }

    public static ArgumentException UnexpectedOperatorUsageException(string operatorStr, int index)
    {
        return new($"Unexpected operator usage at index {index} ('{operatorStr}')");
    }

    public static ArgumentException OperatorCanNotBeParsedException(string operatorStr, int index)
    {
        if (operatorStr.Length == 1)
            return new($"Operator '{operatorStr}' at index {index} can not be parsed.");
        return new($"The sequence of characters '{operatorStr}' at index {index} can not be converted to an operator.");
    }

    public static ArgumentException InvalidFunctionCallingException(string functionName, int index, int numberOfArguments)
    {
        return new($"Invalid function calling at index {index} ('{functionName}'). Implementation not found for number of arguments: {numberOfArguments}.");
    }

    public static ArgumentException OperatorTypizationErrorException(string operatorStr, int index)
    {
        return new($"Typization error while evaluating operator '{operatorStr}' at index {index}.");
    }

    public static ArgumentException FunctionTypizationErrorException(string functionName, int index)
    {
        return new($"Typization error while evaluating function '{functionName}' at index {index}.");
    }

    public static ArgumentException ArgumentTypizationErrorException(string argumentName)
    {
        return new($"Unable to determine argument type ('{argumentName}').");
    }

    #endregion
}
