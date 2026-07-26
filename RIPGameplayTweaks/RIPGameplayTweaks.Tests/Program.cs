using System;

internal static class Program
{
    private static int Main()
    {
        try
        {
            GameplayConfigTests.RunAll();
            AbsorbTests.RunAll();
            IntegrationStaticTests.RunAll();

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
