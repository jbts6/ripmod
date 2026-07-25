using System;
using System.Collections.Generic;

public static class TributeWeightCatalog
{
    public static readonly ISet<string> TrueShangshangIds =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "Tribute_Common_Tr032",
            "Tribute_Common_Tr039",
            "Tribute_Common_Tr065",
            "Tribute_Common_Tr081",
            "Tribute_Epic_Tr101",
            "Tribute_Epic_Tr218",
            "Tribute_Legendary_Tr089",
            "Tribute_Legendary_Tr097",
            "Tribute_Legendary_Tr123",
            "Tribute_Legendary_Tr131",
            "Tribute_Legendary_Tr144",
            "Tribute_Legendary_Tr145",
            "Tribute_Legendary_Tr146",
            "Tribute_Legendary_Tr147",
            "Tribute_Legendary_Tr148",
            "Tribute_Legendary_Tr149",
            "Tribute_Legendary_Tr181",
            "Tribute_Legendary_Tr190",
            "Tribute_Legendary_Tr193",
            "Tribute_Legendary_Tr195",
            "Tribute_Legendary_Tr208",
            "Tribute_Legendary_Tr214",
            "Tribute_Legendary_Tr219",
            "Tribute_Legendary_Tr231",
            "Tribute_Legendary_Tr234",
            "Tribute_Legendary_Tr241",
            "Tribute_Legendary_Tr256",
            "Tribute_Lengendary_Tr001",
            "Tribute_Rare_Tr003",
            "Tribute_Rare_Tr005",
            "Tribute_Rare_Tr021",
            "Tribute_Rare_Tr025",
            "Tribute_Tr304",
            "Tribute_Tr306",
            "Tribute_Tr307",
            "Tribute_Tr308",
            "Tribute_Tr309",
            "Tribute_Tr310",
            "Tribute_Tr315",
            "Tribute_Tr319"
        };

    public static bool IsLegendWeightPool(string depotName)
    {
        if (string.IsNullOrEmpty(depotName))
            return false;

        return depotName.Equals("DropTributeLegend", StringComparison.OrdinalIgnoreCase) ||
               depotName.Equals("DropTributeLegendLv1", StringComparison.OrdinalIgnoreCase) ||
               depotName.Equals("RareTribute", StringComparison.OrdinalIgnoreCase);
    }

}
