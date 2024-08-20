using System.Text;
using LinqExpressions = System.Linq.Expressions;

namespace MathParser;

public class Parser
{
    private readonly MathCollection _mathCollection;

    public ValueDomain DefaultType { get; set; } = ValueDomain.Double;

    public bool EnableTypization { get; set; } = true;

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

        if (!PreliminaryCheck(expressionString, out exceptions))
            return false;
        
        List<ArgumentException> exceptionsTmp = [];

        if (!ParsingProcess(expressionString, out Keyword[] expressionKeywords, out exceptions))
            return false;

        string formattedExpressionStr = GetFormattedExpressionString(expressionKeywords);
        string[] argumentNames = GetArgumentNames(expressionKeywords);

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

        expression = new(formattedExpressionStr, source, argumentNames, argumentTypes, outputType);
        return true;
    }

    #region PreliminaryCheck

    private static bool PreliminaryCheck(string expressionString, out ArgumentException[] exceptions)
    {
        if (string.IsNullOrWhiteSpace(expressionString))
        {
            exceptions = [ExceptionBuilder.ExpressionStringIsNullOrWhitespaceException()];
            return false;
        }
        if (!ValidateCharacters(expressionString, out exceptions))
            return false;
        if (!ValidateParenthesis(expressionString, out ArgumentException? exception))
        {
            exceptions = [exception!];
            return false;
        }
        if (!ContainsLiteralsOrNames(expressionString, out exception))
        {
            exceptions = [exception!];
            return false;
        }

        exceptions = [];
        return true;
    }

    private static bool ValidateCharacters(string expressionString, out ArgumentException[] invalidCharacterExceptions)
    {
        List<ArgumentException> exceptionsTmp = [];
        for (int i = 0; i < expressionString.Length; i++)
            if (!ParserGlobal.ValidateCharacter(expressionString[i]))
                exceptionsTmp.Add(ExceptionBuilder.InvalidCharacterException(i));

        invalidCharacterExceptions = [.. exceptionsTmp];
        return invalidCharacterExceptions.Length == 0;
    }

    private static bool ValidateParenthesis(string expressionString, out ArgumentException? invalidParenthesisException)
    {
        int parenthesisDepth = 0;
        for (int i = 0; i < expressionString.Length; i++)
        {
            if (expressionString[i] == ParserGlobal.LeftParenthesis)
                parenthesisDepth++;
            else if (expressionString[i] == ParserGlobal.RightParenthesis)
                if (--parenthesisDepth < 0)
                    break;
        }

        if (parenthesisDepth != 0)
            invalidParenthesisException = ExceptionBuilder.InvalidParenthesisException();
        else
            invalidParenthesisException = null;

        return invalidParenthesisException is null;
    }

    private static bool ContainsLiteralsOrNames(string expressionString, out ArgumentException? expressionStringMustContainLiteralsOrNamesException)
    {
        if (!expressionString.Any(ParserGlobal.CheckIfCharacterBelongsToLiteralCharacters)
            && !expressionString.Any(ParserGlobal.CheckIfCharacterBelongsToNamingCharacters))
            expressionStringMustContainLiteralsOrNamesException = ExceptionBuilder.ExpressionStringMustContainLiteralsOrNamesException();
        else
            expressionStringMustContainLiteralsOrNamesException = null;

        return expressionStringMustContainLiteralsOrNamesException is null;
    }

    #endregion

    #region Parsing

    private bool ParsingProcess(string expressionString, out Keyword[] expressionKeywords, out ArgumentException[] exceptions)
    {
        List<ArgumentException> exceptionsTmp = [];

        expressionKeywords = SplitToKeywordArray(expressionString);
        if (!ValidateLiterals(expressionKeywords, out ArgumentException[] invalidLiteralExceptions))
            exceptionsTmp.AddRange(invalidLiteralExceptions);
        if (!ValidateNames(expressionKeywords, out ArgumentException[] invalidNameExceptions))
            exceptionsTmp.AddRange(invalidNameExceptions);

        SpecifyNames(expressionKeywords);
        if (!CheckForUndefinedFunctions(expressionKeywords, out ArgumentException[] undefinedFunctionExceptions))
            exceptionsTmp.AddRange(undefinedFunctionExceptions);
        if (!CheckForUnexpectedArgumentSeparators(expressionKeywords, out ArgumentException[] unexpectedArgumentSeparatorUsageExceptions))
            exceptionsTmp.AddRange(unexpectedArgumentSeparatorUsageExceptions);

        exceptions = [.. exceptionsTmp];
        if (exceptions.Length != 0)
            return false;

        if (!ValidateFunctionCallings(expressionKeywords, out ArgumentException[] invalidFunctionCallingExceptions))
            exceptionsTmp.AddRange(invalidFunctionCallingExceptions);
        if (!CheckForUnexpectedOperators(expressionKeywords, out ArgumentException[] unexpectedOperatorExceptions))
            exceptionsTmp.AddRange(unexpectedOperatorExceptions);
        if (!CheckForAbsentOperators(expressionKeywords, out ArgumentException[] infixOperatorIsRequiredExceptions))
            exceptionsTmp.AddRange(infixOperatorIsRequiredExceptions);
        if (!CheckForAbsentNestedExpressions(expressionKeywords, out ArgumentException[] nestedExpressionIsReguiredExceptions))
            exceptionsTmp.AddRange(nestedExpressionIsReguiredExceptions);

        exceptions = [.. exceptionsTmp];
        if (exceptions.Length != 0)
            return false;

        if (!SpecifyOperators(ref expressionKeywords, out ArgumentException[] operatorCanNotBeParsedExceptions))
            exceptionsTmp.AddRange(operatorCanNotBeParsedExceptions);

        exceptions = [.. exceptionsTmp];
        return exceptions.Length == 0;
    }

    private static Keyword[] SplitToKeywordArray(string expressionString)
    {
        List<Keyword> result = [];
        int curKeywordStartPosition = 0;
        KeywordType? prevType = null;
        KeywordType? nextType;
        StringBuilder stringBuilder = new();
        for (int i = 0; i < expressionString.Length; i++)
        {
            nextType = GetTypeByCharacter(expressionString[i], prevType);
            if (MustContinueWord(prevType, nextType))
            {
                stringBuilder.Append(expressionString[i]);
                continue;
            }
            if (MustFinalizeWord(prevType, nextType))
                result.Add(new(stringBuilder.ToString(), curKeywordStartPosition, prevType!.Value, null));
            if (MustStartNewWord(prevType, nextType))
            {
                stringBuilder.Clear();
                stringBuilder.Append(expressionString[i]);
                curKeywordStartPosition = i;
            }
            prevType = nextType;
        }
        if (MustFinalizeWord(prevType, null))
            result.Add(new(stringBuilder.ToString(), curKeywordStartPosition, prevType!.Value, null));

        return [.. result];
    }

    private static KeywordType? GetTypeByCharacter(char character, KeywordType? prevCharacterType)
    {
        switch (character)
        {
            case ParserGlobal.Whitespace:
                // Whitespace separates keywords except operator keyword, which represents sequence of several operators.
                return (prevCharacterType == KeywordType.Operator) ? KeywordType.Operator : null;
            case ParserGlobal.LeftParenthesis:
                return KeywordType.LeftParenthesis;
            case ParserGlobal.RightParenthesis:
                return KeywordType.RightParenthesis;
            case ParserGlobal.ArgumentSeparator:
                return KeywordType.ArgumentSeparator;
            default:
                // Numbers can be part of both literals and names.
                // If previous character is a part of the name, than number is the part of this name too,
                // else number is the part of the literal.
                if (prevCharacterType == KeywordType.Name && ParserGlobal.CheckIfCharacterBelongsToNamingCharacters(character))
                    return KeywordType.Name;
                if (ParserGlobal.CheckIfCharacterBelongsToLiteralCharacters(character))
                    return KeywordType.Literal;
                if (ParserGlobal.CheckIfCharacterBelongsToNamingCharacters(character))
                    return KeywordType.Name;
                if (ParserGlobal.CheckIfCharacterBelongsToOperatorCharacters(character))
                    return KeywordType.Operator;
                break;
        }
        return null;
    }

    private static bool MustStartNewWord(KeywordType? prevCharacterType, KeywordType? nextCharacterType)
    {
        if (!nextCharacterType.HasValue)
            return false;
        if (!prevCharacterType.HasValue)
            return true;
        if (prevCharacterType != nextCharacterType)
            return true;
        return prevCharacterType switch
        {
            KeywordType.LeftParenthesis => true,
            KeywordType.RightParenthesis => true,
            KeywordType.ArgumentSeparator => true,
            _ => false
        };
    }

    private static bool MustFinalizeWord(KeywordType? prevCharacterType, KeywordType? nextCharacterType)
    {
        if (!prevCharacterType.HasValue)
            return false;
        if (!nextCharacterType.HasValue)
            return true;
        if (prevCharacterType != nextCharacterType)
            return true;
        return prevCharacterType switch
        {
            KeywordType.LeftParenthesis => true,
            KeywordType.RightParenthesis => true,
            KeywordType.ArgumentSeparator => true,
            _ => false
        };
    }

    private static bool MustContinueWord(KeywordType? prevCharacterType, KeywordType? nextCharacterType)
    {
        if (!prevCharacterType.HasValue)
            return false;
        if (!nextCharacterType.HasValue)
            return false;
        if (prevCharacterType != nextCharacterType)
            return false;
        return prevCharacterType switch
        {
            KeywordType.LeftParenthesis => false,
            KeywordType.RightParenthesis => false,
            KeywordType.ArgumentSeparator => false,
            _ => true
        };
    }

    private static bool ValidateLiterals(Keyword[] expressionKeywords, out ArgumentException[] invalidLiteralExceptions)
    {
        List<ArgumentException> exceptionsTmp = [];

        foreach (Keyword k in expressionKeywords)
            if (k.Type == KeywordType.Literal && !ParserGlobal.ValidateLiteral(k.Word))
                    exceptionsTmp.Add(ExceptionBuilder.InvalidLiteralException(k.Word, k.OriginalPosition));

        invalidLiteralExceptions = [.. exceptionsTmp];
        return invalidLiteralExceptions.Length == 0;
    }

    private static bool ValidateNames(Keyword[] expressionKeywords, out ArgumentException[] invalidNameExceptions)
    {
        List<ArgumentException> exceptionsTmp = [];

        foreach (Keyword k in expressionKeywords)
            if (k.Type == KeywordType.Name && !ParserGlobal.ValidateName(k.Word))
                exceptionsTmp.Add(ExceptionBuilder.InvalidNameException(k.Word, k.OriginalPosition));

        invalidNameExceptions = [.. exceptionsTmp];
        return invalidNameExceptions.Length == 0;
    }

    private void SpecifyNames(Keyword[] expressionKeywords)
    {
        for (int i = 0; i < expressionKeywords.Length; i++)
        {
            if (expressionKeywords[i].Type == KeywordType.Name)
            {
                if (IsFunctionAtIndex(expressionKeywords, i))
                    expressionKeywords[i] = expressionKeywords[i] with { Subtype = KeywordSubtype.Function };
                else if (_mathCollection.Constants.FirstOrDefault(c => c.Name == expressionKeywords[i].Word) != null)
                    expressionKeywords[i] = expressionKeywords[i] with { Subtype = KeywordSubtype.Constant };
                else
                    expressionKeywords[i] = expressionKeywords[i] with { Subtype = KeywordSubtype.Argument };
            }
        }
    }

    private bool IsFunctionAtIndex(Keyword[] expressionKeywords, int index)
    {
        if (expressionKeywords[index].Type != KeywordType.Name)
            return false;
        if (expressionKeywords.Length <= index + 2 || expressionKeywords[index + 1].Type != KeywordType.LeftParenthesis)
            return false;
        if (_mathCollection.Functions.FirstOrDefault(f => f.Name == expressionKeywords[index].Word) != null)
            return true;

        // arity = 0 indicates that function calling expression is "f()"
        // arity = 1 indicates that function calling expression is "f(...)"
        // arity > 1 indicates that function calling expression is "f(..., ...)"
        int arity = GetFunctionCallingArity(expressionKeywords, index);

        // In case of undefined function "f", sequence "f(...)"
        // will be interpreted as implied multiplication of argument or constant "f" and expression "(...)".
        return arity != 1;
    }

    //!!!!!
    private static bool ContainsArgumentSeparatorInBrackets(Keyword[] expressionKeywords, int leftParenthesisIndex)
    {
        for (int i = leftParenthesisIndex + 1; i < expressionKeywords.Length; i++)
        {
            if (expressionKeywords[i].Type == KeywordType.RightParenthesis)
                break;
            if (expressionKeywords[i].Type == KeywordType.ArgumentSeparator)
                return true;
        }
        return false;
    }

    private bool CheckForUndefinedFunctions(Keyword[] expressionKeywords, out ArgumentException[] undefinedFunctionExceptions)
    {
        List<ArgumentException> exceptionsTmp = [];

        foreach (Keyword keyword in expressionKeywords)
            if (keyword.Subtype.HasValue && keyword.Subtype == KeywordSubtype.Function)
                if (_mathCollection.Functions.FirstOrDefault(f => f.Name == keyword.Word) == null)
                    exceptionsTmp.Add(ExceptionBuilder.UndefinedFunctionException(keyword.Word, keyword.OriginalPosition));

        undefinedFunctionExceptions = [.. exceptionsTmp];
        return undefinedFunctionExceptions.Length == 0;
    }

    private bool ValidateFunctionCallings(Keyword[] expressionKeywords, out ArgumentException[] invalidFunctionCallingExceptions)
    {
        List<ArgumentException> exceptionsTmp = [];
        int tmp;
        for (int i = 0; i < expressionKeywords.Length; i++)
        {
            if (expressionKeywords[i].Subtype != KeywordSubtype.Function)
                continue;
            tmp = GetFunctionCallingArity(expressionKeywords, i);
            if (_mathCollection.Functions.FirstOrDefault(f => f.Name == expressionKeywords[i].Word && f.Arity == tmp) == null)
                exceptionsTmp.Add(ExceptionBuilder.InvalidFunctionCallingException(expressionKeywords[i].Word, i, tmp));
        }
        invalidFunctionCallingExceptions = [.. exceptionsTmp];
        return invalidFunctionCallingExceptions.Length == 0;
    }

    // Function calling arity is zero if next symbols are '(' and ')',
    // else function calling arity is number of argument separators in function calling + 1.
    // Argument separator is part of function calling if it's relative depth is zero
    // and it is located between parenthesis symbols of function calling expression:
    // in expression "f1(a, f2(b, c)) + f3(d, e)"
    // first argument separator relational depth is 0 and it is located in function calling expression,
    // second argument separator relational depth is 1 and it is located out of function calling expression,
    // third argument separator relational depth is 0 and it is located out of function calling expression,
    // so f1 arity is 2.
    private static int GetFunctionCallingArity(Keyword[] expressionKeywords, int functionPosition)
    {
        if (expressionKeywords[functionPosition + 2].Type == KeywordType.RightParenthesis)
            return 0;
        int relativeDepth = 0;
        int argumentSeparatorCount = 0;
        for (int i = functionPosition + 2; i < expressionKeywords.Length && relativeDepth >= 0; i++)
        {
            switch (expressionKeywords[i].Type)
            {
                case KeywordType.LeftParenthesis:
                    ++relativeDepth;
                    break;
                case KeywordType.RightParenthesis:
                    --relativeDepth;
                    break;
                case KeywordType.ArgumentSeparator when relativeDepth == 0:
                    ++argumentSeparatorCount;
                    break;
            }
        }
        return argumentSeparatorCount + 1;
    }

    // Argument separator is valid if it is located between parenthesis symbols of function calling expression
    // except cases of nested parenthesis symbols:
    // in expression "f(x, (y, z))" second parenthesis symbol is invalid.
    private static bool CheckForUnexpectedArgumentSeparators(Keyword[] expressionKeywords, out ArgumentException[] unexpectedArgumentSeparatorUsageExceptions)
    {
        List<ArgumentException> exceptionsTmp = [];

        int currentDepth = 0;
        Dictionary<int, bool> isFunctionCalling = [];
        isFunctionCalling[0] = false;

        for (int i = 0; i < expressionKeywords.Length; i++)
        {
            switch (expressionKeywords[i].Type)
            {
                case KeywordType.RightParenthesis:
                    isFunctionCalling[currentDepth--] = false;
                    break;
                case KeywordType.LeftParenthesis:
                    isFunctionCalling[++currentDepth] = false;
                    break;
                case KeywordType.Name when expressionKeywords[i].Subtype == KeywordSubtype.Function:
                    isFunctionCalling[++currentDepth] = true;
                    i++;
                    break;
                case KeywordType.ArgumentSeparator when isFunctionCalling[currentDepth] == false:
                    exceptionsTmp.Add(ExceptionBuilder.UnexpectedArgumentSeparatorUsageException(expressionKeywords[i].OriginalPosition));
                    break;
            }
        }

        unexpectedArgumentSeparatorUsageExceptions = [.. exceptionsTmp];
        return unexpectedArgumentSeparatorUsageExceptions.Length == 0;
    }

    // The table cell characterizes the need to use an operator between keywords.
    // [PrevKeyword, NextKeyword]

    //         |  Func   |  Const  |   Arg   | Literal |    (    |    )    |    ,    |   END   |
    // =========================================================================================
    // Func    |         |         |         |         |         |         |         |         |
    // Const   |   [1]   |   [1]   |   [1]   |   [1]   |   [1]   |   [3]   |   [3]   |   [3]   |
    // Arg     |   [1]   |   [1]   |   [1]   |   [1]   |   [1]   |   [3]   |   [3]   |   [3]   |
    // Literal |   [1]   |   [1]   |   [1]   |   [1]   |   [1]   |   [3]   |   [3]   |   [3]   |
    // (       |   [2]   |   [2]   |   [2]   |   [2]   |   [2]   |   [4]   |         |         |
    // )       |   [1]   |   [1]   |   [1]   |   [1]   |   [1]   |   [3]   |   [3]   |   [3]   |
    // ,       |   [2]   |   [2]   |   [2]   |   [2]   |   [2]   |         |         |         |
    // BEG     |   [2]   |   [2]   |   [2]   |   [2]   |   [2]   |         |         |         |

    // [1] - Required infix operator (with optional prefix and postfix operators)
    // [2] - Optional prefix operators
    // [3] - Optional postfix operators
    // [4] - Must not contain operators
    // [ ] - Impossible combination (processed in previous stages)
    private static bool CheckForUnexpectedOperators(Keyword[] expressionKeywords, out ArgumentException[] exceptions)
    {
        List<ArgumentException> exceptionsTmp = [];
        bool unexpectedOperator;
        for (int i = 1; i < expressionKeywords.Length - 1; i++)
        {
            if (expressionKeywords[i].Type != KeywordType.Operator)
                continue;
            unexpectedOperator = (expressionKeywords[i - 1].Type, expressionKeywords[i + 1].Type) switch
            {
                (KeywordType.LeftParenthesis, KeywordType.RightParenthesis) => true,
                (KeywordType.LeftParenthesis, KeywordType.ArgumentSeparator) => true,
                (KeywordType.ArgumentSeparator, KeywordType.RightParenthesis) => true,
                (KeywordType.ArgumentSeparator, KeywordType.ArgumentSeparator) => true,
                _ => false
            };
            if (unexpectedOperator)
                exceptionsTmp.Add(ExceptionBuilder.UnexpectedOperatorUsageException(expressionKeywords[i].Word, expressionKeywords[i].OriginalPosition));
        }
        exceptions = [.. exceptionsTmp];
        return exceptions.Length == 0;
    }

    private static bool CheckForAbsentOperators(Keyword[] expressionKeywords, out ArgumentException[] exceptions)
    {
        List<ArgumentException> exceptionsTmp = [];
        bool infixOperatorIsRequired;
        for (int i = 0; i < expressionKeywords.Length - 1; i++)
        {
            infixOperatorIsRequired = (expressionKeywords[i].Type, expressionKeywords[i + 1].Type) switch
            {
                (KeywordType.Operator, _) => false,
                (_, KeywordType.Operator) => false,
                (_, KeywordType.RightParenthesis) => false,
                (_, KeywordType.ArgumentSeparator) => false,
                (KeywordType.Name, _) when expressionKeywords[i].Subtype != KeywordSubtype.Function => true,
                (KeywordType.Literal, _) => true,
                (KeywordType.RightParenthesis, _) => true,
                _ => false
            };

            if (infixOperatorIsRequired)
                exceptionsTmp.Add(ExceptionBuilder.InfixOperatorIsRequiredException(expressionKeywords[i].Word,
                                                                          expressionKeywords[i].OriginalPosition,
                                                                          expressionKeywords[i + 1].Word,
                                                                          expressionKeywords[i + 1].OriginalPosition));
        }
        exceptions = [.. exceptionsTmp];
        return exceptions.Length == 0;
    }

    // The table cell characterizes the need for a nested expression between keywords.
    // [PrevKeyword, NextKeyword]

    //         |  Func   |  Const  |   Arg   | Literal |    (    |    )    |    ,    |   END   |
    // =========================================================================================
    // Func    |         |         |         |         |         |         |         |         |
    // Const   |   [-]   |   [-]   |   [-]   |   [-]   |   [-]   |   [-]   |   [-]   |   [-]   |
    // Arg     |   [-]   |   [-]   |   [-]   |   [-]   |   [-]   |   [-]   |   [-]   |   [-]   |
    // Literal |   [-]   |   [-]   |   [-]   |   [-]   |   [-]   |   [-]   |   [-]   |   [-]   |
    // (       |   [-]   |   [-]   |   [-]   |   [-]   |   [-]   |   [*]   |   [+]   |         |
    // )       |   [-]   |   [-]   |   [-]   |   [-]   |   [-]   |   [-]   |   [-]   |   [-]   |
    // ,       |   [-]   |   [-]   |   [-]   |   [-]   |   [-]   |   [+]   |   [+]   |         |
    // BEG     |   [-]   |   [-]   |   [-]   |   [-]   |   [-]   |         |         |         |

    // [+] - Nested expression is required
    // [-] - Nested expression is not required
    // [*] - Nested expression is required if the keyword before PrevKeyword is not a function
    // [ ] - Impossible combination (processed in previous stages)
    private static bool CheckForAbsentNestedExpressions(Keyword[] expressionKeywords, out ArgumentException[] exceptions)
    {
        List<ArgumentException> exceptionsTmp = [];
        bool nestedExpressionIsRequired;
        for (int i = 0; i < expressionKeywords.Length - 1; i++)
        {
            nestedExpressionIsRequired = (expressionKeywords[i].Type, expressionKeywords[i + 1].Type) switch
            {
                (KeywordType.LeftParenthesis, KeywordType.RightParenthesis) when i > 0
                    => expressionKeywords[i - 1].Subtype != KeywordSubtype.Function,
                (KeywordType.LeftParenthesis, KeywordType.ArgumentSeparator) => true,
                (KeywordType.ArgumentSeparator, KeywordType.RightParenthesis) => true,
                (KeywordType.ArgumentSeparator, KeywordType.ArgumentSeparator) => true,
                _ => false
            };

            if (nestedExpressionIsRequired)
                exceptionsTmp.Add(ExceptionBuilder.NestedExpressionIsRequiredException(expressionKeywords[i].Word,
                                                                             expressionKeywords[i].OriginalPosition,
                                                                             expressionKeywords[i + 1].Word,
                                                                             expressionKeywords[i + 1].OriginalPosition));
        }
        exceptions = [.. exceptionsTmp];
        return exceptions.Length == 0;
    }

    // The table cell characterizes the type of operator located between keywords.
    // [PrevKeyword, NextKeyword]

    //         |  Func   |  Const  |   Arg   | Literal |    (    |    )    |    ,    |   END   |
    // =========================================================================================
    // Func    |         |         |         |         |         |         |         |         |
    // Const   |   [1]   |   [1]   |   [1]   |   [1]   |   [1]   |   [3]   |   [3]   |   [3]   |
    // Arg     |   [1]   |   [1]   |   [1]   |   [1]   |   [1]   |   [3]   |   [3]   |   [3]   |
    // Literal |   [1]   |   [1]   |   [1]   |   [1]   |   [1]   |   [3]   |   [3]   |   [3]   |
    // (       |   [2]   |   [2]   |   [2]   |   [2]   |   [2]   |         |         |         |
    // )       |   [1]   |   [1]   |   [1]   |   [1]   |   [1]   |   [3]   |   [3]   |   [3]   |
    // ,       |   [2]   |   [2]   |   [2]   |   [2]   |   [2]   |         |         |         |
    // BEG     |   [2]   |   [2]   |   [2]   |   [2]   |   [2]   |         |         |         |

    // [1] - Infix operator (with optional prefix and postfix operators)
    // [2] - Prefix operators
    // [3] - Postfix operators
    // [ ] - Impossible combination (processed in previous stages)
    private bool SpecifyOperators(ref Keyword[] expressionKeywords, out ArgumentException[] exceptions)
    {
        List<ArgumentException> exceptionsTmp = [];
        List<Keyword> expressionKeywordsTmp = [];

        KeywordType? prevKeywordType, nextKeywordType;
        string[] prefixOperatorSymbols = _mathCollection.PrefixOperatorSymbols;
        string[] infixOperatorSymbols = _mathCollection.InfixOperatorSymbols;
        string[] postfixOperatorSymbols = _mathCollection.PostfixOperatorSymbols;
        string[] possiblePrefixOperators, possibleInfixOperators, possiblePostfixOperators;
        Keyword[] curResult;
        for (int i = 0; i < expressionKeywords.Length; i++)
        {
            if (expressionKeywords[i].Type != KeywordType.Operator)
            {
                expressionKeywordsTmp.Add(expressionKeywords[i]);
                continue;
            }
            prevKeywordType = (i > 0) ? expressionKeywords[i - 1].Type : null;
            nextKeywordType = (i < expressionKeywords.Length - 1) ? expressionKeywords[i + 1].Type : null;
            switch ((prevKeywordType, nextKeywordType))
            {
                case (_, KeywordType.RightParenthesis):
                case (_, KeywordType.ArgumentSeparator):
                case (_, _) when !nextKeywordType.HasValue:
                    // Postfix operators
                    possiblePrefixOperators = [];
                    possibleInfixOperators = [];
                    possiblePostfixOperators = postfixOperatorSymbols;
                    break;
                case (KeywordType.LeftParenthesis, _):
                case (KeywordType.ArgumentSeparator, _):
                case (_, _) when !prevKeywordType.HasValue:
                    // Prefix operators
                    possiblePrefixOperators = prefixOperatorSymbols;
                    possibleInfixOperators = [];
                    possiblePostfixOperators = [];
                    break;
                default:
                    // Infix operator (with optional prefix and postfix operators)
                    possiblePrefixOperators = prefixOperatorSymbols;
                    possibleInfixOperators = infixOperatorSymbols;
                    possiblePostfixOperators = postfixOperatorSymbols;
                    break;
            }
            if (!TryParseOperatorsRecoursive(expressionKeywords[i].Word,
                                   expressionKeywords[i].OriginalPosition,
                                   possiblePrefixOperators,
                                   possibleInfixOperators,
                                   possiblePostfixOperators,
                                   out curResult))
            {
                exceptionsTmp.Add(ExceptionBuilder.OperatorCanNotBeParsedException(expressionKeywords[i].Word, expressionKeywords[i].OriginalPosition));
                expressionKeywordsTmp.Add(expressionKeywords[i]);
            }
            else
            {
                expressionKeywordsTmp.AddRange(curResult);
            }
        }
        expressionKeywords = [.. expressionKeywordsTmp];
        exceptions = [.. exceptionsTmp];
        return exceptions.Length == 0;
    }


    // Method splits the sequence of operators represented by single keyword into specified operators.
    // Each iteration represents sequence as {optional whitespace}{current operator}{optional whitespace}{optional residual sequence}
    private static bool TryParseOperatorsRecoursive(
        string operators,
        int position,
        string[] possiblePrefixOperators,
        string[] possibleInfixOperators,
        string[] possiblePostfixOperators,
        out Keyword[] result)
    {
        // Trimming string with calculation of current operator position in original expression string
        operators = operators.TrimEnd();
        while (operators.StartsWith(ParserGlobal.Whitespace))
        {
            operators = operators[1..];
            position++;
        }

        bool infixOperatorIsRequired = possibleInfixOperators.Length > 0;

        // Choosing current operator's set of valid values
        string[] possibleOperators;
        if (possiblePostfixOperators.Length != 0)
            possibleOperators = [.. possiblePostfixOperators];
        else if (possibleInfixOperators.Length != 0)
            possibleOperators = [.. possibleInfixOperators];
        else if (possiblePrefixOperators.Length != 0)
            possibleOperators = [.. possiblePrefixOperators];
        else
        {
            result = [];
            return false;
        }

        // Choosing current operator's possible values
        List<string> tmp = [];
        foreach (string operatorSymbol in possibleOperators)
            if (operators.StartsWith(operatorSymbol))
                tmp.Add(operatorSymbol);
        possibleOperators = [.. tmp];

        // Possible values parsing priority is given to most longer values,
        // so in case of operator sequence "<-" it will be interpreted as converse implication
        // rather than combination of operators LessThan and UnaryMinus.
        Array.Sort(possibleOperators, (x, y) => x.Length.CompareTo(y.Length));

        if (possibleOperators.Length == 0)
        {
            // If the search was unseccesfully performed on a set of postfix operators, current operator still may be infix operator
            if (possiblePostfixOperators.Length != 0)
                return TryParseOperatorsRecoursive(operators, position, possiblePrefixOperators, possibleInfixOperators, [], out result);
            result = [];
            return false;
        }

        for (int i = possibleOperators.Length - 1; i >= 0; i--)
        {
            // If the search was seccesfully performed on a set of postfix operators, residual sequence is empty
            // and resulting set of operators must contain infix operator, result is invalid.
            if (possibleOperators[i].Length == operators.Length && possiblePostfixOperators.Length != 0 && infixOperatorIsRequired)
                continue;

            Keyword curResult;
            if (possiblePostfixOperators.Length != 0)
                curResult = new(possibleOperators[i], position, KeywordType.Operator, KeywordSubtype.PostfixOperator);
            else if (infixOperatorIsRequired)
                curResult = new(possibleOperators[i], position, KeywordType.Operator, KeywordSubtype.InfixOperator);
            else
                curResult = new(possibleOperators[i], position, KeywordType.Operator, KeywordSubtype.PrefixOperator);

            //if (curResult.Subtype == KeywordSubtype.PrefixOperator && curResult.Word.Length == operators.Length && infixOperatorIsRequired)
            //    continue;

            // If residual sequence is empty
            if (curResult.Word.Length == operators.Length)
            {
                result = [curResult];
                return true;
            }

            // If parsing of residual sequence was seccessful
            if (TryParseOperatorsRecoursive(operators[possibleOperators[i].Length..],
                                  position + possibleOperators[i].Length,
                                  possiblePrefixOperators,
                                  // If current operator is infix than residual sequence can not contain infix operators
                                  (curResult.Subtype == KeywordSubtype.InfixOperator) ? [] : possibleInfixOperators,
                                  possiblePostfixOperators,
                                  out Keyword[] nextResult))
            {
                result = [curResult, .. nextResult];
                return true;
            }
        }

        // If the search was unseccesfully performed on a set of postfix operators, current operator still may be infix operator
        if (possiblePostfixOperators.Length != 0)
            return TryParseOperatorsRecoursive(operators, position, possiblePrefixOperators, possibleInfixOperators, [], out result);
        result = [];
        return false;
    }

    #endregion

    private static string GetFormattedExpressionString(Keyword[] expressionKeywords)
    {
        StringBuilder stringBuilder = new();
        for (int i = 0; i < expressionKeywords.Length - 1; i++)
        {
            stringBuilder.Append(expressionKeywords[i].Word);

            if (expressionKeywords[i].Type == KeywordType.ArgumentSeparator)
                stringBuilder.Append(ParserGlobal.Whitespace);
            else if (expressionKeywords[i].Subtype == KeywordSubtype.InfixOperator)
                stringBuilder.Append(ParserGlobal.Whitespace);
            else if (expressionKeywords[i + 1].Subtype == KeywordSubtype.InfixOperator)
                stringBuilder.Append(ParserGlobal.Whitespace);
        }
        stringBuilder.Append(expressionKeywords[^1].Word);
        return stringBuilder.ToString();
    }

    private static string[] GetArgumentNames(Keyword[] expressionKeywords)
    {
        List<string> result = [];
        for (int i = 0; i < expressionKeywords.Length; i++)
            if (expressionKeywords[i].Subtype == KeywordSubtype.Argument)
                result.Add(expressionKeywords[i].Word);
        return result.Distinct().ToArray();
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

    #region KeywordClass

    private enum KeywordType
    {
        Name,
        Literal,
        Operator,
        LeftParenthesis,
        RightParenthesis,
        ArgumentSeparator
    }

    private enum KeywordSubtype
    {
        Constant,
        Function,
        Argument,
        PrefixOperator,
        InfixOperator,
        PostfixOperator
    }

    private record Keyword(string Word, int OriginalPosition, KeywordType Type, KeywordSubtype? Subtype);

    #endregion

    #region ArgumentTypizationHandlerClass

    private class ArgumentTypizationHandler
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

    #endregion

    #region SyntaxNodeClass

    private class SyntaxNode
    {
        private readonly Keyword _keyword;
        // signature[0] is return type; signature[1 ..] is input types
        private List<ValueDomain[]> _possibleSignatures;
        private readonly SyntaxNode[] _children;

        public SyntaxNode(Keyword keyword, List<ValueDomain[]> possibleSignatures, SyntaxNode[] children)
        {
            _keyword = keyword;
            _possibleSignatures = new List<ValueDomain[]>(possibleSignatures.Count);
            for (int i = 0; i < possibleSignatures.Count; i++)
            {
                _possibleSignatures.Add(new ValueDomain[possibleSignatures[i].Length]);
                possibleSignatures[i].CopyTo(_possibleSignatures[i], 0);
            }
            _children = new SyntaxNode[children.Length];
            children.CopyTo(_children, 0);
        }

        public static SyntaxNode ArgumentNode(Keyword keyword)
        {
            return new(keyword, [], []);
        }

        public static SyntaxNode LiteralNode(Keyword keyword, double value)
        {
            if (value != 0 && value != 1)
                return new(keyword, [[ValueDomain.Double]], []);
            else
                return new(keyword, [[ValueDomain.Double], [ValueDomain.Boolean]], []);
        }

        public static SyntaxNode ConstantNode(Keyword keyword, ValueDomain outputType)
        {
            return new(keyword, [[outputType]], []);
        }

        public static SyntaxNode FunctionNode(Keyword keyword, ValueDomain[] signature, SyntaxNode[] children)
        {
            return new(keyword, [signature], children);
        }

        public static SyntaxNode OperatorNode(Keyword keyword, List<ValueDomain[]> possibleSignatures, SyntaxNode[] children)
        {
            return new(keyword, possibleSignatures, children);
        }

        public bool Validate(ArgumentTypizationHandler handler, out ArgumentException[] exceptions)
        {
            if (_keyword.Subtype != KeywordSubtype.Function && _keyword.Type != KeywordType.Operator)
            {
                exceptions = [];
                return true;
            }

            ValueDomain?[] childrenTypes = GetChildrenTypes(handler);
            FilterPossibleSignatures([null, .. childrenTypes]);

            ArgumentException? curNodeException = null;
            if (_possibleSignatures.Count == 0)
            {
                if (_keyword.Type == KeywordType.Operator)
                    curNodeException = ExceptionBuilder.OperatorTypizationErrorException(_keyword.Word, _keyword.OriginalPosition);
                else
                    curNodeException = ExceptionBuilder.FunctionTypizationErrorException(_keyword.Word, _keyword.OriginalPosition);
            }
            if (_possibleSignatures.Count == 1)
                SetChildrenTypes(_possibleSignatures[0][1..], handler);

            if (!ValidateChildren(handler, out ArgumentException[] childrenExceptions))
            {
                if (curNodeException is not null)
                    exceptions = [curNodeException, .. childrenExceptions];
                else
                    exceptions = [.. childrenExceptions];
                return false;
            }
            if (curNodeException is not null)
                exceptions = [curNodeException];
            else
                exceptions = [];
            return exceptions.Length == 0;
        }

        private void FilterPossibleSignatures(ValueDomain?[] filter)
        {
            List<ValueDomain[]> possibleSignatures = [];
            bool fits;
            for (int i = 0; i < _possibleSignatures.Count; i++)
            {
                fits = true;
                for (int j = 0; j < _possibleSignatures[i].Length; j++)
                {
                    if (filter[j].HasValue && filter[j] != _possibleSignatures[i][j])
                        fits = false;
                }
                if (fits)
                    possibleSignatures.Add(_possibleSignatures[i]);
            }
            _possibleSignatures = possibleSignatures;
        }

        private ValueDomain?[] GetChildrenTypes(ArgumentTypizationHandler handler)
        {
            ValueDomain?[] childrenTypes = new ValueDomain?[_children.Length];
            for (int i = 0; i < _children.Length; i++)
                childrenTypes[i] = _children[i].GetOutputType(handler);
            return childrenTypes;
        }

        private void SetChildrenTypes(ValueDomain[] types, ArgumentTypizationHandler handler)
        {
            for (int i = 0; i < _children.Length; i++)
                _children[i].SetOutputType(types[i], handler);
        }

        private bool ValidateChildren(ArgumentTypizationHandler handler, out ArgumentException[] exceptions)
        {
            List<ArgumentException> exceptionsTmp = [];
            foreach (SyntaxNode child in _children)
                if (!child.Validate(handler, out ArgumentException[] childExceptionsTmp))
                    exceptionsTmp.AddRange(childExceptionsTmp);
            exceptions = [.. exceptionsTmp];
            return exceptions.Length == 0;
        }

        public ValueDomain? GetOutputType(ArgumentTypizationHandler handler)
        {
            if (_keyword.Subtype == KeywordSubtype.Argument)
                return handler.GetArgumentType(_keyword.Word);

            ValueDomain? type = _possibleSignatures[0][0];
            foreach (ValueDomain[] signature in _possibleSignatures)
            {
                if (type.HasValue && type != signature[0])
                {
                    type = null;
                    break;
                }
            }
            return type;
        }

        private void SetOutputType(ValueDomain type, ArgumentTypizationHandler handler)
        {
            if (_keyword.Subtype == KeywordSubtype.Argument)
                handler.RequireArgumentType(_keyword.Word, type);

            else if (_keyword.Type == KeywordType.Operator || _keyword.Subtype == KeywordSubtype.Function)
            {
                ValueDomain?[] filter = new ValueDomain?[_possibleSignatures[0].Length];
                Array.Fill(filter, null);
                filter[0] = type;
                FilterPossibleSignatures(filter);
            }
            return;
        }
    }

    #endregion
}
