using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

internal static class OracleFusionFeature
{
    public static bool TryEnable(
        string gameAssemblyPath,
        Action<string> info,
        Action<string> error)
    {
        try
        {
            byte[] image = File.ReadAllBytes(gameAssemblyPath);
            VerifyHash(image);

            PeImageFile pe = PeImageFile.Parse(image);
            IReadOnlyList<PreparedBinaryPatch> patches = OracleFusionPatchCatalog
                .Create()
                .Select(spec => spec.Prepare(image, pe))
                .ToArray();

            var writer = WindowsNativePatchWriter.ForLoadedModule("GameAssembly.dll");
            int replacementCount = NativePatchTransaction.ApplyAll(patches, writer);
            info?.Invoke(
                "[OracleFusion] enabled requiredCount=2 signatures=" +
                patches.Count + " replacements=" + replacementCount);
            return true;
        }
        catch (Exception exception)
        {
            error?.Invoke("[OracleFusion] disabled: " + exception);
            return false;
        }
    }

    private static void VerifyHash(byte[] image)
    {
        string actualHash = Convert.ToHexString(SHA256.HashData(image)).ToLowerInvariant();
        if (!string.Equals(
                actualHash,
                OracleFusionPatchCatalog.ExpectedGameAssemblySha256,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Unsupported GameAssembly.dll SHA-256: " + actualHash);
        }
    }
}
