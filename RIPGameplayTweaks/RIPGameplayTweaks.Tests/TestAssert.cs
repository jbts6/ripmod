using System;
using System.Collections.Generic;

internal static class TestAssert
{
    private const double Tolerance = 0.000001;

    public static void Near(double expected, double actual, string description)
    {
        if (Math.Abs(expected - actual) > Tolerance)
            throw new InvalidOperationException(description + ": expected " + expected + ", got " + actual);
    }

    public static void Equal(string expected, string actual, string description)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                description + ": expected '" + expected + "', got '" + actual + "'");
        }
    }

    public static void Equal(int expected, int actual, string description)
    {
        if (expected != actual)
            throw new InvalidOperationException(description + ": expected " + expected + ", got " + actual);
    }

    public static void SequenceEqual(
        IReadOnlyList<byte> expected,
        IReadOnlyList<byte> actual,
        string description)
    {
        if (expected.Count != actual.Count)
        {
            throw new InvalidOperationException(
                description + ": expected length " + expected.Count + ", got " + actual.Count);
        }

        for (int index = 0; index < expected.Count; index++)
        {
            if (expected[index] != actual[index])
            {
                throw new InvalidOperationException(
                    description + ": mismatch at " + index +
                    ", expected " + expected[index].ToString("X2") +
                    ", got " + actual[index].ToString("X2"));
            }
        }
    }

    public static void Throws<TException>(Action action, string description)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException(description + ": expected " + typeof(TException).Name);
    }

    public static void True(bool value, string description)
    {
        if (!value)
            throw new InvalidOperationException(description + ": expected true");
    }

    public static void False(bool value, string description)
    {
        if (value)
            throw new InvalidOperationException(description + ": expected false");
    }
}
