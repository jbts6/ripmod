using System;

internal static class Program
{
    private static int Main()
    {
        try
        {
            PerforationRateLogicTests.RunAll();
            PerforationMaterialLogicTests.RunAll();

            Console.WriteLine("ALL TESTS PASSED");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }
}
