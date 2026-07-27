using System;
using System.IO;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(TributeForcerMod), "TributeForcer", "1.2.5", "you")]
[assembly: MelonGame(null, "REST IN PEACE")]

public sealed class TributeForcerMod : MelonMod
{
    public static MelonLogger.Instance Logger { get; private set; }

    public static string ConfigPath { get; } = Path.GetFullPath(Path.Combine(
        Path.GetDirectoryName(typeof(TributeForcerMod).Assembly.Location) ?? string.Empty,
        "..", "UserData", "TributeForcer.cfg"));

    public static TributeForcerConfig Config { get; private set; } = TributeForcerConfig.CreateDefault();

    private TributeForcerUI _ui;

    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        var harmony = new HarmonyLib.Harmony("rip.tribute.forcer");
        harmony.PatchAll(Assembly.GetExecutingAssembly());
        ReloadConfig();
        SaveConfig();
        _ui = new TributeForcerUI();
        _ui.RequestClose = CloseUI;
        _ui.RequestToggle = ToggleUI;
        Logger.Msg($"[TributeForcer] v1.2.5 已启用。按 {Config.ToggleKey} 开关，ESC/关闭按钮 可关。");
        Logger.Msg($"[TributeForcer] cfg={ConfigPath} enabled={Config.Enabled} hotkey={Config.ToggleKey}");
    }

    public override void OnApplicationQuit()
    {
        TributeNameResolver.FlushIfDirty();
    }

    public override void OnUpdate()
    {
        // 仅 Input.GetKeyDown 边沿（不是每帧扫键轮询）。
        // 打开态关闭：IMGUI 在 TextField 前拦 Event；若 Melon 只派发 Repaint，则靠这里兜底。
        try
        {
            if (_ui == null)
                return;

            KeyCode hotkey = Config?.ToggleKey ?? KeyCode.F7;

            if (_ui.Visible)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    CloseUI();
                    return;
                }
                if (hotkey != KeyCode.None && hotkey != KeyCode.Escape && Input.GetKeyDown(hotkey))
                    CloseUI();
                return;
            }

            if (hotkey != KeyCode.None && hotkey != KeyCode.Escape && Input.GetKeyDown(hotkey))
                OpenUI();
        }
        catch (Exception ex)
        {
            Logger?.Error("[TributeForcer] key handle err: " + ex);
        }
    }

    public override void OnGUI()
    {
        try
        {
            _ui?.Draw();
        }
        catch (Exception ex)
        {
            Logger?.Error("[TributeForcer] OnGUI err: " + ex);
            if (_ui != null)
                _ui.Visible = false;
        }
    }

    private void ToggleUI()
    {
        if (_ui == null)
        {
            _ui = new TributeForcerUI();
            _ui.RequestClose = CloseUI;
            _ui.RequestToggle = ToggleUI;
        }

        if (_ui.Visible)
            CloseUI();
        else
            OpenUI();
    }

    private void OpenUI()
    {
        if (_ui == null)
        {
            _ui = new TributeForcerUI();
            _ui.RequestClose = CloseUI;
            _ui.RequestToggle = ToggleUI;
        }
        _ui.Visible = true;
        _ui.RefreshCatalog();
        Logger?.Msg("[TributeForcer] UI 已打开。");
    }

    private void CloseUI()
    {
        if (_ui == null || !_ui.Visible)
            return;

        _ui.Visible = false;
        try
        {
            // 必须在 OnGUI 事件里清焦点，否则 TextField 会继续吞键
            GUIUtility.keyboardControl = 0;
            GUIUtility.hotControl = 0;
            GUI.FocusControl(null);
        }
        catch { /* OnUpdate 路径下 GUI 调用可能无效，忽略 */ }
        Logger?.Msg("[TributeForcer] UI 已关闭。");
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
