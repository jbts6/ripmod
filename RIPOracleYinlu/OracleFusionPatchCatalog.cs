using System.Collections.Generic;

internal static class OracleFusionPatchCatalog
{
    public const string ExpectedGameAssemblySha256 =
        "c0f8d4d8e48aa374a5825a2935f2103ba84dda4b2bb6f6d9613866acd077d3a3";

    public static IReadOnlyList<BinaryPatchSpec> Create()
    {
        var patches = new List<BinaryPatchSpec>();
        patches.AddRange(CreateBatchPatches());
        patches.AddRange(CreateCompletionPatches());
        patches.AddRange(CreateStartConfirmPatches());
        patches.AddRange(CreateUniversalStonePatches());
        return patches;
    }

    private static IReadOnlyList<BinaryPatchSpec> CreateBatchPatches()
    {
        return new[]
        {
            new BinaryPatchSpec(
                "batch minimum count",
                "48 8B 47 18 48 85 C0 0F 84 ?? ?? ?? ?? 83 78 18 03 7C ?? 48 83 7F 20 00",
                new ByteReplacement(16, "03", "02")),
            new BinaryPatchSpec(
                "batch plan divisor",
                "B8 56 55 55 55 41 F7 6F 18 44 8B E2 41 C1 EC 1F 44 03 E2",
                new ByteReplacement(
                    0,
                    "B8 56 55 55 55 41 F7 6F 18 44 8B E2 41 C1 EC 1F 44 03 E2",
                    "45 8B 67 18 41 D1 EC 90 90 90 90 90 90 90 90 90 90 90 90")),
            new BinaryPatchSpec(
                "batch plan grouping",
                "8D 14 7F 4C 8B 05 ?? ?? ?? ?? 49 8B CF E8 ?? ?? ?? ?? 4C 8B 05 ?? ?? ?? ?? BA 03 00 00 00 48 8B C8",
                new ByteReplacement(2, "7F", "3F"),
                new ByteReplacement(26, "03", "02")),
            new BinaryPatchSpec(
                "batch material validation",
                "83 7F 18 03 0F 8C ?? ?? ?? ?? 4C 8B 05 ?? ?? ?? ?? 48 8D 54 24 38",
                new ByteReplacement(3, "03", "02"))
        };
    }

    private static IReadOnlyList<BinaryPatchSpec> CreateCompletionPatches()
    {
        return new[]
        {
            new BinaryPatchSpec(
                "manual material requirement",
                "4C 89 AC 24 80 00 00 00 B8 03 00 00 00 2B 47 78",
                new ByteReplacement(9, "03", "02")),
            new BinaryPatchSpec(
                "completion target and missing count",
                "83 F8 03 0F 8D ?? ?? ?? ?? 41 BF 03 00 00 00 44 2B F8",
                new ByteReplacement(2, "03", "02"),
                new ByteReplacement(11, "03", "02")),
            new BinaryPatchSpec(
                "completion current bone count",
                "41 B8 03 00 00 00 48 8B 15 ?? ?? ?? ?? FF D0 33 D2",
                new ByteReplacement(2, "03", "02")),
            new BinaryPatchSpec(
                "completion fallback count",
                "48 89 B4 24 A8 00 00 00 C7 84 24 A0 00 00 00 03 00 00 00",
                new ByteReplacement(15, "03", "02")),
            new BinaryPatchSpec(
                "manual slot capacity",
                "C7 84 24 B8 00 00 00 03 00 00 00 83 F8 03 0F 8F",
                new ByteReplacement(7, "03", "02"),
                new ByteReplacement(13, "03", "02")),
            new BinaryPatchSpec(
                "manual button and preview threshold",
                "41 83 FE 03 0F 9C C2 45 33 C0 E8 ?? ?? ?? ?? 41 83 FE 03 7C ?? 4D 85 FF",
                new ByteReplacement(3, "03", "02"),
                new ByteReplacement(18, "03", "02"))
        };
    }

    // QuitOracleFuseStart_Joystick/Keyboard_View gate on placed+pending count before tips.
    private static IReadOnlyList<BinaryPatchSpec> CreateStartConfirmPatches()
    {
        return new[]
        {
            new BinaryPatchSpec(
                "start confirm joystick threshold",
                "8B 48 78 03 CF 83 F9 03 0F 8C ?? ?? ?? ??",
                new ByteReplacement(7, "03", "02")),
            new BinaryPatchSpec(
                "start confirm keyboard threshold",
                "8B 48 78 03 CE 83 F9 03 0F 8C ?? ?? ?? ??",
                new ByteReplacement(7, "03", "02"))
        };
    }

    private static IReadOnlyList<BinaryPatchSpec> CreateUniversalStonePatches()
    {
        return new[]
        {
            new BinaryPatchSpec(
                "universal stone auto fill",
                "BB 03 00 00 00 2B 5C 24 40 85 DB 7E ??",
                new ByteReplacement(1, "03", "02")),
            new BinaryPatchSpec(
                "universal stone refresh threshold",
                "83 FF 03 0F 9C C2 45 33 C0 E8 ?? ?? ?? ??",
                new ByteReplacement(2, "03", "02")),
            new BinaryPatchSpec(
                "universal stone increase limit",
                "41 83 FE 03 7D ?? 8B 44 24 68 E9 ?? ?? ?? ??",
                new ByteReplacement(3, "03", "02")),
            new BinaryPatchSpec(
                "universal stone decrease limit",
                "41 83 FE 03 7D ?? 8B 44 24 68 EB ??",
                new ByteReplacement(3, "03", "02"))
        };
    }
}
