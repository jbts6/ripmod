using System.Collections.Generic;

internal static class PerforationMaterialLogic
{
    public static bool ShouldReject(
        bool hasSlotTarget,
        string materialUuid,
        IReadOnlyList<string> equippedUuids)
    {
        if (!hasSlotTarget || string.IsNullOrEmpty(materialUuid))
            return true;

        if (equippedUuids == null)
            return false;

        for (int index = 0; index < equippedUuids.Count; index++)
        {
            if (materialUuid == equippedUuids[index])
                return true;
        }

        return false;
    }
}
