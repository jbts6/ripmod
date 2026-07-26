using System;

internal static class TributeTests
{
    public static void RunAll()
    {
        TestAssert.Near(15f, TributeMultiplierMath.Apply(10f, 1.5), "runtime scale");
        RuntimeScaleRejectsOverflow();
        TestAssert.Equal("攻击+15%", TributeValueFormatter.Apply("攻击+10%", 1.5), "integer text");
        TestAssert.Equal("攻击+4%", TributeValueFormatter.Apply("攻击+3%", 1.5), "integer text uses Math.Round");
        TestAssert.Equal("速度-3.75", TributeValueFormatter.Apply("速度-2.5", 1.5), "decimal text");
        TestAssert.Equal(
            "攻击+15%，速度-3.75",
            TributeValueFormatter.Apply("攻击+10%，速度-2.5", 1.5),
            "all numbers in detail text");
        TestAssert.Equal("", TributeValueFormatter.Apply("", 1.5), "empty text");
        TestAssert.Equal("无数值", TributeValueFormatter.Apply("无数值", 1.5), "text without numbers");
    }

    private static void RuntimeScaleRejectsOverflow()
    {
        try
        {
            TributeMultiplierMath.Apply(float.MaxValue, 2.0);
        }
        catch (ArgumentOutOfRangeException)
        {
            return;
        }

        throw new InvalidOperationException("runtime scale overflow throws");
    }
}
