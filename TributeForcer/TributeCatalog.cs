using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Il2CppBattle;
using Il2CppRushUser;
using Il2CppStage;
using UnityEngine;

public sealed class TributeCatalogEntry
{
    public string Id;
    public string ChineseName;
    public string Rarity;
    public float BaseWeight;
    public string DepotName;
}

public static class TributeCatalog
{
    public const int MaxForceCount = 3;

    public static List<TributeCatalogEntry> Build()
    {
        var result = new List<TributeCatalogEntry>();
        try
        {
            NZStage stage = StageMgr.CurNZStage;
            if (stage == null)
                return result;

            string currentDepot = GetCurrentDepoName(stage);

            var depoNames = EnumerateDepoNames(stage);
            foreach (string name in depoNames)
            {
                if (!stage.TryGetDepo(name, out DepoData depo) || depo == null)
                    continue;
                var weights = depo.DefaultWeightDict;
                if (weights == null)
                    continue;

                foreach (var pair in weights)
                {
                    result.Add(new TributeCatalogEntry
                    {
                        Id = pair.Key,
                        ChineseName = TributeNameResolver.Resolve(pair.Key) ?? "",
                        Rarity = ClassifyRarity(pair.Key),
                        BaseWeight = pair.Value,
                        DepotName = name,
                    });
                }
            }

            result = result
                .GroupBy(x => x.Id, StringComparer.Ordinal)
                .Select(g => g.First())
                .ToList();
            result.Sort((a, b) => string.Compare(a.Id, b.Id, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(currentDepot))
            {
                result = result
                    .OrderByDescending(x => x.DepotName == currentDepot ? 0 : 1)
                    .ThenBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
        }
        catch (Exception ex)
        {
            TributeForcerMod.Logger?.Error("[TributeForcer] catalog build err: " + ex.Message);
        }
        return result;
    }

    private static string GetCurrentDepoName(NZStage stage)
    {
        try
        {
            var userLevel = GetUserLevelUnit();
            return userLevel?.CurAttrDepoName;
        }
        catch
        {
            return null;
        }
    }

    private static IEnumerable<string> EnumerateDepoNames(NZStage stage)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var field = stage.GetType().GetField("_depoDict",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
            {
                var dict = field.GetValue(stage) as System.Collections.IDictionary;
                if (dict != null)
                {
                    foreach (var key in dict.Keys)
                    {
                        if (key is string s)
                            names.Add(s);
                    }
                }
            }
        }
        catch { }

        if (names.Count == 0)
        {
            names.Add("DropTributeLegend");
            names.Add("DropTributeLegendLv1");
            names.Add("RareTribute");
        }
        return names;
    }

    private static UserLevelUnit GetUserLevelUnit()
    {
        try
        {
            var stage = StageMgr.CurNZStage;
            if (stage == null)
                return null;
            var prop = stage.GetType().GetProperty("UserLevelUnit",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop != null)
                return prop.GetValue(stage) as UserLevelUnit;

            var field = stage.GetType().GetField("UserLevelUnit",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
                return field.GetValue(stage) as UserLevelUnit;
        }
        catch { }
        return null;
    }

    public static string ClassifyRarity(string id)
    {
        if (string.IsNullOrEmpty(id))
            return "?";
        if (id.Contains("_Common_", StringComparison.Ordinal))
            return "Common";
        if (id.Contains("_Rare_", StringComparison.Ordinal))
            return "Rare";
        if (id.Contains("_Epic_", StringComparison.Ordinal))
            return "Epic";
        if (id.Contains("_Legendary_", StringComparison.Ordinal) ||
            id.Contains("_Lengendary_", StringComparison.Ordinal))
            return "Legendary";
        if (id.Contains("_Tr", StringComparison.Ordinal))
            return "Special";
        return "Other";
    }

    public static Color RarityColor(string rarity)
    {
        switch (rarity)
        {
            case "Common": return new Color(0.7f, 0.7f, 0.7f);
            case "Rare": return new Color(0.2f, 0.6f, 1.0f);
            case "Epic": return new Color(0.8f, 0.3f, 1.0f);
            case "Legendary": return new Color(1.0f, 0.75f, 0.2f);
            case "Special": return new Color(1.0f, 0.4f, 0.4f);
            default: return Color.white;
        }
    }
}
