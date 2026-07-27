using System;
using System.IO;
using System.Reflection;
using MelonLoader;

[assembly: MelonInfo(typeof(RIPPerforation100Mod), "RIPPerforation100", "1.1.0", "you")]
[assembly: MelonGame(null, "REST IN PEACE")]

public sealed class RIPPerforation100Mod : MelonMod
{
    public static MelonLogger.Instance Logger { get; private set; }

    public static string ConfigPath { get; } = Path.GetFullPath(Path.Combine(
        Path.GetDirectoryName(typeof(RIPPerforation100Mod).Assembly.Location) ?? string.Empty,
        "..",
        "UserData",
        "RIPPerforation100.cfg"));

    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        EnsureConfigTemplate();
        var harmony = new HarmonyLib.Harmony("rip.perforation.100");
        harmony.PatchAll(Assembly.GetExecutingAssembly());
        Logger.Msg("[Features] perforationSuccessRate=100% materialRestriction=anyUnequipped");
        Logger.Msg("[Config] path=" + ConfigPath);
    }

    private static void EnsureConfigTemplate()
    {
        try
        {
            if (File.Exists(ConfigPath))
                return;

            string directory = Path.GetDirectoryName(ConfigPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllLines(ConfigPath, new[]
            {
                "# RIPPerforation100 1.1.0",
                "# 装备打孔（技能孔扩展）成功率恒定 100%。",
                "# 覆盖 Shop_Mould 配置键 SkillExpand1-5 / CurSkillExpand1-5 为 1.0。",
                "# 已去除\"材料必须为同名阴律\"限制：任何未装备的物品都可作打孔材料。",
                "# 正在装备中的物品仍不可当材料（防止误吞）；材料在打孔时会被消耗。",
                "# 界面概率显示与实际判定同步为 100%，打孔消耗不受影响。",
                "# 当前版本默认启用，无需配置开关。"
            });
        }
        catch (Exception exception)
        {
            Logger?.Error("[Config] template write failed: " + exception);
        }
    }
}
