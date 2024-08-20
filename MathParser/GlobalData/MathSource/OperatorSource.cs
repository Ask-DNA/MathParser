namespace MathParser;

internal class OperatorSource
{
    #region ArithmeticalOperators

    public static double UnaryPlus(double[] arguments)
    {
        return Double.IsFinite(arguments[0]) ? arguments[0] : Double.NaN;
    }

    public static double UnaryMinus(double[] arguments)
    {
        return Double.IsFinite(arguments[0]) ? -arguments[0] : Double.NaN;
    }

    public static double Addition(double[] arguments)
    {
        return (Double.IsFinite(arguments[0]) && Double.IsFinite(arguments[1])) ? arguments[0] + arguments[1] : Double.NaN;
    }

    public static double Subtraction(double[] arguments)
    {
        return (Double.IsFinite(arguments[0]) && Double.IsFinite(arguments[1])) ? arguments[0] - arguments[1] : Double.NaN;
    }

    public static double Multiplication(double[] arguments)
    {
        return (Double.IsFinite(arguments[0]) && Double.IsFinite(arguments[1])) ? arguments[0] * arguments[1] : Double.NaN;
    }

    public static double Division(double[] arguments)
    {
        return (Double.IsFinite(arguments[0]) && Double.IsFinite(arguments[1]) && arguments[1] != 0) ? arguments[0] / arguments[1] : Double.NaN;
    }

    public static double Modulation(double[] arguments)
    {
        return (Double.IsFinite(arguments[0]) && Double.IsFinite(arguments[1]) && arguments[1] != 0) ? arguments[0] % arguments[1] : Double.NaN;
    }

    public static double Exponentiation(double[] arguments)
    {
        return (Double.IsFinite(arguments[0]) && Double.IsFinite(arguments[1])) ? Math.Pow(arguments[0], arguments[1]) : Double.NaN;
    }

    public static double Factorial(double[] arguments)
    {
        if (!Double.IsFinite(arguments[0]) || (arguments[0] % 1) != 0)
            return Double.NaN;
        double result = 1;
        for (int i = 2; i <= arguments[0]; i++)
            result *= i;
        return result;
    }

    #endregion

    #region LogicalOperators

    public static double LogicNegation(double[] arguments)
    {
        if (arguments[0] != 0 && arguments[0] != 1)
            return Double.NaN;
        return (arguments[0] == 1) ? 0 : 1;
    }

    public static double Conjunction(double[] arguments)
    {
        if ((arguments[0] != 0 && arguments[0] != 1) || (arguments[1] != 0 && arguments[1] != 1))
            return Double.NaN;
        return (arguments[0] == 1 && arguments[1] == 1) ? 1 : 0;
    }

    public static double Disjunction(double[] arguments)
    {
        if ((arguments[0] != 0 && arguments[0] != 1) || (arguments[1] != 0 && arguments[1] != 1))
            return Double.NaN;
        return (arguments[0] == 1 || arguments[1] == 1) ? 1 : 0;
    }

    public static double Equivalence(double[] arguments)
    {
        if ((arguments[0] != 0 && arguments[0] != 1) || (arguments[1] != 0 && arguments[1] != 1))
            return Double.NaN;
        return (arguments[0] == arguments[1]) ? 1 : 0;
    }

    public static double Implication(double[] arguments)
    {
        if ((arguments[0] != 0 && arguments[0] != 1) || (arguments[1] != 0 && arguments[1] != 1))
            return Double.NaN;
        return (arguments[0] == 0 || arguments[1] == 1) ? 1 : 0;
    }

    public static double ConverseImplication(double[] arguments)
    {
        if ((arguments[0] != 0 && arguments[0] != 1) || (arguments[1] != 0 && arguments[1] != 1))
            return Double.NaN;
        return (arguments[0] == 1 || arguments[1] == 0) ? 1 : 0;
    }

    #endregion

    #region RelationalOperators

    public static double Equality(double[] arguments)
    {
        if (!Double.IsFinite(arguments[0]) || !Double.IsFinite(arguments[1]))
            return Double.NaN;
        return (arguments[0] == arguments[1]) ? 1 : 0;
    }

    public static double Inequality(double[] arguments)
    {
        if (!Double.IsFinite(arguments[0]) || !Double.IsFinite(arguments[1]))
            return Double.NaN;
        return (arguments[0] != arguments[1]) ? 1 : 0;
    }

    public static double LessThan(double[] arguments)
    {
        if (!Double.IsFinite(arguments[0]) || !Double.IsFinite(arguments[1]))
            return Double.NaN;
        return (arguments[0] < arguments[1]) ? 1 : 0;
    }

    public static double LessThanOrEqual(double[] arguments)
    {
        if (!Double.IsFinite(arguments[0]) || !Double.IsFinite(arguments[1]))
            return Double.NaN;
        return (arguments[0] <= arguments[1]) ? 1 : 0;
    }

    public static double GreaterThan(double[] arguments)
    {
        if (!Double.IsFinite(arguments[0]) || !Double.IsFinite(arguments[1]))
            return Double.NaN;
        return (arguments[0] > arguments[1]) ? 1 : 0;
    }

    public static double GreaterThanOrEqual(double[] arguments)
    {
        if (!Double.IsFinite(arguments[0]) || !Double.IsFinite(arguments[1]))
            return Double.NaN;
        return (arguments[0] >= arguments[1]) ? 1 : 0;
    }

    #endregion

    public static double Coalesce(double[] arguments)
    {
        for (int i = 0; i < arguments.Length; i++)
            if (Double.IsFinite(arguments[i]))
                return arguments[i];
        return Double.NaN;
    }
}
