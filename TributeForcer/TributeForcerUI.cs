using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 纯 IMGUI 面板。不继承 MonoBehaviour，避免 Il2Cpp 下 AddComponent 未注册类型崩溃。
/// 由 <see cref="TributeForcerMod.OnGUI"/> 驱动绘制。
/// </summary>
public sealed class TributeForcerUI
{
    private const string SearchControlName = "TributeForcerSearch";

    private Rect _windowRect = new Rect(40, 40, 560, 640);
    private Vector2 _scrollPos;
    private string _search = "";
    private List<TributeCatalogEntry> _catalog = new List<TributeCatalogEntry>();
    private readonly HashSet<string> _selected = new HashSet<string>(StringComparer.Ordinal);
    private bool _needRefresh = true;

    private GUIStyle _styleLabel;
    private GUIStyle _styleButton;
    private GUIStyle _styleToggle;
    private GUIStyle _styleBox;
    private GUIStyle _styleText;
    private GUIStyle _styleClose;
    private bool _stylesInit;

    public bool Visible { get; set; }

    /// <summary>关闭回调（由 Mod 注入，在 IMGUI 事件路径里调用）。</summary>
    public Action RequestClose { get; set; }

    /// <summary>开关回调。</summary>
    public Action RequestToggle { get; set; }

    public void RefreshCatalog()
    {
        _needRefresh = true;
    }

    public void Draw()
    {
        if (!Visible)
            return;

        try
        {
            // 中文 IME；关闭时再还原由系统处理
            Input.imeCompositionMode = IMECompositionMode.On;

            InitStyles();
            KeyCode hotkey = TributeForcerMod.Config?.ToggleKey ?? KeyCode.F7;
            string title = $"TributeForcer — 强制刷出 ({hotkey} 开关 / ESC 关闭)";
            _windowRect = GUI.Window(
                21370001,
                _windowRect,
                (GUI.WindowFunction)DrawWindow,
                title);
        }
        catch (Exception ex)
        {
            TributeForcerMod.Logger?.Error("[TributeForcer] UI Draw err: " + ex);
            Visible = false;
        }
    }

    private void InitStyles()
    {
        if (_stylesInit)
            return;
        _styleLabel = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            wordWrap = true,
        };
        _styleButton = new GUIStyle(GUI.skin.button)
        {
            fontStyle = FontStyle.Bold,
            fontSize = 14,
        };
        _styleToggle = new GUIStyle(GUI.skin.toggle)
        {
            fontSize = 14,
            wordWrap = false,
            fixedHeight = 22,
        };
        _styleBox = new GUIStyle(GUI.skin.box)
        {
            fontSize = 13,
            wordWrap = true,
            alignment = TextAnchor.UpperLeft,
        };
        _styleText = new GUIStyle(GUI.skin.textField)
        {
            fontSize = 15,
        };
        _styleClose = new GUIStyle(GUI.skin.button)
        {
            fontStyle = FontStyle.Bold,
            fontSize = 14,
            normal = { textColor = Color.white },
        };
        _stylesInit = true;
    }

    private void DrawWindow(int id)
    {
        // ★ 必须在 TextField 之前拦截按键，否则 TextField 会 Use 掉 ESC
        if (TryHandleCloseOrToggleKeys())
        {
            // 已关闭：不再画控件，避免同帧 TextField 重新抢焦点
            return;
        }

        if (_needRefresh)
        {
            try
            {
                _catalog = TributeCatalog.Build();
            }
            catch (Exception ex)
            {
                TributeForcerMod.Logger?.Error("[TributeForcer] catalog build err: " + ex);
                _catalog = new List<TributeCatalogEntry>();
            }
            _needRefresh = false;
        }

        GUILayout.Space(4);

        // 顶栏：统计 + 关闭按钮（不依赖快捷键）
        GUILayout.BeginHorizontal();
        int resolved = _catalog.Count(e => !string.IsNullOrEmpty(e.ChineseName));
        int selectable = _catalog.Count(e => e.CanSelect);
        int locked = _catalog.Count(e => e.IsLocked);
        int full = _catalog.Count(e => e.IsFull);
        int owned = _catalog.Count(e => e.OwnedCount > 0);
        GUILayout.Label(
            $"贡品 {_catalog.Count} — 已选 {_selected.Count}/{TributeCatalog.MaxForceCount} — 中文 {resolved}\n" +
            $"可选 {selectable} / 已持有 {owned} / 已满 {full} / 未解锁 {locked}",
            _styleLabel,
            GUILayout.ExpandWidth(true));
        Color prev = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.75f, 0.2f, 0.2f);
        if (GUILayout.Button("关闭", _styleClose, GUILayout.Width(64), GUILayout.Height(36)))
        {
            DoClose();
            GUI.backgroundColor = prev;
            return;
        }
        GUI.backgroundColor = prev;
        GUILayout.EndHorizontal();

        GUILayout.Space(4);

        // 搜索行
        GUILayout.BeginHorizontal();
        GUI.SetNextControlName(SearchControlName);
        string newSearch = GUILayout.TextField(_search ?? "", _styleText, GUILayout.Height(28), GUILayout.ExpandWidth(true));
        if (newSearch != _search)
            _search = newSearch;

        if (GUILayout.Button("粘贴", _styleButton, GUILayout.Width(56), GUILayout.Height(28)))
        {
            try
            {
                string clip = GUIUtility.systemCopyBuffer;
                if (!string.IsNullOrEmpty(clip))
                    _search = (_search ?? "") + clip;
            }
            catch (Exception ex)
            {
                TributeForcerMod.Logger?.Warning("[TributeForcer] 粘贴失败: " + ex.Message);
            }
        }
        GUILayout.EndHorizontal();

        // Ctrl+V（仅搜索框聚焦）
        Event e = Event.current;
        if (e != null && e.type == EventType.KeyDown && e.control && e.keyCode == KeyCode.V)
        {
            if (string.Equals(GUI.GetNameOfFocusedControl(), SearchControlName, StringComparison.Ordinal))
            {
                try
                {
                    string clip = GUIUtility.systemCopyBuffer;
                    if (!string.IsNullOrEmpty(clip))
                    {
                        _search = (_search ?? "") + clip;
                        e.Use();
                    }
                }
                catch { /* ignore */ }
            }
        }

        GUILayout.Space(4);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("清空搜索", _styleButton, GUILayout.Height(30)))
            _search = "";
        if (GUILayout.Button("清空选择", _styleButton, GUILayout.Height(30)))
            _selected.Clear();
        GUILayout.EndHorizontal();

        GUILayout.Space(4);

        string q = _search?.Trim().ToLowerInvariant() ?? "";
        var filtered = _catalog.Where(entry =>
        {
            if (string.IsNullOrEmpty(q))
                return true;
            string id = entry.Id.ToLowerInvariant();
            string name = (entry.ChineseName ?? "").ToLowerInvariant();
            string rarity = (entry.Rarity ?? "").ToLowerInvariant();
            string status = (entry.StatusLabel ?? "").ToLowerInvariant();
            return id.Contains(q) || name.Contains(q) || rarity.Contains(q) || status.Contains(q);
        }).ToList();

        _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Height(380));
        foreach (var entry in filtered)
        {
            bool isSel = _selected.Contains(entry.Id);
            GUI.color = TributeCatalog.RowColor(entry);

            string namePart = string.IsNullOrEmpty(entry.ChineseName)
                ? entry.Id
                : $"{entry.ChineseName}  ({entry.Id})";
            string display = $"{namePart}  [{entry.Rarity}]  ·{entry.StatusLabel}";

            bool nowSel = GUILayout.Toggle(isSel, display, _styleToggle);
            GUI.color = Color.white;

            if (nowSel && !isSel)
            {
                if (_selected.Count >= TributeCatalog.MaxForceCount)
                {
                    var oldest = _selected.FirstOrDefault();
                    if (oldest != null)
                        _selected.Remove(oldest);
                }
                _selected.Add(entry.Id);
            }
            else if (!nowSel && isSel)
            {
                _selected.Remove(entry.Id);
            }
        }
        GUILayout.EndScrollView();

        GUILayout.Space(8);
        GUI.enabled = _selected.Count > 0;
        if (GUILayout.Button("★ 应用到下次刷新 ★", _styleButton, GUILayout.Height(40)))
        {
            TributeForcerRefreshPatch.SetForcedIds(_selected);
            string label = string.Join(", ", _selected.Select(s =>
            {
                string n = TributeNameResolver.Resolve(s);
                return string.IsNullOrEmpty(n) ? s : $"{n}({s})";
            }));
            TributeForcerMod.Logger?.Msg("[TributeForcer] 已设置强制贡品: " + label);
            DoClose();
            return;
        }
        GUI.enabled = true;

        GUILayout.Space(4);
        GUI.color = Color.yellow;
        KeyCode hotkey = TributeForcerMod.Config?.ToggleKey ?? KeyCode.F7;
        GUILayout.Label(
            $"提示: {hotkey} 开关 · ESC/右上角「关闭」可关（搜索框聚焦时同样有效）。\n" +
            "选中后点刷新，最多 3 个。行颜色=稀有度。",
            _styleBox);
        GUI.color = Color.white;

        GUI.DragWindow(new Rect(0, 0, 10000, 24));
    }

    /// <summary>
    /// 在任意控件（尤其 TextField）处理之前消费 ESC / 开关快捷键。
    /// 使用 Event 路径，不轮询。
    /// </summary>
    /// <returns>true 表示已关闭，调用方应停止绘制。</returns>
    private bool TryHandleCloseOrToggleKeys()
    {
        Event e = Event.current;
        if (e == null)
            return false;

        // MelonLoader / Window 内有时 type 已是 Used，但 rawType 仍是 KeyDown
        bool isKeyDown = e.type == EventType.KeyDown || e.rawType == EventType.KeyDown;
        if (!isKeyDown)
            return false;

        KeyCode code = e.keyCode;
        // 部分 IME 取消组合时 keyCode 为空，character 也可能为空；仅认明确 KeyCode
        if (code == KeyCode.None)
            return false;

        KeyCode hotkey = TributeForcerMod.Config?.ToggleKey ?? KeyCode.F7;

        // 打开态：ESC 与热键一律关闭（不 Toggle，避免与其它路径打架）
        if (code == KeyCode.Escape || (hotkey != KeyCode.None && code == hotkey))
        {
            e.Use();
            DoClose();
            return true;
        }

        return false;
    }

    private void DoClose()
    {
        try
        {
            GUIUtility.keyboardControl = 0;
            GUIUtility.hotControl = 0;
            GUI.FocusControl(null);
        }
        catch { /* ignore */ }

        if (RequestClose != null)
            RequestClose.Invoke();
        else
            Visible = false;
    }
}
