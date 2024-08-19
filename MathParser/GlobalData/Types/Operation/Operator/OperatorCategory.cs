namespace MathParser;

// Uses as priority value
internal enum OperatorCategory
{
    Coalesce,
    Implication,
    Equivalence,
    Disjunction,
    Conjunction,
    Equality,
    Relational,
    Additive,
    Multiplicative,
    Exponentiation,
    UnaryPrefix,
    UnaryPostfix
}
