using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class TributeForcerUI : MonoBehaviour
{
    private Rect _windowRect = new Rect(50, 50, 460, 540);
    private Vector2 _scrollPos;
    private string _search = "";
    private List<TributeCatalogEntry> _catalog = new List<TributeCatalogEntry>();
    private readonly HashSet<string> _selected = new HashSet<string>(StringComparer.Ordinal);
    private bool _needRefresh = true;

    private GUIStyle _styleWindow;
    private GUIStyle _styleLabel;
    private GUIStyle _styleButton;
    private GUIStyle _styleToggle;
    private GUIStyle _styleBox;
    private bool _stylesInit;

    public bool Visible { get; set; }

    public void RefreshCatalog()
    {
        _needRefresh = true;
    }

    private void OnGUI()
    {
        if (!Visible)
            return;

        InitStyles();
        _windowRect = UnityEngine.GUI.Window(
            21370001,
            _windowRect,
            (Action<int>)DrawWindow,
            "TributeForcer — 强制刷出贡品 (F7 关闭)");
    }

    private void InitStyles()
    {
        if (_stylesInit)
            return;
        _styleWindow = new GUIStyle(GUI.skin.window)
        {
            fontStyle = FontStyle.Bold,
            fontSize = 14,
        };
        _styleLabel = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
        };
        _styleButton = new GUIStyle(GUI.skin.button)
        {
            fontStyle = FontStyle.Bold,
            fontSize = 12,
        };
        _styleToggle = new GUIStyle(GUI.skin.toggle)
        {
            fontSize = 11,
        };
        _styleBox = new GUIStyle(GUI.skin.box)
        {
            fontSize = 11,
        };
        _stylesInit = true;
    }

    private void DrawWindow(int id)
    {
        if (_needRefresh)
        {
            _catalog = TributeCatalog.Build();
            _needRefresh = false;
        }

        GUILayout.Space(4);

        int resolved = _catalog.Count(e => !string.IsNullOrEmpty(e.ChineseName));
        GUILayout.Label($"当前仓库贡品 ({_catalog.Count}) — 已选 {_selected.Count}/{TributeCatalog.MaxForceCount} — 已识别中文名 {resolved}", _styleLabel);
        GUILayout.Space(4);

        _search = GUILayout.TextField(_search, GUILayout.Height(24));
        GUILayout.Space(4);

        if (GUILayout.Button("清空选择", _styleButton, GUILayout.Height(28)))
        {
            _selected.Clear();
        }

        GUILayout.Space(4);

        string q = _search?.Trim().ToLowerInvariant() ?? "";
        var filtered = _catalog.Where(e =>
        {
            if (string.IsNullOrEmpty(q))
                return true;
            string id = e.Id.ToLowerInvariant();
            string name = (e.ChineseName ?? "").ToLowerInvariant();
            string rarity = (e.Rarity ?? "").ToLowerInvariant();
            return id.Contains(q) || name.Contains(q) || rarity.Contains(q);
        }).ToList();

        _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Height(340));
        foreach (var entry in filtered)
        {
            bool isSel = _selected.Contains(entry.Id);
            Color c = TributeCatalog.RarityColor(entry.Rarity);
            GUI.color = c;

            string display = string.IsNullOrEmpty(entry.ChineseName)
                ? $"{entry.Id}  [{entry.Rarity}]"
                : $"{entry.ChineseName}  ({entry.Id})  [{entry.Rarity}]";

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
        if (GUILayout.Button("★ 应用到下次刷新 ★", _styleButton, GUILayout.Height(36)))
        {
            TributeForcerRefreshPatch.SetForcedIds(_selected);
            string label = string.Join(", ", _selected.Select(s =>
            {
                string n = TributeNameResolver.Resolve(s);
                return string.IsNullOrEmpty(n) ? s : $"{n}({s})";
            }));
            TributeForcerMod.Logger?.Msg("[TributeForcer] 已设置强制贡品: " + label);
            Visible = false;
        }
        GUI.enabled = true;

        GUILayout.Space(4);
        GUI.color = Color.yellow;
        GUILayout.Label("提示: 选中后点击刷新即可刷出。最多 3 个。\n中文名在游戏显示贡品后自动抓取。", _styleBox);
        GUI.color = Color.white;

        GUI.DragWindow(new Rect(0, 0, 10000, 20));
    }
}
