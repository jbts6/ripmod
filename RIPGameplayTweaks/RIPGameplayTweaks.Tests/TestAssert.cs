using System;

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
