using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Collections.Generic;
using HarmonyLib;
using MelonLoader;
using Il2CppBattle;
using Il2CppRushUser;
using Il2CppNZUser;

[assembly: MelonInfo(typeof(QualityBoostMod), "QualityBoostMod", "1.3.0", "you")]
[assembly: MelonGame(null, "REST IN PEACE")]

// v1.3.0: 合并贡品属性倍率与纸钞正向获取倍率。
public class QualityBoostMod : MelonMod
{
    public static MelonLogger.Instance L;
    public static string CfgPath = Path.Combine(
        Path.GetDirectoryName(typeof(QualityBoostMod).Assembly.Location) ?? "",
        "..", "UserData", "QualityBoost.cfg");
    public static string LastSetDepot = null;

    public override void OnInitializeMelon()
    {
        L = LoggerInstance;
        var h = new HarmonyLib.Harmony("rip.tribute.qualityboost");
        h.PatchAll(Assembly.GetExecutingAssembly());
        L.Msg("QualityBoostMod v1.3.0: 品质控制 + 贡品属性倍率 + 纸钞获取倍率。");
        ReloadCfg();
        L.Msg($"  enabled={Cfg.enabled} targetDepot={Cfg.targetDepot} boostChance={Cfg.boostChance}" +
              $" shangshangChance={Cfg.shangshangChance} shangshangMultiplier={Cfg.shangshangMultiplier}");
        L.Msg($"  yinluEnabled={Cfg.yinluEnabled} yinluLegendChance={Cfg.yinluLegendChance}" +
              $" shopEnabled={Cfg.shopEnabled} shopLegendChance={Cfg.shopLegendChance}");
        L.Msg($"  tributeAttributeMultiplier={Cfg.tributeAttributeMultiplier}" +
              $" cashGainMultiplier={Cfg.cashGainMultiplier}");
        L.Msg($"  cfg={Path.GetFullPath(CfgPath)}");
    }

    public class CfgData
    {
        public bool enabled = true;
        public string targetDepot = "RareTribute";
        public double boostChance = 1.0;
        public double shangshangChance = 0.70;
        public double shangshangMultiplier = 1.0;
        public bool yinluEnabled = true;
        public double yinluLegendChance = 0.70;
        public bool shopEnabled = true;
        public double shopLegendChance = 0.70;
        public double tributeAttributeMultiplier = 1.5;
        public double cashGainMultiplier = 1.0;
    }

    public static CfgData Cfg = new CfgData();

    public static void ReloadCfg()
    {
        try
        {
            if (!File.Exists(CfgPath))
                return;

            CfgData previous = Cfg;
            CfgData next = CopyConfig(previous);
            foreach (var line in File.ReadAllLines(CfgPath))
                ApplyConfigLine(next, line);

            ValidateConfig(next, previous);
            Cfg = next;
        }
        catch (Exception ex) { L?.Error("cfg err: " + ex.Message); }
    }

    private static CfgData CopyConfig(CfgData source)
    {
        return new CfgData
        {
            enabled = source.enabled,
            targetDepot = source.targetDepot,
            boostChance = source.boostChance,
            shangshangChance = source.shangshangChance,
            shangshangMultiplier = source.shangshangMultiplier,
            yinluEnabled = source.yinluEnabled,
            yinluLegendChance = source.yinluLegendChance,
            shopEnabled = source.shopEnabled,
            shopLegendChance = source.shopLegendChance,
            tributeAttributeMultiplier = source.tributeAttributeMultiplier,
            cashGainMultiplier = source.cashGainMultiplier
        };
    }

    private static void ApplyConfigLine(CfgData config, string line)
    {
        string content = line.Split('#')[0].Trim();
        if (content.Length == 0)
            return;

        int separator = content.IndexOf('=');
        if (separator < 0)
            return;

        string key = content.Substring(0, separator).Trim().ToLowerInvariant();
        string value = content.Substring(separator + 1).Trim();
        if (key == "enabled" && TryParseBool(value, out bool enabled))
            config.enabled = enabled;
        else if (key == "targetdepot")
            config.targetDepot = value;
        else if (key == "boostchance")
            config.boostChance = ParseConfigDouble(value, config.boostChance, key);
        else if (key == "shangshangchance")
            config.shangshangChance = ParseConfigDouble(
                value,
                config.shangshangChance,
                key);
        else if (key == "shangshangmultiplier")
            config.shangshangMultiplier = ParseConfigDouble(
                value,
                config.shangshangMultiplier,
                key);
        else if (key == "yinluenabled" && TryParseBool(value, out bool yinluEnabled))
            config.yinluEnabled = yinluEnabled;
        else if (key == "yinlulegendchance")
            config.yinluLegendChance = ParseConfigDouble(
                value,
                config.yinluLegendChance,
                key);
        else if (key == "shopenabled" && TryParseBool(value, out bool shopEnabled))
            config.shopEnabled = shopEnabled;
        else if (key == "shoplegendchance")
            config.shopLegendChance = ParseConfigDouble(
                value,
                config.shopLegendChance,
                key);
        else if (key == "tributeattributemultiplier")
            config.tributeAttributeMultiplier = QualityBoostConfigValueParser.ParseMultiplierOrKeep(
                value,
                config.tributeAttributeMultiplier,
                key,
                message => L?.Warning(message));
        else if (key == "cashgainmultiplier")
            config.cashGainMultiplier = QualityBoostConfigValueParser.ParseMultiplierOrKeep(
                value,
                config.cashGainMultiplier,
                key,
                message => L?.Warning(message));
    }

    private static void ValidateConfig(CfgData config, CfgData previous)
    {
        if (!IsFinite(config.shangshangChance) ||
            config.shangshangChance < 0.0 ||
            config.shangshangChance >= 1.0)
        {
            L?.Warning($"shangshangChance={config.shangshangChance} 无效，改用 0（由 shangshangMultiplier 控制）。");
            config.shangshangChance = 0.0;
        }
        if (!IsFinite(config.shangshangMultiplier) ||
            config.shangshangMultiplier < 0.0)
        {
            L?.Warning($"shangshangMultiplier={config.shangshangMultiplier} 无效，恢复为 1.0。");
            config.shangshangMultiplier = 1.0;
        }
        if (!IsFinite(config.boostChance))
        {
            L?.Warning($"boostChance={config.boostChance} 无效，恢复为 1.0。");
            config.boostChance = 1.0;
        }
        if (!IsProbability(config.yinluLegendChance))
        {
            L?.Warning($"yinluLegendChance={config.yinluLegendChance} 无效，保留 {previous.yinluLegendChance}。");
            config.yinluLegendChance = previous.yinluLegendChance;
        }
        if (!IsProbability(config.shopLegendChance))
        {
            L?.Warning($"shopLegendChance={config.shopLegendChance} 无效，保留 {previous.shopLegendChance}。");
            config.shopLegendChance = previous.shopLegendChance;
        }

        config.boostChance = Math.Max(0.0, Math.Min(1.0, config.boostChance));
    }

    private static double ParseConfigDouble(
        string value,
        double previous,
        string key)
    {
        return QualityBoostConfigValueParser.ParseDoubleOrKeep(
            value,
            previous,
            key,
            message => L?.Warning(message));
    }

    private static bool IsProbability(double value)
    {
        return IsFinite(value) && value >= 0.0 && value <= 1.0;
    }

    private static bool IsFinite(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }

    private static bool TryParseBool(string value, out bool result)
    {
        string normalized = value.Trim().ToLowerInvariant();
        if (normalized == "1" || normalized == "true" ||
            normalized == "yes" || normalized == "on")
        {
            result = true;
            return true;
        }
        if (normalized == "0" || normalized == "false" ||
            normalized == "no" || normalized == "off")
        {
            result = false;
            return true;
        }

        result = false;
        return false;
    }

    public static string GetStrField(LevelAttrObj lao, string key)
    {
        try
        {
            var d = lao.StrValueDict;
            if (d == null) return null;
            foreach (var kv in d)
                if (kv.Key == key)
                {
                    var p = kv.Value?.GetType().GetProperty("Value");
                    if (p != null) return p.GetValue(kv.Value) as string;
                }
        }
        catch (Exception ex) { L?.Error($"[GetStrField] key={key} err: {ex.Message}"); }
        return null;
    }

    public static string GetRarityToken(LevelAttrObj lao)
    {
        string bg = GetStrField(lao, "AttrIconBG");
        if (string.IsNullOrEmpty(bg)) return "?";
        int i = bg.IndexOf("xinqian_", StringComparison.OrdinalIgnoreCase);
        if (i < 0) return "(none)";
        i += 8;
        var sb = new StringBuilder();
        while (i < bg.Length && (char.IsLetterOrDigit(bg[i]) || bg[i] == '_')) { sb.Append(bg[i]); i++; }
        return sb.ToString();
    }

    public static string JoinStr(Il2CppSystem.Collections.Generic.List<string> list)
    {
        try
        {
            if (list == null) return "null";
            var sb = new StringBuilder();
            foreach (var s in list) { sb.Append(s).Append(","); }
            return sb.ToString();
        }
        catch (Exception e) { return "err:" + e.Message; }
    }
}

[HarmonyPatch(typeof(UserLevelUnit), "UI_PickAttrRefresh_Keyboard_Data")]
class P_RRefresh
{
    static DateTime _last = DateTime.MinValue;
    static readonly Random Rng = new Random();

    [HarmonyPriority(Priority.Last)]
    static void Prefix(UserLevelUnit __instance)
    {
        try
        {
            QualityBoostMod.ReloadCfg();
            QualityBoostMod.LastSetDepot = null;
            if (!QualityBoostMod.Cfg.enabled)
            {
                TributeWeightController.ApplyForRefresh(__instance);
                return;
            }

            var rnd = Rng.NextDouble();
            if (rnd < QualityBoostMod.Cfg.boostChance)
            {
                QualityBoostMod.LastSetDepot = QualityBoostMod.Cfg.targetDepot;
                try { __instance.CurAttrDepoName = QualityBoostMod.Cfg.targetDepot; }
                catch (Exception ex) { QualityBoostMod.L?.Error("[Prefix] set CurAttrDepoName err: " + ex.Message); }
            }

            TributeWeightController.ApplyForRefresh(__instance);
        }
        catch (Exception ex) { QualityBoostMod.L?.Error("[Prefix] err: " + ex.Message); }
    }

    static void Postfix(UserLevelUnit __instance)
    {
        var now = DateTime.UtcNow;
        if ((now - _last).TotalSeconds < 0.6) return;
        _last = now;
        try
        {
            var sb = new StringBuilder();
            // 刷新后的当前池
            string curAfter = null;
            try { curAfter = __instance.CurAttrDepoName; }
            catch (Exception ex) { QualityBoostMod.L?.Error("[Postfix] read CurAttrDepoName err: " + ex.Message); }
            sb.Append("CurAttrDepoName(after)=").Append(curAfter ?? "?");
            // 我们设的值是否保留
            bool persisted = curAfter != null && QualityBoostMod.LastSetDepot != null &&
                             curAfter.Equals(QualityBoostMod.LastSetDepot, StringComparison.OrdinalIgnoreCase);
            sb.Append(" | setByUs=").Append(QualityBoostMod.LastSetDepot ?? "-")
              .Append(" | persisted=").Append(persisted);
            // 品质
            var list = __instance.LevelAttrList;
            int n = list == null ? 0 : list.Count;
            sb.Append(" | offerings(").Append(n).Append("): ");
            for (int i = 0; i < n && i < 3; i++) sb.Append("[").Append(i).Append(":").Append(QualityBoostMod.GetRarityToken(list[i])).Append("] ");
            QualityBoostMod.L?.Msg("[Tribute] " + sb.ToString());
        }
        catch (Exception e) { QualityBoostMod.L?.Error("[Postfix] err: " + e); }
    }
}
