using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using HarmonyLib;
using Il2Cpp;
using Il2CppGameConfig;
using Il2CppLanguage;
using Il2CppNZUI;
using Il2CppSysCommon;
using MelonLoader;
using Il2CppDict = Il2CppSystem.Collections.Generic.Dictionary<string, Il2CppGameConfig.CnfBase>;
using Il2CppStrDict = Il2CppSystem.Collections.Generic.Dictionary<string, string>;

/// <summary>
/// 贡品中文名解析。
/// 游戏真实链路（ViewBagTributeDetails）：
///   tipId = CommonObj.FetchStrValue("TributeName")
///   name  = LangLogic.GetTipStr(tipId)
/// 配置来源：ConfLogic.GetConf("TributeView" / "TributeView_DLC01", cnfId).KV
/// </summary>
public static class TributeNameResolver
{
    private static readonly System.Collections.Generic.Dictionary<string, string> _names =
        new System.Collections.Generic.Dictionary<string, string>(StringComparer.Ordinal);

    private static readonly System.Collections.Generic.HashSet<string> _missCache =
        new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);

    private static bool _diskLoaded;
    private static bool _diskDirty;
    private static bool _didProbe;
    private static string _cachePath;

    private static readonly string[] ViewTables =
    {
        "TributeView",
        "TributeView_DLC01",
    };

    public static string CachePath
    {
        get
        {
            if (_cachePath != null)
                return _cachePath;
            string dir = Path.GetDirectoryName(typeof(TributeForcerMod).Assembly.Location) ?? string.Empty;
            _cachePath = Path.GetFullPath(Path.Combine(dir, "..", "UserData", "TributeNames.cache"));
            return _cachePath;
        }
    }

    private static string DebugPath
    {
        get
        {
            string dir = Path.GetDirectoryName(CachePath) ?? ".";
            return Path.Combine(dir, "TributeNameDebug.txt");
        }
    }

    public static string Resolve(string tributeId)
    {
        if (string.IsNullOrEmpty(tributeId))
            return null;

        EnsureDiskLoaded();

        lock (_names)
        {
            if (_names.TryGetValue(tributeId, out string cached) && !string.IsNullOrEmpty(cached))
                return cached;
        }

        string resolved = ResolveFromGame(tributeId);
        if (!string.IsNullOrEmpty(resolved))
        {
            Register(tributeId, resolved, persist: true);
            return resolved;
        }

        return null;
    }

    public static int EnsureResolved(System.Collections.Generic.IEnumerable<string> tributeIds)
    {
        if (tributeIds == null)
            return 0;

        EnsureDiskLoaded();

        var idList = new System.Collections.Generic.List<string>();
        foreach (string id in tributeIds)
        {
            if (!string.IsNullOrEmpty(id))
                idList.Add(id);
        }

        if (!_didProbe && idList.Count > 0)
        {
            _didProbe = true;
            WriteProbe(idList[0]);
        }

        // 优先：整表扫描 TributeView 缓存（一次拿全量映射）
        int bulk = TryBulkResolveFromLoaders();
        if (bulk > 0)
            TributeForcerMod.Logger?.Msg($"[TributeForcer] 从配置表批量解析中文名 {bulk} 条");

        int resolved = 0;
        foreach (string id in idList)
        {
            if (!string.IsNullOrEmpty(Resolve(id)))
                resolved++;
        }

        FlushIfDirty();
        TributeForcerMod.Logger?.Msg(
            $"[TributeForcer] 中文名解析完成: {resolved}/{idList.Count} (缓存总数={CachedCount})");
        return resolved;
    }

    public static int CachedCount
    {
        get
        {
            EnsureDiskLoaded();
            lock (_names)
                return _names.Count;
        }
    }

    internal static void Register(string tributeId, string chineseName, bool persist = true)
    {
        if (string.IsNullOrEmpty(tributeId) || string.IsNullOrEmpty(chineseName))
            return;
        if (!IsUsableDisplayName(chineseName))
            return;

        lock (_names)
        {
            if (_names.TryGetValue(tributeId, out string existing) &&
                string.Equals(existing, chineseName, StringComparison.Ordinal))
            {
                return;
            }
            _names[tributeId] = chineseName;
            _missCache.Remove(tributeId);
            if (persist)
                _diskDirty = true;
        }
    }

    /// <summary>
    /// tipId 已拿到时，走与游戏相同的 GetTipStr 本地化。
    /// </summary>
    internal static void RegisterFromTipId(string tributeId, string tipId)
    {
        if (string.IsNullOrEmpty(tributeId) || string.IsNullOrEmpty(tipId))
            return;

        string name = LocalizeTip(tipId);
        if (string.IsNullOrEmpty(name))
        {
            // 配置里偶尔直接写中文
            if (ContainsCjk(tipId) && IsUsableDisplayName(tipId))
                name = tipId;
            else
                return;
        }
        Register(tributeId, name, persist: true);
    }

    public static void FlushIfDirty()
    {
        if (!_diskDirty)
            return;
        try
        {
            System.Collections.Generic.Dictionary<string, string> snapshot;
            lock (_names)
            {
                snapshot = new System.Collections.Generic.Dictionary<string, string>(_names, StringComparer.Ordinal);
                _diskDirty = false;
            }

            string path = CachePath;
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var lines = new System.Collections.Generic.List<string>(snapshot.Count + 2)
            {
                "# TributeForcer 中文名缓存 (id=中文名)",
                "# 自动生成，可手工补充。",
            };
            foreach (var pair in snapshot)
            {
                if (string.IsNullOrEmpty(pair.Key) || string.IsNullOrEmpty(pair.Value))
                    continue;
                lines.Add(pair.Key + "=" + EscapeValue(pair.Value));
            }
            File.WriteAllLines(path, lines, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            TributeForcerMod.Logger?.Warning("[TributeForcer] 中文名缓存写入失败: " + ex.Message);
            _diskDirty = true;
        }
    }

    private static void EnsureDiskLoaded()
    {
        if (_diskLoaded)
            return;
        _diskLoaded = true;
        try
        {
            string path = CachePath;
            if (!File.Exists(path))
                return;

            foreach (string raw in File.ReadAllLines(path, Encoding.UTF8))
            {
                string line = raw?.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("#", StringComparison.Ordinal))
                    continue;
                int sep = line.IndexOf('=');
                if (sep <= 0)
                    continue;
                string id = line.Substring(0, sep).Trim();
                string name = UnescapeValue(line.Substring(sep + 1).Trim());
                if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(name))
                    continue;
                lock (_names)
                {
                    if (!_names.ContainsKey(id))
                        _names[id] = name;
                }
            }
            TributeForcerMod.Logger?.Msg($"[TributeForcer] 已加载中文名缓存 {_names.Count} 条");
        }
        catch (Exception ex)
        {
            TributeForcerMod.Logger?.Warning("[TributeForcer] 中文名缓存读取失败: " + ex.Message);
        }
    }

    private static string ResolveFromGame(string tributeId)
    {
        lock (_missCache)
        {
            if (_missCache.Contains(tributeId))
                return null;
        }

        try
        {
            BaseFramework fw = BaseFramework.Instance;
            if (fw == null || fw.ConfHandler == null || fw.LangHandler == null)
                return null;

            // 1) 配置表 KV 中的 TributeName → tipId → 中文
            string fromConf = ResolveViaTributeView(fw, tributeId);
            if (!string.IsNullOrEmpty(fromConf))
                return fromConf;

            // 2) tipId 直接等于 cnfId
            string direct = LocalizeTip(tributeId);
            if (!string.IsNullOrEmpty(direct))
                return direct;
        }
        catch (Exception ex)
        {
            TributeForcerMod.Logger?.Warning(
                "[TributeForcer] 解析中文名异常 " + tributeId + ": " + ex.Message);
            return null;
        }

        lock (_missCache)
        {
            _missCache.Add(tributeId);
        }
        return null;
    }

    private static string ResolveViaTributeView(BaseFramework fw, string tributeId)
    {
        ConfLogic conf = fw.ConfHandler;
        if (conf == null)
            return null;

        foreach (string table in ViewTables)
        {
            CnfTableData tableData = null;
            try { tableData = conf.GetConf(table, tributeId); }
            catch { tableData = null; }
            if (tableData == null)
                continue;

            string tipId = ExtractTipIdFromKv(tableData.KV);
            if (string.IsNullOrEmpty(tipId))
                continue;

            if (ContainsCjk(tipId) && IsUsableDisplayName(tipId))
                return tipId;

            string localized = LocalizeTip(tipId);
            if (!string.IsNullOrEmpty(localized))
                return localized;
        }

        return null;
    }

    private static int TryBulkResolveFromLoaders()
    {
        int added = 0;
        try
        {
            BaseFramework fw = BaseFramework.Instance;
            ConfLogic conf = fw?.ConfHandler;
            if (conf == null)
                return 0;

            foreach (string table in ViewTables)
            {
                ConfLoader loader = null;
                try { loader = conf.GetCnfLoader(table); }
                catch { loader = null; }
                if (loader == null)
                    continue;

                Il2CppDict caches = null;
                try { caches = loader.GetCaches(); }
                catch { caches = null; }
                if (caches == null || caches.Count == 0)
                    continue;

                foreach (var pair in caches)
                {
                    string id = pair.Key;
                    if (string.IsNullOrEmpty(id) || !id.StartsWith("Tribute_", StringComparison.Ordinal))
                        continue;

                    lock (_names)
                    {
                        if (_names.ContainsKey(id))
                            continue;
                    }

                    CnfTableData row = pair.Value as CnfTableData;
                    if (row == null && pair.Value != null)
                    {
                        try { row = pair.Value.TryCast<CnfTableData>(); }
                        catch { row = null; }
                    }
                    if (row == null)
                        continue;

                    string tipId = ExtractTipIdFromKv(row.KV);
                    if (string.IsNullOrEmpty(tipId))
                        continue;

                    string name = ContainsCjk(tipId) && IsUsableDisplayName(tipId)
                        ? tipId
                        : LocalizeTip(tipId);
                    if (string.IsNullOrEmpty(name))
                        continue;

                    Register(id, name, persist: true);
                    added++;
                }
            }
        }
        catch (Exception ex)
        {
            TributeForcerMod.Logger?.Warning("[TributeForcer] 批量解析失败: " + ex.Message);
        }
        return added;
    }

    /// <summary>
    /// TributeView 配置是槽位结构，例如：
    ///   Str1 = TributeName
    ///   StrValue1 = Tribute41test   ← tipId
    /// 不能直接 GetValue("TributeName")。
    /// </summary>
    private static string ExtractTipIdFromKv(CnfKV kv)
    {
        if (kv == null)
            return null;

        // 1) 槽位：StrN == "TributeName" → StrValueN
        try
        {
            for (int i = 1; i <= 32; i++)
            {
                string field = null;
                try { field = kv.GetValue("Str" + i); } catch { field = null; }
                if (string.IsNullOrEmpty(field))
                    continue;
                if (!string.Equals(field.Trim(), "TributeName", StringComparison.OrdinalIgnoreCase))
                    continue;

                string tipId = null;
                try { tipId = kv.GetValue("StrValue" + i); } catch { tipId = null; }
                if (!string.IsNullOrEmpty(tipId))
                    return tipId.Trim();
            }
        }
        catch { /* fall through */ }

        // 2) 兼容：直接键
        try
        {
            string direct = kv.GetValue("TributeName");
            if (!string.IsNullOrEmpty(direct))
                return direct.Trim();
        }
        catch { /* ignore */ }

        // 3) 回退：扫全部 KV
        try
        {
            Il2CppStrDict datas = kv.GetCnfDatas();
            if (datas == null || datas.Count == 0)
                return null;

            // 建索引：Str1 → TributeName, StrValue1 → tip
            var strFields = new System.Collections.Generic.Dictionary<int, string>();
            var strValues = new System.Collections.Generic.Dictionary<int, string>();
            foreach (var pair in datas)
            {
                string key = pair.Key ?? "";
                string val = pair.Value;
                if (string.IsNullOrEmpty(val))
                    continue;

                if (key.StartsWith("StrValue", StringComparison.Ordinal) &&
                    int.TryParse(key.Substring("StrValue".Length), out int vi))
                {
                    strValues[vi] = val.Trim();
                    continue;
                }
                if (key.StartsWith("Str", StringComparison.Ordinal) &&
                    !key.StartsWith("StrValue", StringComparison.Ordinal) &&
                    !key.StartsWith("StrCal", StringComparison.Ordinal) &&
                    int.TryParse(key.Substring(3), out int si))
                {
                    strFields[si] = val.Trim();
                }
            }

            foreach (var kvField in strFields)
            {
                if (!string.Equals(kvField.Value, "TributeName", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (strValues.TryGetValue(kvField.Key, out string tip) && !string.IsNullOrEmpty(tip))
                    return tip;
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    private static string LocalizeTip(string tipId)
    {
        if (string.IsNullOrEmpty(tipId))
            return null;

        try
        {
            string viaTools = NZUIEditorTools.GetTipsValue(tipId);
            if (IsResolvedLocalized(tipId, viaTools))
                return viaTools.Trim();
        }
        catch { /* ignore */ }

        try
        {
            LangLogic lang = BaseFramework.Instance?.LangHandler;
            if (lang != null)
            {
                string viaLang = lang.GetTipStr(tipId);
                if (IsResolvedLocalized(tipId, viaLang))
                    return viaLang.Trim();

                if (lang.GetTipStr(tipId, out string outVal) && IsResolvedLocalized(tipId, outVal))
                    return outVal.Trim();
            }
        }
        catch { /* ignore */ }

        return null;
    }

    private static bool IsResolvedLocalized(string tipId, string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;
        if (!IsUsableDisplayName(value))
            return false;
        // 未命中时 LangLogic 会原样返回 tipId
        if (string.Equals(value, tipId, StringComparison.Ordinal))
            return ContainsCjk(value);
        return true;
    }

    private static bool IsUsableDisplayName(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;
        if (value.Length > 64)
            return false;
        if (value.IndexOf('\n') >= 0 || value.IndexOf('\r') >= 0)
            return false;
        // 排除明显的资源路径
        if (value.IndexOf('/') >= 0 || value.IndexOf('\\') >= 0)
            return false;
        return true;
    }

    private static bool ContainsCjk(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;
        foreach (char c in value)
        {
            if (c >= 0x4E00 && c <= 0x9FFF) return true;
            if (c >= 0x3400 && c <= 0x4DBF) return true;
            if (c >= 0xF900 && c <= 0xFAFF) return true;
        }
        return false;
    }

    private static void WriteProbe(string sampleId)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# TributeForcer 中文名探测 " + DateTime.Now.ToString("s"));
        sb.AppendLine("sampleId=" + sampleId);
        try
        {
            BaseFramework fw = BaseFramework.Instance;
            sb.AppendLine("BaseFramework.Instance=" + (fw != null));
            sb.AppendLine("ConfHandler=" + (fw?.ConfHandler != null));
            sb.AppendLine("LangHandler=" + (fw?.LangHandler != null));

            if (fw?.ConfHandler != null)
            {
                foreach (string table in ViewTables)
                {
                    sb.AppendLine();
                    sb.AppendLine("== table " + table + " ==");

                    ConfLoader loader = null;
                    try { loader = fw.ConfHandler.GetCnfLoader(table); }
                    catch (Exception ex) { sb.AppendLine("GetCnfLoader: " + ex.Message); }
                    sb.AppendLine("loader=" + (loader != null));
                    if (loader != null)
                    {
                        try
                        {
                            var caches = loader.GetCaches();
                            sb.AppendLine("cacheCount=" + (caches?.Count ?? -1));
                            if (caches != null)
                            {
                                int n = 0;
                                foreach (var p in caches)
                                {
                                    if (n++ < 5)
                                        sb.AppendLine("  cacheKey=" + p.Key + " type=" + (p.Value?.GetType().Name ?? "null"));
                                }
                            }
                        }
                        catch (Exception ex) { sb.AppendLine("GetCaches: " + ex.Message); }
                    }

                    CnfTableData row = null;
                    try { row = fw.ConfHandler.GetConf(table, sampleId); }
                    catch (Exception ex) { sb.AppendLine("GetConf: " + ex.Message); }
                    sb.AppendLine("row=" + (row != null));
                    if (row?.KV != null)
                    {
                        string extracted = ExtractTipIdFromKv(row.KV);
                        sb.AppendLine("ExtractTipIdFromKv=" + Safe(extracted));
                        try
                        {
                            var datas = row.KV.GetCnfDatas();
                            sb.AppendLine("CnfDatas.Count=" + (datas?.Count ?? -1));
                            if (datas != null)
                            {
                                int n = 0;
                                foreach (var p in datas)
                                {
                                    if (n++ >= 40) break;
                                    sb.AppendLine("  " + p.Key + " = " + p.Value);
                                }
                            }
                        }
                        catch (Exception ex) { sb.AppendLine("GetCnfDatas: " + ex.Message); }

                        if (!string.IsNullOrEmpty(extracted) && fw.LangHandler != null)
                        {
                            try
                            {
                                string loc = LocalizeTip(extracted);
                                sb.AppendLine("LocalizeTip=" + Safe(loc));
                                sb.AppendLine("GetTipsValue=" + Safe(NZUIEditorTools.GetTipsValue(extracted)));
                                sb.AppendLine("GetTipStr=" + Safe(fw.LangHandler.GetTipStr(extracted)));
                            }
                            catch (Exception ex) { sb.AppendLine("localize probe: " + ex.Message); }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            sb.AppendLine("probe fatal: " + ex);
        }

        try
        {
            File.WriteAllText(DebugPath, sb.ToString(), Encoding.UTF8);
            TributeForcerMod.Logger?.Msg("[TributeForcer] 已写探测文件: " + DebugPath);
        }
        catch (Exception ex)
        {
            TributeForcerMod.Logger?.Warning("[TributeForcer] 写探测文件失败: " + ex.Message);
        }
    }

    private static string Safe(string s)
    {
        if (s == null) return "<null>";
        if (s.Length == 0) return "<empty>";
        return s.Replace("\r", "\\r").Replace("\n", "\\n");
    }

    private static string EscapeValue(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\n", "\\n").Replace("\r", "\\r");
    }

    private static string UnescapeValue(string value)
    {
        if (string.IsNullOrEmpty(value) || value.IndexOf('\\') < 0)
            return value;
        var sb = new StringBuilder(value.Length);
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (c == '\\' && i + 1 < value.Length)
            {
                char n = value[++i];
                switch (n)
                {
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case '\\': sb.Append('\\'); break;
                    default: sb.Append(n); break;
                }
            }
            else sb.Append(c);
        }
        return sb.ToString();
    }
}

/// <summary>
/// 游戏详情界面取名链路：FetchStrValue("TributeName") 得到 tipId，再 GetTipStr。
/// 在 FetchStrValue 出口抓 tipId 并本地化，作为补充捕获。
/// </summary>
[HarmonyPatch(typeof(CommonObj), nameof(CommonObj.FetchStrValue), new Type[] { typeof(string) })]
internal static class TributeNameFetchStrPatch
{
    static void Postfix(CommonObj __instance, string calKey, string __result)
    {
        try
        {
            if (__instance == null || string.IsNullOrEmpty(__result))
                return;
            if (!string.Equals(calKey, "TributeName", StringComparison.Ordinal))
                return;

            string cnfId = null;
            try { cnfId = __instance.GetCnfID(); } catch { /* ignore */ }
            if (string.IsNullOrEmpty(cnfId))
            {
                try { cnfId = __instance.GetMainCnf(); } catch { /* ignore */ }
            }
            if (string.IsNullOrEmpty(cnfId) || !cnfId.StartsWith("Tribute_", StringComparison.Ordinal))
                return;

            TributeNameResolver.RegisterFromTipId(cnfId, __result);
        }
        catch (Exception ex)
        {
            TributeForcerMod.Logger?.Error("[TributeForcer] FetchStr name capture err: " + ex.Message);
        }
    }
}

[HarmonyPatch(typeof(NZUIHandle), "GetTunnelStrData")]
internal static class TributeNameCapturePatch
{
    static void Postfix(
        NZUIHandle __instance,
        CommonObj commonObj,
        string tableName,
        string tunnelName,
        ref string value,
        bool __result)
    {
        try
        {
            if (!__result || commonObj == null || string.IsNullOrEmpty(value))
                return;

            bool nameTunnel =
                (!string.IsNullOrEmpty(tunnelName) &&
                 tunnelName.IndexOf("TributeName", StringComparison.OrdinalIgnoreCase) >= 0) ||
                (!string.IsNullOrEmpty(tableName) &&
                 tableName.IndexOf("TributeName", StringComparison.OrdinalIgnoreCase) >= 0);
            if (!nameTunnel)
                return;

            string cnfId = commonObj.GetCnfID();
            if (string.IsNullOrEmpty(cnfId))
                cnfId = commonObj.GetMainCnf();
            if (string.IsNullOrEmpty(cnfId) || !cnfId.StartsWith("Tribute_", StringComparison.Ordinal))
                return;

            // tunnel 值可能是 tipId，也可能已是中文
            if (ContainsCjkFast(value))
                TributeNameResolver.Register(cnfId, value, persist: true);
            else
                TributeNameResolver.RegisterFromTipId(cnfId, value);
        }
        catch (Exception ex)
        {
            TributeForcerMod.Logger?.Error("[TributeForcer] tunnel name capture err: " + ex.Message);
        }
    }

    private static bool ContainsCjkFast(string value)
    {
        foreach (char c in value)
        {
            if (c >= 0x4E00 && c <= 0x9FFF) return true;
        }
        return false;
    }
}
