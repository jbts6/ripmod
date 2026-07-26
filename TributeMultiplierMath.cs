using System;

public static class TributeMultiplierMath
{
    public static float Apply(float value, double multiplier)
    {
        var result = (double)value * multiplier;
        if (double.IsNaN(result) || double.IsInfinity(result) ||
            result < -float.MaxValue || result > float.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(multiplier), "The scaled tribute value is outside the float range.");
        }

        return (float)result;
    }
}
