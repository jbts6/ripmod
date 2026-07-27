internal static class PerforationRateLogicTests
{
    public static void RunAll()
    {
        AcceptsAllBaseRateKeys();
        AcceptsAllPityRateKeys();
        RejectsUnrelatedKeys();
        ForcedRateIsFullProbability();
    }

    private static void AcceptsAllBaseRateKeys()
    {
        for (int slot = 1; slot <= 5; slot++)
        {
            string key = "SkillExpand" + slot;
            TestAssert.True(
                PerforationRateLogic.TryGetForcedRate(key, out float value),
                key + " should be overridden");
            TestAssert.Near(1.0, value, key + " forced value");
        }
    }

    private static void AcceptsAllPityRateKeys()
    {
        for (int slot = 1; slot <= 5; slot++)
        {
            string key = "CurSkillExpand" + slot;
            TestAssert.True(
                PerforationRateLogic.TryGetForcedRate(key, out float value),
                key + " should be overridden");
            TestAssert.Near(1.0, value, key + " forced value");
        }
    }

    private static void RejectsUnrelatedKeys()
    {
        string[] keys =
        {
            null,
            "",
            "SkillExpand",
            "SkillExpand0",
            "SkillExpand6",
            "SkillExpand10",
            "SkillExpandCost1",
            "CurSkillExpand",
            "CurSkillExpand0",
            "CurSkillExpand6",
            "SkillSlotExpand",
            "EquipSlotMax",
            "skillexpand1",
            "Common_Prop",
            "Legend_Prop"
        };

        foreach (string key in keys)
        {
            TestAssert.False(
                PerforationRateLogic.TryGetForcedRate(key, out _),
                "'" + key + "' should not be overridden");
        }
    }

    private static void ForcedRateIsFullProbability()
    {
        TestAssert.Near(1.0, PerforationRateLogic.ForcedRate, "ForcedRate");
    }
}
