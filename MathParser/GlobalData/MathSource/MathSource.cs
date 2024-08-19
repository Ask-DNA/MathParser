namespace MathParser;

internal static class MathSource
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
        for (int i =0; i < arguments.Length; i++)
            if (Double.IsFinite(arguments[i]))
                return arguments[i];
        return Double.NaN;
    }

    #region MathFunctions

    public static double Abs(double[] arguments)
    {
        return Double.IsFinite(arguments[0]) ? Math.Abs(arguments[0]) : Double.NaN;
    }

    public static double Sqrt(double[] arguments)
    {
        return Double.IsFinite(arguments[0]) && arguments[0] >= 0 ? Math.Sqrt(arguments[0]) : Double.NaN;
    }

    public static double Cbrt(double[] arguments)
    {
        return Double.IsFinite(arguments[0]) && arguments[0] >= 0 ? Math.Cbrt(arguments[0]) : Double.NaN;
    }

    public static double Exp(double[] arguments)
    {
        return Double.IsFinite(arguments[0]) ? Math.Exp(arguments[0]) : Double.NaN;
    }

    public static double Pow(double[] arguments)
    {
        double result = Math.Pow(arguments[0], arguments[1]);
        return Double.IsFinite(result) ? result : Double.NaN;
    }

    public static double Log(double[] arguments)
    {
        double result = Math.Log(arguments[0], arguments[1]);
        return Double.IsFinite(result) ? result : Double.NaN;
    }

    public static double Log2(double[] arguments)
    {
        double result = Math.Log2(arguments[0]);
        return Double.IsFinite(result) ? result : Double.NaN;
    }

    public static double Log10(double[] arguments)
    {
        double result = Math.Log10(arguments[0]);
        return Double.IsFinite(result) ? result : Double.NaN;
    }

    public static double Ln(double[] arguments)
    {
        double result = Math.Log(arguments[0]);
        return Double.IsFinite(result) ? result : Double.NaN;
    }

    public static double Sin(double[] arguments)
    {
        double result = Math.Sin(arguments[0]);
        return Double.IsFinite(result) ? result : Double.NaN;
    }

    public static double Cos(double[] arguments)
    {
        double result = Math.Cos(arguments[0]);
        return Double.IsFinite(result) ? result : Double.NaN;
    }

    public static double Tan(double[] arguments)
    {
        double result = Math.Tan(arguments[0]);
        return Double.IsFinite(result) ? result : Double.NaN;
    }

    public static double Asin(double[] arguments)
    {
        double result = Math.Asin(arguments[0]);
        return Double.IsFinite(result) ? result : Double.NaN;
    }

    public static double Acos(double[] arguments)
    {
        double result = Math.Acos(arguments[0]);
        return Double.IsFinite(result) ? result : Double.NaN;
    }

    public static double Atan(double[] arguments)
    {
        double result = Math.Atan(arguments[0]);
        return Double.IsFinite(result) ? result : Double.NaN;
    }

    public static double Sinh(double[] arguments)
    {
        double result = Math.Sinh(arguments[0]);
        return Double.IsFinite(result) ? result : Double.NaN;
    }

    public static double Cosh(double[] arguments)
    {
        double result = Math.Cosh(arguments[0]);
        return Double.IsFinite(result) ? result : Double.NaN;
    }

    public static double Tanh(double[] arguments)
    {
        double result = Math.Tanh(arguments[0]);
        return Double.IsFinite(result) ? result : Double.NaN;
    }

    public static double Asinh(double[] arguments)
    {
        double result = Math.Asinh(arguments[0]);
        return Double.IsFinite(result) ? result : Double.NaN;
    }

    public static double Acosh(double[] arguments)
    {
        double result = Math.Acosh(arguments[0]);
        return Double.IsFinite(result) ? result : Double.NaN;
    }

    public static double Atanh(double[] arguments)
    {
        double result = Math.Atanh(arguments[0]);
        return Double.IsFinite(result) ? result : Double.NaN;
    }

    public static double Not(double[] arguments) => LogicNegation(arguments);

    public static double And(double[] arguments) => Conjunction(arguments);

    public static double Or(double[] arguments) => Disjunction(arguments);

    public static double Eqv(double[] arguments) => Equivalence(arguments);

    public static double Imp(double[] arguments) => Implication(arguments);

    public static double Xor(double[] arguments)
    {
        if ((arguments[0] != 0 && arguments[0] != 1) || (arguments[1] != 0 && arguments[1] != 1))
            return Double.NaN;
        return (arguments[0] != arguments[1]) ? 1 : 0;
    }

    public static double Nand(double[] arguments)
    {
        if ((arguments[0] != 0 && arguments[0] != 1) || (arguments[1] != 0 && arguments[1] != 1))
            return Double.NaN;
        return (arguments[0] == 1 && arguments[1] == 1) ? 0 : 1;
    }

    public static double Nor(double[] arguments)
    {
        if ((arguments[0] != 0 && arguments[0] != 1) || (arguments[1] != 0 && arguments[1] != 1))
            return Double.NaN;
        return (arguments[0] == 1 || arguments[1] == 1) ? 0 : 1;
    }

    #endregion
}
