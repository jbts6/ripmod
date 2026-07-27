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

    /// <summary>配置 MAXTriCount，0 表示未知。</summary>
    public int MaxCount;
    /// <summary>当前已持有数量。</summary>
    public int OwnedCount;
    /// <summary>仍在 DropSys 锁定集（图鉴未解锁）。</summary>
    public bool IsLocked;
    /// <summary>持有数已达上限。</summary>
    public bool IsFull;
    /// <summary>理论上可被强制刷出（未锁且未满）。</summary>
    public bool CanSelect = true;
    /// <summary>列表状态短标签：可选/未解锁/已满…</summary>
    public string StatusLabel = "可选";
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
                        ChineseName = "",
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

            // 进界面即对当前仓库全部 ID 主动解析中文名（不依赖“刷出来过”）
            TributeNameResolver.EnsureResolved(result.Select(e => e.Id));
            foreach (var entry in result)
                entry.ChineseName = TributeNameResolver.Resolve(entry.Id) ?? "";

            // 解锁 / 持有上限状态
            TributeStatus.Fill(result);

            // 排序：可选 → 已满 → 未解锁；同组内稀有度高优先，再按中文名
            // （不再按仓库拆分，避免同稀有度被打散）
            result.Sort((a, b) =>
            {
                int sa = StatusRank(a);
                int sb = StatusRank(b);
                if (sa != sb) return sa - sb;

                int ra = RarityRank(a.Rarity);
                int rb = RarityRank(b.Rarity);
                if (ra != rb) return ra - rb;

                string na = string.IsNullOrEmpty(a.ChineseName) ? a.Id : a.ChineseName;
                string nb = string.IsNullOrEmpty(b.ChineseName) ? b.Id : b.ChineseName;
                int nc = string.Compare(na, nb, StringComparison.OrdinalIgnoreCase);
                if (nc != 0) return nc;
                return string.Compare(a.Id, b.Id, StringComparison.OrdinalIgnoreCase);
            });
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

    /// <summary>可选(0) → 已满(1) → 未解锁(2)</summary>
    private static int StatusRank(TributeCatalogEntry e)
    {
        if (e == null) return 9;
        if (e.IsLocked) return 2;
        if (e.IsFull) return 1;
        return 0; // 可选（含已持有未满）
    }

    private static int RarityRank(string rarity)
    {
        switch (rarity)
        {
            case "Legendary": return 0;
            case "Epic": return 1;
            case "Rare": return 2;
            case "Special": return 3;
            case "Common": return 4;
            default: return 5;
        }
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

    /// <summary>
    /// 行颜色只表示稀有度；未解锁时降低透明度，不改色相，避免和状态语义打架。
    /// </summary>
    public static Color RowColor(TributeCatalogEntry e)
    {
        Color c = RarityColor(e?.Rarity);
        if (e != null && e.IsLocked)
            c.a = 0.45f;
        else if (e != null && e.IsFull)
            c.a = 0.65f;
        return c;
    }
}
