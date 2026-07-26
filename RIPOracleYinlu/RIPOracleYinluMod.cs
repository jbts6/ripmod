using System;
using System.IO;
using System.Reflection;
using HarmonyLib;
using MelonLoader;

[assembly: MelonInfo(typeof(RIPOracleYinluMod), "RIPOracleYinlu", "1.0.0", "you")]
[assembly: MelonGame(null, "REST IN PEACE")]

public sealed class RIPOracleYinluMod : MelonMod
{
    public static MelonLogger.Instance Logger { get; private set; }

    public static string ConfigPath { get; } = Path.GetFullPath(Path.Combine(
        Path.GetDirectoryName(typeof(RIPOracleYinluMod).Assembly.Location) ?? string.Empty,
        "..",
        "UserData",
        "RIPOracleYinlu.cfg"));

    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        EnsureConfigTemplate();
        var harmony = new HarmonyLib.Harmony("rip.oracle.yinlu");
        harmony.PatchAll(Assembly.GetExecutingAssembly());

        string gameAssemblyPath = Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(typeof(RIPOracleYinluMod).Assembly.Location) ?? string.Empty,
            "..",
            "GameAssembly.dll"));
        bool oracleFusionEnabled = OracleFusionFeature.TryEnable(
            gameAssemblyPath,
            message => Logger.Msg(message),
            message => Logger.Error(message));

        Logger.Msg("[Features] oracleFusion=" + oracleFusionEnabled +
                   " yinluSingleMaterial=true");
        Logger.Msg("[Config] path=" + ConfigPath + " (no switches; both features always on)");
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
                "# RIPOracleYinlu 1.0.0",
                "# 命石二合一 + 阴律单材料进阶。",
                "# 当前版本两项默认启用，无需配置开关。",
                "# 命石依赖 GameAssembly.dll 哈希与 16 签名全有或全无预检。"
            });
        }
        catch (Exception exception)
        {
            Logger?.Error("[Config] template write failed: " + exception);
        }
    }
}
