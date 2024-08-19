namespace MathParser;

public class Constant
{
    public string Name { get; init; }
    public ValueDomain Type { get; init; }
    public double Value { get; init; }

    #region BuiltInConstants

    internal static Constant Pi
    {
        get => new("Pi", ValueDomain.Double, Math.PI);
    }

    internal static Constant E
    {
        get => new("E", ValueDomain.Double, Math.E);
    }

    internal static Constant Tau
    {
        get => new("Tau", ValueDomain.Double, Math.Tau);
    }

    internal static Constant True
    {
        get => new("True", ValueDomain.Boolean, 1);
    }

    internal static Constant False
    {
        get => new("False", ValueDomain.Boolean, 0);
    }

    #endregion

    public Constant(string name, ValueDomain type, double value)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw ExceptionBuilder.NameIsNullOrWhitespaceOnObjectCreationException();
        if (!ParserGlobal.ValidateName(name))
            throw ExceptionBuilder.InvalidNamingOnObjectCreationException(name);
        if (!ParserGlobal.CheckIfValueBelongsToType(value, type))
            throw ExceptionBuilder.ValueTypeMismatchOnConstantCreationException(value, type);
        Name = name;
        Type = type;
        Value = value;
    }
}