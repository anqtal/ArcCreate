using System;

public readonly struct Option<T>
{
    public readonly bool HasValue;

    public readonly T Value;

    private Option(T value, bool hasValue)
    {
        Value = value;
        HasValue = hasValue;
    }

    public static implicit operator Option<T>(T value)
    {
        return new Option<T>(value, true);
    }

    public static Option<T> Some(T value)
    {
        return new Option<T>(value, true);
    }

    public static Option<T> None()
    {
        return new Option<T>(default, false);
    }

    public T Or(T alternate)
    {
        return HasValue ? Value : alternate;
    }

    public Option<R> Map<R>(Func<T, R> mapper)
    {
        return HasValue ? Option<R>.Some(mapper.Invoke(Value)) : Option<R>.None();
    }
}