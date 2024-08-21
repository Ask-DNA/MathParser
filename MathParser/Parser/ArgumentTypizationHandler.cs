namespace MathParser;

internal class ArgumentTypizationHandler
{
    private readonly Dictionary<string, ValueDomain?> _argumentTypes;

    private readonly Dictionary<string, List<ValueDomain>> _argumentTypeRequirements;

    public ArgumentTypizationHandler(string[] argumentNames)
    {
        _argumentTypes = new(argumentNames.Length);
        _argumentTypeRequirements = new(argumentNames.Length);
        foreach (string name in argumentNames)
        {
            _argumentTypes[name] = null;
            _argumentTypeRequirements[name] = [];
        }
    }

    public void RequireArgumentType(string name, ValueDomain type) => _argumentTypeRequirements[name].Add(type);

    public ValueDomain? GetArgumentType(string name) => _argumentTypes[name];

    public bool RefreshArgumentTypes(out bool typesChanged, out ArgumentException[] argumentTypizationErrors)
    {
        typesChanged = false;
        List<ArgumentException> exceptionsTmp = [];
        foreach (string name in _argumentTypes.Keys)
        {
            if (_argumentTypes[name].HasValue)
                continue;
            ValueDomain? requiredType = null;
            foreach (ValueDomain type in _argumentTypeRequirements[name])
            {
                if (!requiredType.HasValue)
                    requiredType = type;
                else if (requiredType != type)
                    exceptionsTmp.Add(ExceptionBuilder.ArgumentTypizationErrorException(name));
            }
            if (requiredType.HasValue)
            {
                typesChanged = true;
                _argumentTypes[name] = requiredType;
            }
        }
        argumentTypizationErrors = [.. exceptionsTmp];
        return argumentTypizationErrors.Length == 0;
    }
}
