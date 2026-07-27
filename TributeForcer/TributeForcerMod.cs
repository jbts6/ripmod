using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using HarmonyLib;
using MelonLoader;
using Il2CppBattle;
using Il2CppRushUser;
using UnityEngine;

[assembly: MelonInfo(typeof(TributeForcerMod), "TributeForcer", "1.0.0", "you")]
[assembly: MelonGame(null, "REST IN PEACE")]

public sealed class TributeForcerMod : MelonMod
{
    public static MelonLogger.Instance Logger { get; private set; }

    public static string ConfigPath { get; } = Path.GetFullPath(Path.Combine(
        Path.GetDirectoryName(typeof(TributeForcerMod).Assembly.Location) ?? string.Empty,
        "..", "UserData", "TributeForcer.cfg"));

    public static TributeForcerConfig Config { get; private set; } = TributeForcerConfig.CreateDefault();

    private GameObject _uiHost;
    private TributeForcerUI _ui;

    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        var harmony = new HarmonyLib.Harmony("rip.tribute.forcer");
        harmony.PatchAll(Assembly.GetExecutingAssembly());
        ReloadConfig();
        Logger.Msg("[TributeForcer] v1.0.0 已启用。按 F7 打开贡品选择界面。");
        Logger.Msg($"[TributeForcer] cfg={ConfigPath} enabled={Config.Enabled}");
    }

    public override void OnUpdate()
    {
        if (Input.GetKeyDown(KeyCode.F7))
        {
            ToggleUI();
        }
    }

    private void ToggleUI()
    {
        if (_uiHost == null)
        {
            _uiHost = new GameObject("TributeForcerUI");
            UnityEngine.Object.DontDestroyOnLoad(_uiHost);
            _ui = _uiHost.AddComponent<TributeForcerUI>();
            Logger.Msg("[TributeForcer] UI 宿主已创建。");
        }
        _ui.Visible = !_ui.Visible;
        if (_ui.Visible)
        {
            _ui.RefreshCatalog();
        }
    }

    public static void ReloadConfig()
    {
        try
        {
            if (!File.Exists(ConfigPath))
            {
                SaveConfig();
                return;
            }
            Config = TributeForcerConfig.ParseLines(File.ReadAllLines(ConfigPath), Config);
        }
        catch (Exception ex)
        {
            Logger?.Error("[TributeForcer] cfg load err: " + ex.Message);
        }
    }

    public static void SaveConfig()
    {
        try
        {
            string dir = Path.GetDirectoryName(ConfigPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllLines(ConfigPath, Config.Serialize());
        }
        catch (Exception ex)
        {
            Logger?.Error("[TributeForcer] cfg save err: " + ex.Message);
        }
    }
}
