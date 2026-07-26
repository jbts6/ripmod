using System;
using System.IO;
using System.Reflection;
using HarmonyLib;
using MelonLoader;

[assembly: MelonInfo(typeof(RIPGameplayTweaksMod), "RIPGameplayTweaks", "1.1.0", "you")]
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
        string gameAssemblyPath = Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(typeof(RIPGameplayTweaksMod).Assembly.Location) ?? string.Empty,
            "..",
            "GameAssembly.dll"));
        bool oracleFusionEnabled = OracleFusionFeature.TryEnable(
            gameAssemblyPath,
            message => Logger.Msg(message),
            message => Logger.Error(message));
        Logger.Msg("[Config] path=" + ConfigPath +
                   " absorbEnabled=" + CurrentConfig.AbsorbEnabled +
                   " tributeAttributeMultiplier=" + CurrentConfig.TributeAttributeMultiplier +
                   " cashGainMultiplier=" + CurrentConfig.CashGainMultiplier);
        Logger.Msg("[Features] oracleFusion=" + oracleFusionEnabled +
                   " yinluSingleMaterial=true" +
                   " cashGainMultiplier=true" +
                   " absorbFKey=" + CurrentConfig.AbsorbEnabled +
                   " tributeMultiplier=true");
    }

    public override void OnUpdate()
    {
        ReloadConfigIfChanged();
        if (CurrentConfig.AbsorbEnabled &&
            UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.F))
        {
            GlobalDropAbsorber.TryAbsorbAll();
        }
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
            "tributeAttributeMultiplier=1.5",
            "# 战斗内纸钞正向获取倍率，必须大于 0 且不超过 100。",
            "cashGainMultiplier=1.0"
        });
    }
}
