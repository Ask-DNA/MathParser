using MathParser;
using System.Globalization;
using System.Text;

namespace MathParser.Demo;

static class Demo
{
    public static void Run()
    {
        Parser parser = new();
        while (true)
        {
            try
            {
                Expression expression = GetExpression(parser);

                Console.WriteLine();
                PrintExpression(expression);
                
                double[] arguments = [];
                if (expression.Arity > 0)
                {
                    Console.WriteLine();
                    arguments = GetArguments();
                }
                
                double result = expression.Calculate(arguments);
                string resultStr = string.Format(NumberFormatInfo.InvariantInfo, "{0:N}", result);
                
                Console.WriteLine();
                PrintResult(result, arguments);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(Environment.NewLine + ex.Message);
            }
            catch (AggregateException ex)
            {
                Console.WriteLine(Environment.NewLine + ex.Message);
            }
            Console.WriteLine();
            Console.WriteLine("Press any key to continue . . .");
            Console.ReadKey();
            Console.Clear();
        }
        
    }

    static Expression GetExpression(Parser parser)
    {
        Console.WriteLine("Enter expression");
        Console.Write("> ");
        string? expressionStr = Console.ReadLine() ?? throw new ArgumentException("Incorrect input.");
        Expression expression = parser.Parse(expressionStr);

        return expression;
    }

    private static void PrintExpression(Expression expression)
    {
        StringBuilder stringBuilder = new($"{expression.OutputType} f(");
        for (int i = 0; i < expression.Arity; i++)
        {
            stringBuilder.Append($"{expression.ArgumentTypes[i]} {expression.Arguments[i]}");
            if (i < expression.Arity - 1)
                stringBuilder.Append(", ");
        }
        stringBuilder.Append($") => {expression.ExpressionString}");
        Console.WriteLine(stringBuilder.ToString());
    }

    static double[] GetArguments()
    {
        string? tmp;

        Console.WriteLine("Enter arguments in one line (for boolean values use 1 and 0)");
        Console.Write("> ");
        tmp = Console.ReadLine() ?? throw new ArgumentException("Incorrect input.");

        string[] argsStr = tmp.Split(' ');
        double[] arguments = new double[argsStr.Length];

        for (int i = 0; i < argsStr.Length; i++)
        {
            if (!double.TryParse(argsStr[i], NumberFormatInfo.InvariantInfo, out arguments[i]))
                throw new ArgumentException("Incorrect input.");
        }

        return arguments;
    }

    private static void PrintResult(double result, double[] arguments)
    {
        string tmp;
        StringBuilder stringBuilder = new("f(");
        for (int i = 0; i < arguments.Length; i++)
        {
            tmp = string.Format(NumberFormatInfo.InvariantInfo, "{0:N}", arguments[i]);
            stringBuilder.Append(tmp);
            if (i < arguments.Length - 1)
                stringBuilder.Append(", ");
        }
        tmp = string.Format(NumberFormatInfo.InvariantInfo, "{0:N}", result);
        stringBuilder.Append($") = {tmp}");
        Console.WriteLine(stringBuilder.ToString());
    }
}
