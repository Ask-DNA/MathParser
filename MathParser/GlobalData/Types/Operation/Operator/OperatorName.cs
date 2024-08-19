namespace MathParser;

internal enum OperatorName
{
    // Arithmetical prefix operators:
    UnaryPlus,
    UnaryMinus,

    // Arithmetical infix operators:
    Addition,
    Subtraction,
    Multiplication,
    Division,
    Modulation,
    Exponentiation,

    // Arithmetical postfix operators:
    Factorial,

    // Logical prefix operators:
    LogicNegation,

    // Logical infix operators:
    Conjunction,
    Disjunction,
    Equivalence,
    Implication,
    ConverseImplication,

    // Multitype relational:
    Equality,
    Inequality,

    // Arithmetical relational:
    LessThan,
    LessThanOrEqual,
    GreaterThan,
    GreaterThanOrEqual,

    Coalesce
}
