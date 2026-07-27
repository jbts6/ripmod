using System.Collections.Generic;

internal static class PerforationMaterialLogicTests
{
    private static readonly string[] Equipped = { "uuid-a", "uuid-b", "uuid-c", "uuid-d" };

    public static void RunAll()
    {
        AllowsAnyUnequippedMaterial();
        RejectsEquippedMaterials();
        RejectsWithoutSlotTarget();
        RejectsInvalidUuid();
        AllowsWhenNoEquippedListAvailable();
    }

    private static void AllowsAnyUnequippedMaterial()
    {
        TestAssert.False(
            PerforationMaterialLogic.ShouldReject(true, "uuid-other", Equipped),
            "unequipped material should be usable");
        TestAssert.False(
            PerforationMaterialLogic.ShouldReject(true, "uuid-dup", new List<string>()),
            "material should be usable with empty equipped list");
    }

    private static void RejectsEquippedMaterials()
    {
        foreach (string uuid in Equipped)
        {
            TestAssert.True(
                PerforationMaterialLogic.ShouldReject(true, uuid, Equipped),
                uuid + " is equipped and should be rejected");
        }
    }

    private static void RejectsWithoutSlotTarget()
    {
        TestAssert.True(
            PerforationMaterialLogic.ShouldReject(false, "uuid-other", Equipped),
            "no slot target should reject");
    }

    private static void RejectsInvalidUuid()
    {
        TestAssert.True(
            PerforationMaterialLogic.ShouldReject(true, null, Equipped),
            "null uuid should reject");
        TestAssert.True(
            PerforationMaterialLogic.ShouldReject(true, "", Equipped),
            "empty uuid should reject");
    }

    private static void AllowsWhenNoEquippedListAvailable()
    {
        TestAssert.False(
            PerforationMaterialLogic.ShouldReject(true, "uuid-other", null),
            "null equipped list should not reject");
    }
}
