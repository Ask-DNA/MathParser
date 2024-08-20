using System.Text;

namespace MathParser;

internal class SyntaxAnalyzer
{
    private readonly Constant[] _usedConstants;
    private readonly Function[] _usedFunctions;
    private readonly string[] _usedPrefixOperatorSymbols;
    private readonly string[] _usedInfixOperatorSymbols;
    private readonly string[] _usedPostfixOperatorSymbols;

    private readonly List<ArgumentException> _accumulatedExceptions = [];

    public SyntaxAnalyzer(Constant[] usedConstants,
                          Function[] usedFunctions,
                          string[] usedPrefixOperatorSymbols,
                          string[] usedInfixOperatorSymbols,
                          string[] usedPostfixOperatorSymbols)
    {
        _usedConstants = new Constant[usedConstants.Length];
        usedConstants.CopyTo(_usedConstants, 0);

        _usedFunctions = new Function[usedFunctions.Length];
        usedFunctions.CopyTo(_usedFunctions, 0);

        _usedPrefixOperatorSymbols = new string[usedPrefixOperatorSymbols.Length];
        usedPrefixOperatorSymbols.CopyTo(_usedPrefixOperatorSymbols, 0);

        _usedInfixOperatorSymbols = new string[usedInfixOperatorSymbols.Length];
        usedInfixOperatorSymbols.CopyTo(_usedInfixOperatorSymbols, 0);

        _usedPostfixOperatorSymbols = new string[usedPostfixOperatorSymbols.Length];
        usedPostfixOperatorSymbols.CopyTo(_usedPostfixOperatorSymbols, 0);
    }

    public bool Run(string expressionString,
                    out Keyword[] expressionKeywords,
                    out string[] argumentNames,
                    out string formattedExpressionString,
                    out ArgumentException[] exceptions)
    {
        _accumulatedExceptions.Clear();
        argumentNames = [];
        formattedExpressionString = string.Empty;
        exceptions = [];
        expressionKeywords = [];

        if (!PreliminaryCheck(expressionString))
        {
            exceptions = new ArgumentException[_accumulatedExceptions.Count];
            _accumulatedExceptions.CopyTo(exceptions, 0);
            return false;
        }

        expressionKeywords = SplitToKeywordArray(expressionString);
        ValidateLiteralsAndNames(expressionKeywords);
        SpecifyNames(expressionKeywords);
        ValidateFunctions(expressionKeywords);
        ValidateArgumentSeparators(expressionKeywords);

        if (_accumulatedExceptions.Count > 0)
        {
            exceptions = new ArgumentException[_accumulatedExceptions.Count];
            _accumulatedExceptions.CopyTo(exceptions, 0);
            return false;
        }

        ValidateOperatorsUsage(expressionKeywords);
        ValidateNestedExpressionsUsage(expressionKeywords);

        if (_accumulatedExceptions.Count > 0)
        {
            exceptions = new ArgumentException[_accumulatedExceptions.Count];
            _accumulatedExceptions.CopyTo(exceptions, 0);
            return false;
        }

        if (!SpecifyOperators(ref expressionKeywords))
        {
            exceptions = new ArgumentException[_accumulatedExceptions.Count];
            _accumulatedExceptions.CopyTo(exceptions, 0);
            return false;
        }

        argumentNames = GetArgumentNames(expressionKeywords);
        formattedExpressionString = GetFormattedExpressionString(expressionKeywords);
        return true;
    }

    public bool Run(string expressionString,
                    out Keyword[] expressionKeywords,
                    out string[] argumentNames,
                    out string formattedExpressionString)
    {
        return Run(expressionString, out expressionKeywords, out argumentNames, out formattedExpressionString, out _);
    }

    private bool PreliminaryCheck(string expressionString)
    {
        if (string.IsNullOrWhiteSpace(expressionString))
        {
            _accumulatedExceptions.Add(ExceptionBuilder.ExpressionStringIsNullOrWhitespaceException());
            return false;
        }
        if (!ValidateCharacters(expressionString))
            return false;
        if (!ValidateParenthesis(expressionString))
            return false;
        if (!expressionString.Any(ParserGlobal.CheckIfCharacterBelongsToLiteralCharacters)
            && !expressionString.Any(ParserGlobal.CheckIfCharacterBelongsToNamingCharacters))
        {
            _accumulatedExceptions.Add(ExceptionBuilder.ExpressionStringMustContainLiteralsOrNamesException()); ;
            return false;
        }
        return true;
    }

    private bool ValidateCharacters(string expressionString)
    {
        bool valid = true;

        for (int i = 0; i < expressionString.Length; i++)
        {
            if (!ParserGlobal.ValidateCharacter(expressionString[i]))
            {
                valid = false;
                _accumulatedExceptions.Add(ExceptionBuilder.InvalidCharacterException(i));
            }
        }
        
        return valid;
    }

    private bool ValidateParenthesis(string expressionString)
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
            _accumulatedExceptions.Add(ExceptionBuilder.InvalidParenthesisException());

        return parenthesisDepth == 0;
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
                // If previous character is a part of a name, than number is the part of this name too,
                // else number is the part of a literal.
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

    private bool ValidateLiteralsAndNames(Keyword[] expressionKeywords)
    {
        bool valid = true;

        foreach (Keyword k in expressionKeywords)
        {
            if (k.Type == KeywordType.Literal && !ParserGlobal.ValidateLiteral(k.Word))
            {
                _accumulatedExceptions.Add(ExceptionBuilder.InvalidLiteralException(k.Word, k.OriginalPosition));
                valid = false;
            }
            else if (k.Type == KeywordType.Name && !ParserGlobal.ValidateName(k.Word))
            {
                _accumulatedExceptions.Add(ExceptionBuilder.InvalidNameException(k.Word, k.OriginalPosition));
                valid = false;
            }
        }
        
        return valid;
    }

    private void SpecifyNames(Keyword[] expressionKeywords)
    {
        for (int i = 0; i < expressionKeywords.Length; i++)
        {
            if (expressionKeywords[i].Type == KeywordType.Name)
            {
                if (IsFunctionAtIndex(expressionKeywords, i))
                    expressionKeywords[i] = expressionKeywords[i] with { Subtype = KeywordSubtype.Function };
                else if (_usedConstants.FirstOrDefault(c => c.Name == expressionKeywords[i].Word) != null)
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
        if (_usedFunctions.FirstOrDefault(f => f.Name == expressionKeywords[index].Word) != null)
            return true;

        // arity = 0 indicates that function calling expression is "f()"
        // arity = 1 indicates that function calling expression is "f(...)"
        // arity > 1 indicates that function calling expression is "f(..., ...)"
        int arity = GetFunctionCallingArity(expressionKeywords, index);

        // In case of undefined function "f", sequence "f(...)"
        // will be interpreted as implied multiplication of argument or constant "f" and expression "(...)".
        return arity != 1;
    }

    private bool ValidateFunctions(Keyword[] expressionKeywords)
    {
        bool valid = true;
        int functionArity;

        for (int i = 0; i < expressionKeywords.Length; i++)
        {
            if (expressionKeywords[i].Subtype != KeywordSubtype.Function)
                continue;

            if (_usedFunctions.FirstOrDefault(f => f.Name == expressionKeywords[i].Word) == null)
            {
                valid = false;
                _accumulatedExceptions.Add(ExceptionBuilder.UndefinedFunctionException(expressionKeywords[i].Word,
                                                                                       expressionKeywords[i].OriginalPosition));
                continue;
            }

            functionArity = GetFunctionCallingArity(expressionKeywords, i);
            if (_usedFunctions.FirstOrDefault(f => f.Name == expressionKeywords[i].Word && f.Arity == functionArity) == null)
            {
                valid = false;
                _accumulatedExceptions.Add(ExceptionBuilder.InvalidFunctionCallingException(expressionKeywords[i].Word,
                                                                                            expressionKeywords[i].OriginalPosition,
                                                                                            functionArity));
            }
        }

        return valid;
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
    private bool ValidateArgumentSeparators(Keyword[] expressionKeywords)
    {
        bool valid = true;

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
                    valid = false;
                    _accumulatedExceptions.Add(ExceptionBuilder.UnexpectedArgumentSeparatorUsageException(expressionKeywords[i].OriginalPosition));
                    break;
            }
        }

        return valid;
    }

    // The table below characterizes the need to use an operator between keywords.
    // [PrevKeyword, NextKeyword]

    //         |  Func   |  Const  |   Arg   | Literal |    (    |    )    |    ,    |   END   |
    // =========================================================================================
    // Func    |         |         |         |         |   [4]   |         |         |         |
    // Const   |   [1]   |   [1]   |   [1]   |   [1]   |   [1]   |   [3]   |   [3]   |   [3]   |
    // Arg     |   [1]   |   [1]   |   [1]   |   [1]   |   [1]   |   [3]   |   [3]   |   [3]   |
    // Literal |   [1]   |   [1]   |   [1]   |   [1]   |   [1]   |   [3]   |   [3]   |   [3]   |
    // (       |   [2]   |   [2]   |   [2]   |   [2]   |   [2]   |   [4]   |   [4]   |         |
    // )       |   [1]   |   [1]   |   [1]   |   [1]   |   [1]   |   [3]   |   [3]   |   [3]   |
    // ,       |   [2]   |   [2]   |   [2]   |   [2]   |   [2]   |   [4]   |   [4]   |         |
    // BEG     |   [2]   |   [2]   |   [2]   |   [2]   |   [2]   |         |         |         |

    // [1] - Required infix operator (with optional prefix and postfix operators)
    // [2] - Optional prefix operators
    // [3] - Optional postfix operators
    // [4] - Must not contain operators
    // [ ] - Impossible combination (processed in previous stages)
    private bool ValidateOperatorsUsage(Keyword[] expressionKeywords)
    {
        bool valid = true;

        bool unexpectedOperator;
        bool operatorIsRequired;
        KeywordType? prevKeywordType;
        KeywordType curKeywordType, nextKeywordType;
        for (int i = 0; i < expressionKeywords.Length - 1; i++)
        {
            prevKeywordType = (i == 0) ? null : expressionKeywords[i - 1].Type;
            curKeywordType = expressionKeywords[i].Type;
            nextKeywordType = expressionKeywords[i + 1].Type;

            unexpectedOperator = (prevKeywordType, nextKeywordType) switch
            {
                (_, _) when curKeywordType != KeywordType.Operator => false,
                (KeywordType.LeftParenthesis, KeywordType.RightParenthesis) => true,
                (KeywordType.LeftParenthesis, KeywordType.ArgumentSeparator) => true,
                (KeywordType.ArgumentSeparator, KeywordType.RightParenthesis) => true,
                (KeywordType.ArgumentSeparator, KeywordType.ArgumentSeparator) => true,
                _ => false
            };

            operatorIsRequired = (curKeywordType, nextKeywordType) switch
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

            if (unexpectedOperator)
            {
                valid = false;
                _accumulatedExceptions.Add(ExceptionBuilder.UnexpectedOperatorUsageException(expressionKeywords[i].Word,
                                                                                             expressionKeywords[i].OriginalPosition));
            }
                
            if (operatorIsRequired)
            {
                valid = false;
                _accumulatedExceptions.Add(ExceptionBuilder.InfixOperatorIsRequiredException(expressionKeywords[i].Word,
                                                                                             expressionKeywords[i].OriginalPosition,
                                                                                             expressionKeywords[i + 1].Word,
                                                                                             expressionKeywords[i + 1].OriginalPosition));
            }
        }
        
        return valid;
    }

    // The table below characterizes the need for a nested expression between keywords.
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
    private bool ValidateNestedExpressionsUsage(Keyword[] expressionKeywords)
    {
        bool valid = true;
        
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
            {
                valid = false;
                _accumulatedExceptions.Add(ExceptionBuilder.NestedExpressionIsRequiredException(expressionKeywords[i].Word,
                                                                                                expressionKeywords[i].OriginalPosition,
                                                                                                expressionKeywords[i + 1].Word,
                                                                                                expressionKeywords[i + 1].OriginalPosition));
            }
        }
        
        return valid;
    }

    // The table below characterizes the type of operator located between keywords.
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
    private bool SpecifyOperators(ref Keyword[] expressionKeywords)
    {
        bool valid = true;
        List<Keyword> expressionKeywordsTmp = [];

        KeywordType? prevKeywordType, nextKeywordType;
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
                    possiblePostfixOperators = _usedPostfixOperatorSymbols;
                    break;
                case (KeywordType.LeftParenthesis, _):
                case (KeywordType.ArgumentSeparator, _):
                case (_, _) when !prevKeywordType.HasValue:
                    // Prefix operators
                    possiblePrefixOperators = _usedPrefixOperatorSymbols;
                    possibleInfixOperators = [];
                    possiblePostfixOperators = [];
                    break;
                default:
                    // Infix operator (with optional prefix and postfix operators)
                    possiblePrefixOperators = _usedPrefixOperatorSymbols;
                    possibleInfixOperators = _usedInfixOperatorSymbols;
                    possiblePostfixOperators = _usedPostfixOperatorSymbols;
                    break;
            }
            if (!TryParseOperatorsRecoursive(expressionKeywords[i].Word,
                                             expressionKeywords[i].OriginalPosition,
                                             possiblePrefixOperators,
                                             possibleInfixOperators,
                                             possiblePostfixOperators,
                                             out curResult))
            {
                valid = false;
                _accumulatedExceptions.Add(ExceptionBuilder.OperatorCanNotBeParsedException(expressionKeywords[i].Word,
                                                                                            expressionKeywords[i].OriginalPosition));
                expressionKeywordsTmp.Add(expressionKeywords[i]);
            }
            else
            {
                expressionKeywordsTmp.AddRange(curResult);
            }
        }
        expressionKeywords = [.. expressionKeywordsTmp];
        return valid;
    }


    // Method splits a sequence of operators represented by a single keyword into specified operators.
    // Each iteration represents a sequence as {optional whitespace}{current operator}{optional residual sequence}
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
            // If search was unseccesfully performed on a set of postfix operators, current operator still may be infix operator
            if (possiblePostfixOperators.Length != 0)
                return TryParseOperatorsRecoursive(operators, position, possiblePrefixOperators, possibleInfixOperators, [], out result);
            result = [];
            return false;
        }

        for (int i = possibleOperators.Length - 1; i >= 0; i--)
        {
            // If search was seccesfully performed on a set of postfix operators, residual sequence is empty
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

        // If search was unseccesfully performed on a set of postfix operators, current operator still may be infix operator
        if (possiblePostfixOperators.Length != 0)
            return TryParseOperatorsRecoursive(operators, position, possiblePrefixOperators, possibleInfixOperators, [], out result);
        result = [];
        return false;
    }

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
}
