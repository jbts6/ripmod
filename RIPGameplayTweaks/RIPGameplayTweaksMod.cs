using System;
using System.IO;
using MelonLoader;

[assembly: MelonInfo(typeof(RIPGameplayTweaksMod), "RIPGameplayTweaks", "1.2.0", "you")]
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
        ReloadConfigIfChanged(true);
        Logger.Msg("[Config] path=" + ConfigPath +
                   " absorbEnabled=" + CurrentConfig.AbsorbEnabled);
        Logger.Msg("[Features] absorbFKey=" + CurrentConfig.AbsorbEnabled +
                   " (absorb-only; multipliers→QualityBoost; oracle/yinlu→RIPOracleYinlu)");
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
            "# RIPGameplayTweaks 配置（仅 F 键全图吸取）",
            "# 1/true 启用 F 键吸取，0/false 禁用。",
            "absorbEnabled=1",
            "# 贡品属性倍率与纸钞倍率已迁至 QualityBoost.cfg",
            "# 命石二合一与阴律单材料进阶见 RIPOracleYinlu"
        });
    }
}
