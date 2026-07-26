using System;

internal static class Program
{
    private static int Main()
    {
        try
        {
            OracleFusionPatchTests.RunAll();
            NativePatchTransactionTests.RunAll();
            YinluAdvanceTests.RunAll();
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
