namespace MathParser;

public class MathCollection
{
    private readonly Operator[] _operators;
    private readonly List<Function> _functions;
    private readonly List<Constant> _constants;

    internal Operator[] Operators
    {
        get
        {
            Operator[] result = new Operator[_operators.Length];
            _operators.CopyTo(result, 0);
            return result;
        }
    }

    internal Operator[] PrefixOperators
    {
        get => _operators.Where(o => o.Category == OperatorCategory.UnaryPrefix).ToArray();
    }

    internal Operator[] PostfixOperators
    {
        get => _operators.Where(o => o.Category == OperatorCategory.UnaryPostfix).ToArray();
    }

    internal Operator[] InfixOperators
    {
        get => _operators.Where(o => o.Category != OperatorCategory.UnaryPrefix && o.Category != OperatorCategory.UnaryPostfix).ToArray();
    }

    internal string[] PrefixOperatorSymbols
    {
        get
        {
            Operator[] operators = _operators.Where(o => o.Category == OperatorCategory.UnaryPrefix).ToArray();
            return operators.Select(o => o.Symbol).ToArray();
        }
    }

    internal string[] InfixOperatorSymbols
    {
        get
        {
            Operator[] operators = _operators.Where(o => o.Category != OperatorCategory.UnaryPrefix && o.Category != OperatorCategory.UnaryPostfix).ToArray();
            return operators.Select(o => o.Symbol).ToArray();
        }
    }

    internal string[] PostfixOperatorSymbols
    {
        get
        {
            Operator[] operators = _operators.Where(o => o.Category == OperatorCategory.UnaryPostfix).ToArray();
            return operators.Select(o => o.Symbol).ToArray();
        }
    }

    public Function[] Functions
    {
        get
        {
            Function[] result = new Function[_functions.Count];
            _functions.CopyTo(result, 0);
            return result;
        }
    }

    public Constant[] Constants
    {
        get
        {
            Constant[] result = new Constant[_constants.Count];
            _constants.CopyTo(result, 0);
            return result;
        }
    }

    public MathCollection(Function[] functions, Constant[] constants)
    {
        List<Exception> exceptions = [];

        Function[] functionDuplicates = FindDuplicates(functions, (f1, f2) => f1.Name == f2.Name && f1.Arity == f2.Arity);
        if (functionDuplicates.Length != 0)
            foreach (Function f in functionDuplicates)
                exceptions.Add(ExceptionBuilder.DuplicateInInputFunctionArray(f.Name, f.Arity));

        Constant[] constantDuplicates = FindDuplicates(constants, (c1, c2) => c1.Name == c2.Name);
        if (constantDuplicates.Length != 0)
            foreach (Constant c in constantDuplicates)
                exceptions.Add(ExceptionBuilder.DuplicateInInputConstantArray(c.Name));

        if (exceptions.Count != 0)
            throw ExceptionBuilder.SeveralErrorsDuringMathCollectionProcessingException([.. exceptions]);

        _operators = ParserGlobal.BuiltInOperators;
        _functions = new(functions);
        _constants = new(constants);
    }

    public static MathCollection Default()
    {
        return new(ParserGlobal.BuiltInFunctions, ParserGlobal.BuiltInConstants);
    }

    private static T[] FindDuplicates<T>(T[] array, Func<T, T, bool> comparsion)
    {
        List<T> result = [];
        T[] tmp;
        bool[] checkedIndices = new bool[array.Length];
        Array.Fill(checkedIndices, false);
        for (int i = 0; i < array.Length; i++)
        {
            if (checkedIndices[i])
                continue;
            tmp = Array.FindAll(array, f => comparsion(f, array[i]));
            if (tmp.Length > 1)
            {
                result.Add(tmp[0]);
                for (int j = 1; j < tmp.Length; j++)
                    checkedIndices[Array.IndexOf(array, tmp[j])] = true;
            }
            checkedIndices[i] = true;
        }
        return [.. result];
    }

    public void Add(Function function)
    {
        if (_functions.Exists(f => f.Name == function.Name && f.Arity == function.Arity))
            throw ExceptionBuilder.ConflictOnFunctionInsertionException(function.Name, function.Arity);
        _functions.Add(function);
    }

    public void Add(Constant constant)
    {
        if (_constants.Exists(c => c.Name == constant.Name))
            throw ExceptionBuilder.ConflictOnConstantInsertionException(constant.Name);
        _constants.Add(constant);
    }

    public bool TryAdd(Function function)
    {
        if (_functions.Exists(f => f.Name == function.Name && f.Arity == function.Arity))
            return false;
        _functions.Add(function);
        return true;
    }

    public bool TryAdd(Constant constant)
    {
        if (_constants.Exists(c => c.Name == constant.Name))
            return false;
        _constants.Add(constant);
        return true;
    }

    public void AddRange(Function[] functions)
    {
        List<Exception> exceptions = [];

        Function[] duplicates = FindDuplicates(functions, (f1, f2) => f1.Name == f2.Name && f1.Arity == f2.Arity);
        if (duplicates.Length != 0)
            foreach (Function f in duplicates)
                exceptions.Add(ExceptionBuilder.DuplicateInInputFunctionArray(f.Name, f.Arity));

        if (exceptions.Count != 0)
            throw ExceptionBuilder.SeveralErrorsDuringMathCollectionProcessingException([.. exceptions]);

        for (int i = 0; i < functions.Length; i++)
        {
            if (_functions.Exists(f => f.Name == functions[i].Name && f.Arity == functions[i].Arity))
                exceptions.Add(ExceptionBuilder.ConflictOnFunctionInsertionException(functions[i].Name, functions[i].Arity));
        }

        if (exceptions.Count == 0)
            _functions.AddRange(functions);
        else
            throw ExceptionBuilder.SeveralErrorsDuringMathCollectionProcessingException([.. exceptions]);
    }

    public void AddRange(Constant[] constants)
    {
        List<Exception> exceptions = [];

        Constant[] duplicates = FindDuplicates(constants, (c1, c2) => c1.Name == c2.Name);
        if (duplicates.Length != 0)
            foreach (Constant c in duplicates)
                exceptions.Add(ExceptionBuilder.DuplicateInInputConstantArray(c.Name));

        if (exceptions.Count != 0)
            throw ExceptionBuilder.SeveralErrorsDuringMathCollectionProcessingException([.. exceptions]);

        for (int i = 0; i < constants.Length; i++)
        {
            if (_constants.Exists(c => c.Name == constants[i].Name))
                exceptions.Add(ExceptionBuilder.ConflictOnConstantInsertionException(constants[i].Name));
        }

        if (exceptions.Count == 0)
            _constants.AddRange(constants);
        else
            throw ExceptionBuilder.SeveralErrorsDuringMathCollectionProcessingException([.. exceptions]);
    }

    public bool TryAddRange(Function[] functions)
    {
        if (FindDuplicates(functions, (f1, f2) => f1.Name == f2.Name && f1.Arity == f2.Arity).Length != 0)
            return false;

        for (int i = 0; i < functions.Length; i++)
        {
            if (_functions.Exists(f => f.Name == functions[i].Name && f.Arity == functions[i].Arity))
                return false;
        }
        _functions.AddRange(functions);
        return true;
    }

    public bool TryAddRange(Constant[] constants)
    {
        if (FindDuplicates(constants, (c1, c2) => c1.Name == c2.Name).Length != 0)
            return false;

        for (int i = 0; i < constants.Length; i++)
        {
            if (_constants.Exists(c => c.Name == constants[i].Name))
                return false;
        }
        _constants.AddRange(constants);
        return true;
    }

    public void Remove(Function function)
    {
        if (!_functions.Exists(f => f == function))
            throw ExceptionBuilder.FunctionNotFoundException(function.Name, function.Arity);
        _functions.Remove(function);
    }

    public void Remove(Constant constant)
    {
        if (!_constants.Exists(f => f == constant))
            throw ExceptionBuilder.ConstantNotFoundException(constant.Name);
        _constants.Remove(constant);
    }

    public bool TryRemove(Function function)
    {
        if (!_functions.Exists(f => f == function))
            return false;
        _functions.Remove(function);
        return true;
    }

    public bool TryRemove(Constant constant)
    {
        if (!_constants.Exists(f => f == constant))
            return false;
        _constants.Remove(constant);
        return true;
    }

    public void RemoveRange(Function[] functions)
    {
        List<Exception> exceptions = [];

        Function[] duplicates = FindDuplicates(functions, (f1, f2) => f1.Name == f2.Name && f1.Arity == f2.Arity);
        if (duplicates.Length != 0)
            foreach (Function f in duplicates)
                exceptions.Add(ExceptionBuilder.DuplicateInInputFunctionArray(f.Name, f.Arity));

        if (exceptions.Count != 0)
            throw ExceptionBuilder.SeveralErrorsDuringMathCollectionProcessingException([.. exceptions]);

        for (int i = 0; i < functions.Length; i++)
        {
            if (!_functions.Exists(f => f.Name == functions[i].Name && f.Arity == functions[i].Arity))
                exceptions.Add(ExceptionBuilder.FunctionNotFoundException(functions[i].Name, functions[i].Arity));
        }

        if (exceptions.Count == 0)
            foreach (Function f in functions)
                _functions.Remove(f);
        else
            throw ExceptionBuilder.SeveralErrorsDuringMathCollectionProcessingException([.. exceptions]);
    }

    public void RemoveRange(Constant[] constants)
    {
        List<Exception> exceptions = [];

        Constant[] duplicates = FindDuplicates(constants, (c1, c2) => c1.Name == c2.Name);
        if (duplicates.Length != 0)
            foreach (Constant c in duplicates)
                exceptions.Add(ExceptionBuilder.DuplicateInInputConstantArray(c.Name));

        if (exceptions.Count != 0)
            throw ExceptionBuilder.SeveralErrorsDuringMathCollectionProcessingException([.. exceptions]);

        for (int i = 0; i < constants.Length; i++)
        {
            if (_constants.Exists(c => c.Name == constants[i].Name))
                exceptions.Add(ExceptionBuilder.ConflictOnConstantInsertionException(constants[i].Name));
        }

        if (exceptions.Count == 0)
            foreach (Constant c in constants)
                _constants.Remove(c);
        else
            throw ExceptionBuilder.SeveralErrorsDuringMathCollectionProcessingException([.. exceptions]);
    }

    public bool TryRemoveRange(Function[] functions)
    {
        if (FindDuplicates(functions, (f1, f2) => f1.Name == f2.Name && f1.Arity == f2.Arity).Length != 0)
            return false;

        for (int i = 0; i < functions.Length; i++)
        {
            if (!_functions.Exists(f => f.Name == functions[i].Name && f.Arity == functions[i].Arity))
                return false;
        }
        foreach (Function f in functions)
            _functions.Remove(f);
        return true;
    }

    public bool TryRemoveRange(Constant[] constants)
    {
        if (FindDuplicates(constants, (c1, c2) => c1.Name == c2.Name).Length != 0)
            return false;

        for (int i = 0; i < constants.Length; i++)
        {
            if (!_constants.Exists(c => c.Name == constants[i].Name))
                return false;
        }
        foreach (Constant c in constants)
            _constants.Remove(c);
        return true;
    }

    public MathCollection Clone()
    {
        return new(Functions, Constants);
    }
}