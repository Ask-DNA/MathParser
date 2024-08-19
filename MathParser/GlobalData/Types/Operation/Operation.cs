using LinqExpressions = System.Linq.Expressions;

namespace MathParser;

public abstract class Operation
{
    private string[] _arguments = [];

    private ValueDomain[] _argumentTypes = [];

    internal Func<double[], double> Source
    {
        get;
        private set;
    } = (arr) => double.NaN;

    public string[] Arguments
    {
        get
        {
            string[] result = new string[_arguments.Length];
            _arguments.CopyTo(result, 0);
            return result;
        }
        private set
        {
            _arguments = new string[value.Length];
            value.CopyTo(_arguments, 0);
        }
    }

    public ValueDomain[] ArgumentTypes
    {
        get
        {
            ValueDomain[] result = new ValueDomain[_argumentTypes.Length];
            _argumentTypes.CopyTo(result, 0);
            return result;
        }
        private set
        {
            _argumentTypes = new ValueDomain[value.Length];
            value.CopyTo(_argumentTypes, 0);
        }
    }

    public ValueDomain OutputType
    {
        get;
        private set;
    } = ValueDomain.Double;

    public int Arity
    {
        get => _arguments.Length;
    }

    protected internal void SetSourceIfValid(Func<double[], double> source, string[] arguments, ValueDomain[] argumentTypes, ValueDomain outputType)
    {
        if (arguments.Length != argumentTypes.Length)
            return;
        if (arguments.Length != 0 && arguments.Distinct().ToArray().Length != argumentTypes.Length)
            return;

        foreach (string argument in arguments)
            if (!ParserGlobal.ValidateName(argument))
                return;

        Source = source;
        Arguments = arguments;
        ArgumentTypes = argumentTypes;
        OutputType = outputType;
    }

    protected internal void SetSource(Operation operation)
    {
        Source = operation.Source;
        Arguments = operation.Arguments;
        ArgumentTypes = operation.ArgumentTypes;
        OutputType = operation.OutputType;
    }

    internal LinqExpressions.InvocationExpression AsLinqExpression(params LinqExpressions.Expression[] arguments)
    {
        LinqExpressions.ConstantExpression sourceExpression = LinqExpressions.Expression.Constant(Source);
        LinqExpressions.NewArrayExpression argumentArrayCreationExpression;
        argumentArrayCreationExpression = LinqExpressions.Expression.NewArrayInit(typeof(double), arguments);
        return LinqExpressions.Expression.Invoke(sourceExpression, argumentArrayCreationExpression);
    }
}