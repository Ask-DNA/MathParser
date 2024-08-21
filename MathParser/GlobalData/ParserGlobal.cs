using System.Text.RegularExpressions;

namespace MathParser;

internal static partial class ParserGlobal
{
    private static readonly char[] _operatorCharacters;
    private static readonly char[] _literalCharacters;
    private static readonly char[] _namingCharacters;
    private static readonly char[] _validCharacters;

    private static Operator[] _builtInOperators = [];
    private static Function[] _builtInFunctions = [];
    private static Constant[] _builtInConstants = [];

    // Leading zeros double pattern: ^([0-9]\d*)(\.\d+)?$
    // No leading zeros double pattern: ^(\d*)(\.\d+)?$
    private const string _nameRegexPattern = @"^[A-Za-z_][A-Za-z0-9_]*$";
    private const string _doubleLiteralRegexPattern = @"^([1-9]\d*|0)(\.\d+)?$";
    private const string _booleanLiteralRegexPattern = @"^(1|0)(\.0+)?$";

    public const char LeftParenthesis = '(';
    public const char RightParenthesis = ')';
    public const char ArgumentSeparator = ',';
    public const char Whitespace = ' ';

    internal static Operator[] BuiltInOperators
    {
        get
        {
            if (_builtInOperators.Length == 0)
                _builtInOperators = GetBuiltInOperators();
            return _builtInOperators;
        }
    }

    internal static Function[] BuiltInFunctions
    {
        get
        {
            if (_builtInFunctions.Length == 0)
                _builtInFunctions = GetBuiltInFunctions();
            return _builtInFunctions;
        }
    }

    internal static Constant[] BuiltInConstants
    {
        get
        {
            if (_builtInConstants.Length == 0)
                _builtInConstants = GetBuiltInConstants();
            return _builtInConstants;
        }
    }

    static ParserGlobal()
    {
        _operatorCharacters = GetOperatorCharacters();
        _literalCharacters = GetLiteralCharacters();
        _namingCharacters = GetNamingCharacters();

        List<char> tmp = new(_operatorCharacters);
        tmp.AddRange(_literalCharacters);
        tmp.AddRange(_namingCharacters);
        tmp.AddRange([LeftParenthesis, RightParenthesis, ArgumentSeparator, Whitespace]);
        _validCharacters = tmp.Distinct().ToArray();
    }

    private static char[] GetOperatorCharacters()
    {
        return ['+', '-', '*', '/', '%', '^', '!', '~', '&', '|', '<', '>', '=', '?'];
    }

    private static char[] GetLiteralCharacters()
    {
        char[] result = new char[11];

        // Unicode characters 0-9
        for (int i = 48, j = 0; i < 58; i++, j++)
            result[j] = (char)i;

        result[10] = '.';

        return result;
    }

    private static char[] GetNamingCharacters()
    {
        char[] result = new char[63];

        // Unicode characters A-Z
        for (int i = 65, j = 0; i < 91; i++, j++)
            result[j] = (char)i;

        // Unicode characters a-z
        for (int i = 97, j = 26; i < 123; i++, j++)
            result[j] = (char)i;

        // Unicode characters 0-9
        for (int i = 48, j = 52; i < 58; i++, j++)
            result[j] = (char)i;

        result[62] = '_';

        return result;
    }

    private static Operator[] GetBuiltInOperators()
    {
        return 
            [
                Operator.UnaryPlus,
                Operator.UnaryMinus,
                Operator.Addition,
                Operator.Subtraction,
                Operator.Division,
                Operator.Modulation,
                Operator.Multiplication,
                Operator.Exponentiation,
                Operator.Factorial,

                Operator.LogicNegation1,
                Operator.LogicNegation2,
                Operator.Conjunction1,
                Operator.Conjunction2,
                Operator.Disjunction1,
                Operator.Disjunction2,
                Operator.Equivalence,
                Operator.Implication,
                Operator.ConverseImplication,

                Operator.DoubleEquality,
                Operator.DoubleInequality1,
                Operator.DoubleInequality2,
                Operator.BooleanEquality,
                Operator.BooleanInequality1,
                Operator.BooleanInequality2,

                Operator.LessThan,
                Operator.LessThanOrEqual,
                Operator.GreaterThan,
                Operator.GreaterThanOrEqual,

                Operator.DoubleCoalesce,
                Operator.BooleanCoalesce
            ];
    }

    private static Function[] GetBuiltInFunctions()
    {
        return
            [
                Function.Abs,
                Function.Sqrt,
                Function.Cbrt,
                Function.Exp,
                Function.Pow,
                Function.Log,
                Function.Log2,
                Function.Log10,
                Function.Ln,
                Function.Sin,
                Function.Cos,
                Function.Tan,
                Function.Asin,
                Function.Acos,
                Function.Atan,
                Function.Sinh,
                Function.Cosh,
                Function.Tanh,
                Function.Asinh,
                Function.Acosh,
                Function.Atanh,
                Function.Not,
                Function.And,
                Function.Or,
                Function.Eqv,
                Function.Imp,
                Function.Xor,
                Function.Nand,
                Function.Nor
            ];
    }

    private static Constant[] GetBuiltInConstants()
    {
        return
            [
                Constant.Pi,
                Constant.E,
                Constant.Tau,
                Constant.True,
                Constant.False
            ];
    }

    public static bool ValidateCharacter(char character)
    {
        return _validCharacters.Contains(character);
    }

    public static bool ValidateName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;
        
        for (int i = 0; i < name.Length; i++)
            if (!_namingCharacters.Contains(name[i]))
                return false;

        return NameRegex().IsMatch(name);
    }

    public static bool ValidateLiteral(string literal, ValueDomain? type = null)
    {
        if (string.IsNullOrEmpty(literal))
            return false;

        for (int i = 0; i < literal.Length; i++)
            if (!_literalCharacters.Contains(literal[i]))
                return false;

        if (type == null || type == ValueDomain.Double)
            return DoubleLiteralRegex().IsMatch(literal);
        else
            return BooleanLiteralRegex().IsMatch(literal);
    }

    public static bool CheckIfValueBelongsToType(double value, ValueDomain type)
    {
        return type switch
        {
            ValueDomain.Double => Double.IsFinite(value),
            ValueDomain.Boolean => (value == 1 || value == 0),
            _ => false
        };
    }

    public static bool CheckIfCharacterBelongsToOperatorCharacters(char character)
    {
        return _operatorCharacters.Contains(character);
    }

    public static bool CheckIfCharacterBelongsToLiteralCharacters(char character)
    {
        return _literalCharacters.Contains(character);
    }

    public static bool CheckIfCharacterBelongsToNamingCharacters(char character)
    {
        return _namingCharacters.Contains(character);
    }

    public static double ParseLiteral(string literal)
    {
        return double.Parse(literal, System.Globalization.CultureInfo.InvariantCulture);
    }

    [GeneratedRegex(_doubleLiteralRegexPattern)]
    private static partial Regex DoubleLiteralRegex();

    [GeneratedRegex(_booleanLiteralRegexPattern)]
    private static partial Regex BooleanLiteralRegex();

    [GeneratedRegex(_nameRegexPattern)]
    private static partial Regex NameRegex();
}
