using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

internal static class OracleFusionPatchTests
{
    private const string ExpectedGameAssemblySha256 =
        "c0f8d4d8e48aa374a5825a2935f2103ba84dda4b2bb6f6d9613866acd077d3a3";

    public static void RunAll()
    {
        PatternSupportsWildcards();
        MissingPatternIsRejected();
        DuplicatePatternIsRejected();
        ReplacementMustTargetFixedExpectedBytes();
        CurrentGameAssemblyMatchesCatalog();
    }

    private static void PatternSupportsWildcards()
    {
        BytePattern pattern = BytePattern.Parse("AA ?? CC");
        IReadOnlyList<int> matches = pattern.FindMatches(new byte[]
        {
            0x00, 0xAA, 0x11, 0xCC, 0xAA, 0x22, 0xCC
        });

        TestAssert.Equal(2, matches.Count, "wildcard match count");
        TestAssert.Equal(1, matches[0], "first wildcard match");
        TestAssert.Equal(4, matches[1], "second wildcard match");
    }

    private static void MissingPatternIsRejected()
    {
        var spec = new BinaryPatchSpec(
            "missing",
            "AA BB",
            new ByteReplacement(0, "AA", "CC"));

        TestAssert.Throws<InvalidOperationException>(
            () => spec.Prepare(new byte[] { 0x00, 0x01 }, PeImageFile.ForRawImage(2)),
            "zero matches rejected");
    }

    private static void DuplicatePatternIsRejected()
    {
        var spec = new BinaryPatchSpec(
            "duplicate",
            "AA BB",
            new ByteReplacement(0, "AA", "CC"));

        TestAssert.Throws<InvalidOperationException>(
            () => spec.Prepare(
                new byte[] { 0xAA, 0xBB, 0x00, 0xAA, 0xBB },
                PeImageFile.ForRawImage(5)),
            "multiple matches rejected");
    }

    private static void ReplacementMustTargetFixedExpectedBytes()
    {
        TestAssert.Throws<ArgumentException>(
            () => new BinaryPatchSpec(
                "wildcard replacement",
                "AA ?? CC",
                new ByteReplacement(1, "00", "01")),
            "replacement over wildcard rejected");

        TestAssert.Throws<ArgumentException>(
            () => new BinaryPatchSpec(
                "wrong expected byte",
                "AA BB CC",
                new ByteReplacement(1, "BC", "01")),
            "replacement expected byte must match pattern");
    }

    private static void CurrentGameAssemblyMatchesCatalog()
    {
        string path = FindGameAssembly();
        byte[] image = File.ReadAllBytes(path);
        string actualHash = Convert.ToHexString(SHA256.HashData(image)).ToLowerInvariant();
        TestAssert.Equal(ExpectedGameAssemblySha256, actualHash, "GameAssembly SHA-256");

        PeImageFile pe = PeImageFile.Parse(image);
        IReadOnlyList<BinaryPatchSpec> catalog = OracleFusionPatchCatalog.Create();
        TestAssert.Equal(16, catalog.Count, "oracle fusion signature count");

        var prepared = catalog.Select(spec => spec.Prepare(image, pe)).ToArray();
        TestAssert.Equal(
            20,
            prepared.Sum(item => item.Replacements.Count),
            "oracle fusion replacement count");

        foreach (PreparedBinaryPatch patch in prepared)
        {
            foreach (PreparedByteReplacement replacement in patch.Replacements)
            {
                byte[] actual = image
                    .Skip(replacement.FileOffset)
                    .Take(replacement.ExpectedBytes.Length)
                    .ToArray();
                TestAssert.SequenceEqual(
                    replacement.ExpectedBytes,
                    actual,
                    patch.Name + " original bytes");
                TestAssert.True(replacement.Rva > 0, patch.Name + " maps to RVA");
            }
        }
    }

    private static string FindGameAssembly()
    {
        DirectoryInfo directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        for (int depth = 0; depth < 8 && directory != null; depth++, directory = directory.Parent)
        {
            string candidate = Path.Combine(directory.FullName, "GameAssembly.dll");
            if (File.Exists(candidate))
                return candidate;
        }

        throw new FileNotFoundException("GameAssembly.dll not found from test working directory.");
    }
}
