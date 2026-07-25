using System;
using System.IO;
using System.Reflection;
using HarmonyLib;
using MelonLoader;

[assembly: MelonInfo(typeof(RIPGameplayTweaksMod), "RIPGameplayTweaks", "1.0.0", "you")]
[assembly: MelonGame(null, "REST IN PEACE")]

public sealed class RIPGameplayTweaksMod : MelonMod
{
    private static DateTime _lastConfigWriteTimeUtc = DateTime.MinValue;

    public static MelonLogger.Instance Logger { get; private set; }
    public static GameplayTweaksConfig CurrentConfig { get; private set; } = new GameplayTweaksConfig();
    public static string ConfigPath { get; } = Path.GetFullPath(Path.Combine(
        Path.GetDirectoryName(typeof(RIPGameplayTweaksMod).Assembly.Location) ?? string.Empty,
        "..",
        "UserData",
        "RIPGameplayTweaks.cfg"));

    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        var harmony = new HarmonyLib.Harmony("rip.gameplay.tweaks");
        harmony.PatchAll(Assembly.GetExecutingAssembly());
        ReloadConfigIfChanged(true);
        Logger.Msg("[Config] path=" + ConfigPath +
                   " absorbEnabled=" + CurrentConfig.AbsorbEnabled +
                   " tributeAttributeMultiplier=" + CurrentConfig.TributeAttributeMultiplier);
    }

    public static void ReloadConfigIfChanged(bool force = false)
    {
        try
        {
            EnsureConfigFileExists();
            DateTime writeTimeUtc = File.GetLastWriteTimeUtc(ConfigPath);
            if (!force && writeTimeUtc == _lastConfigWriteTimeUtc)
                return;

            GameplayTweaksConfig next = GameplayTweaksConfig.ParseLines(
                File.ReadAllLines(ConfigPath),
                CurrentConfig,
                message => Logger?.Warning("[Config] " + message));
            CurrentConfig = next;
            _lastConfigWriteTimeUtc = writeTimeUtc;
        }
        catch (Exception exception)
        {
            Logger?.Error("[Config] load failed: " + exception);
        }
    }

    private static void EnsureConfigFileExists()
    {
        if (File.Exists(ConfigPath))
            return;

        string directory = Path.GetDirectoryName(ConfigPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllLines(ConfigPath, new[]
        {
            "# RIPGameplayTweaks 配置",
            "# 1/true 启用 F 键吸取，0/false 禁用。",
            "absorbEnabled=1",
            "# 贡品属性数值与详情显示的统一倍率。",
            "tributeAttributeMultiplier=1.5"
        });
    }
}
