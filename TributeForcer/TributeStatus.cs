using System;
using System.Collections.Generic;
using Il2Cpp;
using Il2CppBattle;
using Il2CppGameConfig;
using Il2CppInterop.Runtime;
using Il2CppRushTribute;
using Il2CppRushUser;
using Il2CppServiceSysBase;
using Il2CppSysCommon;

/// <summary>
/// 贡品可选状态：图鉴解锁（DropSys 锁定集）、持有数量 / MAXTriCount。
/// 持有数来自 HostSys → TributeBase 的 TributeObj.CurTriCount（同 ID 通常只有一条，数量在 int 字段里）。
/// </summary>
public static class TributeStatus
{
    private const string TributeSrvType = "TributeBase";
    private static readonly string[] ViewTables = { "TributeView", "TributeView_DLC01" };
    private static bool _loggedOnce;

    public static void Fill(List<TributeCatalogEntry> entries)
    {
        if (entries == null || entries.Count == 0)
            return;

        HashSet<string> locked = SnapshotLockSet();
        Dictionary<string, int> owned = SnapshotOwnedCounts();
        Dictionary<string, int> maxMap = SnapshotMaxCounts(entries);

        // 按 configId 精确补全：GetTributeObjByConfigId + 运行时 MAX/Cur
        EnrichFromRuntimeObjects(entries, owned, maxMap);

        int ownedKinds = owned.Count;
        int ownedTotal = 0;
        foreach (var kv in owned)
            ownedTotal += kv.Value;

        foreach (var e in entries)
        {
            int max = 0;
            if (maxMap.TryGetValue(e.Id, out int m))
                max = m;
            e.MaxCount = max;

            int own = 0;
            if (owned.TryGetValue(e.Id, out int o))
                own = o;
            e.OwnedCount = own;

            e.IsLocked = locked != null && locked.Contains(e.Id);
            // own>=max 即满；强制叠到超过 max 时也算满（仍显示真实 own/max）
            e.IsFull = max > 0 && own >= max;
            e.CanSelect = !e.IsLocked && !e.IsFull;
            e.StatusLabel = BuildLabel(e);
        }

        if (!_loggedOnce)
        {
            _loggedOnce = true;
            TributeForcerMod.Logger?.Msg(
                $"[TributeForcer] 状态统计: 锁定={locked?.Count ?? 0}, 持有种类={ownedKinds}, 持有总数={ownedTotal}");
            // 打印持有明细，方便核对角标数量（如火灵珠 x2）
            try
            {
                var parts = new List<string>();
                foreach (var e in entries)
                {
                    if (e == null || e.OwnedCount <= 0)
                        continue;
                    string n = string.IsNullOrEmpty(e.ChineseName) ? e.Id : e.ChineseName;
                    parts.Add($"{n}={e.OwnedCount}/{e.MaxCount}");
                }
                if (parts.Count > 0)
                    TributeForcerMod.Logger?.Msg("[TributeForcer] 持有明细: " + string.Join(", ", parts));
            }
            catch { /* ignore */ }
        }
    }

    private static string BuildLabel(TributeCatalogEntry e)
    {
        if (e.IsLocked)
            return "未解锁";

        // 已持有：永远显示真实数量，绝不用「唯一」掩盖（持有 2 时「唯一已满」是错的）
        if (e.OwnedCount > 0)
        {
            if (e.MaxCount > 0)
            {
                if (e.OwnedCount >= e.MaxCount)
                    return $"已满{e.OwnedCount}/{e.MaxCount}";
                return $"持有{e.OwnedCount}/{e.MaxCount}";
            }
            return $"持有{e.OwnedCount}";
        }

        // 未持有：上限提示
        if (e.MaxCount == 1)
            return "唯一";
        if (e.MaxCount > 1)
            return $"上限{e.MaxCount}";
        return "可选";
    }

    private static HashSet<string> SnapshotLockSet()
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        try
        {
            var set = DropSys.CurTributeLockSet;
            if (set == null)
                return result;

            foreach (string id in set)
            {
                if (!string.IsNullOrEmpty(id))
                    result.Add(id);
            }
        }
        catch (Exception ex)
        {
            TributeForcerMod.Logger?.Warning("[TributeForcer] 读取锁定集失败: " + ex.Message);
        }
        return result;
    }

    private static Dictionary<string, int> SnapshotOwnedCounts()
    {
        var result = new Dictionary<string, int>(StringComparer.Ordinal);
        try
        {
            TributeBaseSys sys = FindTributeBaseSys();
            if (sys == null)
            {
                TributeForcerMod.Logger?.Warning("[TributeForcer] 未找到 TributeBaseSys，持有数量不可用");
                return result;
            }

            // 1) 列表优先：GetTributeList
            try
            {
                var list = sys.GetTributeList();
                if (list != null)
                {
                    foreach (var obj in list)
                        AccumulateOwned(result, obj);
                }
            }
            catch { /* ignore */ }

            // 2) 字典兜底：uuid -> CommonObj（同 ID 应叠加 CurTriCount，而非 +1）
            try
            {
                var dict = sys.TributeDict;
                if (dict != null)
                {
                    foreach (var pair in dict)
                        AccumulateOwned(result, pair.Value);
                }
            }
            catch { /* ignore */ }

            // 3) Objs 再兜底
            try
            {
                var objs = sys.Objs;
                if (objs != null)
                {
                    foreach (var pair in objs)
                        AccumulateOwned(result, pair.Value);
                }
            }
            catch { /* ignore */ }
        }
        catch (Exception ex)
        {
            TributeForcerMod.Logger?.Warning("[TributeForcer] 读取持有贡品失败: " + ex.Message);
        }
        return result;
    }

    private static void AccumulateOwned(Dictionary<string, int> result, CommonObj obj)
    {
        if (obj == null)
            return;

        string id = ReadConfigId(obj);
        if (string.IsNullOrEmpty(id))
            return;

        // 事件模具等非贡品对象跳过
        if (id.IndexOf("Mould", StringComparison.OrdinalIgnoreCase) >= 0)
            return;

        int count = ReadCurTriCount(obj);
        if (count <= 0)
            count = 1; // 对象在列表里至少算 1

        // 同 ID 取较大值（dict/list 可能重复扫到同一条）
        if (result.TryGetValue(id, out int prev))
        {
            if (count > prev)
                result[id] = count;
        }
        else
        {
            result[id] = count;
        }
    }

    private static string ReadConfigId(CommonObj obj)
    {
        if (obj == null)
            return null;
        try
        {
            string id = obj.GetCnfID();
            if (!string.IsNullOrEmpty(id))
                return id;
        }
        catch { /* ignore */ }
        try
        {
            string id = obj.GetMainCnf();
            if (!string.IsNullOrEmpty(id))
                return id;
        }
        catch { /* ignore */ }
        return null;
    }

    /// <summary>
    /// 取多个计数字段中的最大值。背包角标可能来自 CurTriCount / CurTriCount2 / SpecialCountDispInt。
    /// </summary>
    private static int ReadCurTriCount(CommonObj obj)
    {
        if (obj == null)
            return 0;

        int best = 0;
        best = Math.Max(best, TryInt(obj, "CurTriCount"));
        best = Math.Max(best, TryInt(obj, "CurTriCount2"));
        best = Math.Max(best, TryInt(obj, "SpecialCountDispInt"));
        best = Math.Max(best, TryInt(obj, "TributeCount"));
        return best;
    }

    private static int TryInt(CommonObj obj, string key)
    {
        if (obj == null || string.IsNullOrEmpty(key))
            return 0;
        try
        {
            // 显式 default=0：FetchIntValue 默认值是 1，会把“无字段”误判成 1
            int v = obj.FetchIntValue(key, 0);
            if (v > 0)
                return v;
        }
        catch { /* ignore */ }
        try
        {
            if (obj.TryFetchIntValue(key, out int v) && v > 0)
                return v;
        }
        catch { /* ignore */ }
        return 0;
    }

    private static int ReadMaxFromObj(CommonObj obj)
    {
        if (obj == null)
            return 0;
        try
        {
            int v = obj.FetchIntValue("MAXTriCount", 0);
            if (v > 0)
                return v;
        }
        catch { /* ignore */ }
        return 0;
    }

    /// <summary>
    /// 对目录中每个 ID 走 GetTributeObjByConfigId，补全持有数与运行时上限。
    /// </summary>
    private static void EnrichFromRuntimeObjects(
        List<TributeCatalogEntry> entries,
        Dictionary<string, int> owned,
        Dictionary<string, int> maxMap)
    {
        try
        {
            TributeBaseSys sys = FindTributeBaseSys();
            if (sys == null || entries == null)
                return;

            foreach (var e in entries)
            {
                if (e == null || string.IsNullOrEmpty(e.Id))
                    continue;

                // ContainsCnfID 快速判断
                bool has = false;
                try { has = sys.ContainsCnfID(e.Id); }
                catch { has = false; }

                TributeObj tobj = null;
                try { tobj = sys.GetTributeObjByConfigId(e.Id); }
                catch { tobj = null; }

                if (tobj == null && !has)
                    continue;

                if (tobj != null)
                {
                    int cur = ReadCurTriCount(tobj);
                    if (cur <= 0 && has)
                        cur = 1;
                    if (cur > 0)
                    {
                        if (!owned.TryGetValue(e.Id, out int prev) || cur > prev)
                            owned[e.Id] = cur;
                    }

                    int runtimeMax = ReadMaxFromObj(tobj);
                    if (runtimeMax > 0)
                    {
                        if (!maxMap.TryGetValue(e.Id, out int cfg) || runtimeMax > cfg)
                            maxMap[e.Id] = runtimeMax;
                    }
                }
                else if (has)
                {
                    // 有 ID 但取不到对象时至少记 1
                    if (!owned.ContainsKey(e.Id))
                        owned[e.Id] = 1;
                }
            }
        }
        catch { /* ignore */ }
    }

    private static Dictionary<string, int> SnapshotMaxCounts(List<TributeCatalogEntry> entries)
    {
        var result = new Dictionary<string, int>(StringComparer.Ordinal);
        try
        {
            ConfLogic conf = BaseFramework.Instance?.ConfHandler;
            if (conf == null)
                return result;

            foreach (var e in entries)
            {
                if (e == null || string.IsNullOrEmpty(e.Id))
                    continue;
                int max = ReadMaxTriCount(conf, e.Id);
                if (max > 0)
                    result[e.Id] = max;
            }
        }
        catch (Exception ex)
        {
            TributeForcerMod.Logger?.Warning("[TributeForcer] 读取 MAXTriCount 失败: " + ex.Message);
        }
        return result;
    }

    private static int ReadMaxTriCount(ConfLogic conf, string tributeId)
    {
        foreach (string table in ViewTables)
        {
            CnfTableData row = null;
            try { row = conf.GetConf(table, tributeId); }
            catch { row = null; }
            if (row?.KV == null)
                continue;

            int max = ExtractIntSlot(row.KV, "MAXTriCount");
            if (max > 0)
                return max;
        }
        return 0;
    }

    /// <summary>
    /// Int 槽：IntN = MAXTriCount, IntValueN = 数值
    /// </summary>
    private static int ExtractIntSlot(CnfKV kv, string fieldName)
    {
        if (kv == null || string.IsNullOrEmpty(fieldName))
            return 0;

        for (int i = 1; i <= 32; i++)
        {
            string field = null;
            try { field = kv.GetValue("Int" + i); } catch { field = null; }
            if (string.IsNullOrEmpty(field))
                continue;
            if (!string.Equals(field.Trim(), fieldName, StringComparison.OrdinalIgnoreCase))
                continue;

            string raw = null;
            try { raw = kv.GetValue("IntValue" + i); } catch { raw = null; }
            if (string.IsNullOrEmpty(raw))
                return 0;
            if (int.TryParse(raw.Trim(), out int v))
                return v;
        }
        return 0;
    }

    /// <summary>
    /// 正确入口：HostSys.GetSrvSys("TributeBase")。
    /// ViewUserBattleUnit 不是 Unity Object，FindObjectsOfType 永远拿不到。
    /// </summary>
    private static TributeBaseSys FindTributeBaseSys()
    {
        // 1) SrvUtil.GetHostSys()
        try
        {
            HostSys host = SrvUtil.GetHostSys();
            TributeBaseSys sys = FromHost(host);
            if (sys != null)
                return sys;
        }
        catch { /* ignore */ }

        // 2) RushGameFramework.HostUser
        try
        {
            BaseFramework fw = BaseFramework.Instance;
            if (fw != null)
            {
                var rush = fw.TryCast<RushGameFramework>();
                if (rush != null)
                {
                    TributeBaseSys sys = FromHost(rush.HostUser);
                    if (sys != null)
                        return sys;
                }
            }
        }
        catch { /* ignore */ }

        // 3) 旧路径兜底（通常无效，但保留）
        try
        {
            var arr = UnityEngine.Object.FindObjectsOfType(Il2CppType.Of<ViewUserBattleUnit>());
            if (arr != null)
            {
                for (int i = 0; i < arr.Length; i++)
                {
                    var u = arr[i].TryCast<ViewUserBattleUnit>();
                    if (u == null)
                        continue;
                    try
                    {
                        TributeBaseSys sys = u.TributeSys;
                        if (sys != null)
                            return sys;
                    }
                    catch { /* next */ }
                }
            }
        }
        catch { /* ignore */ }

        return null;
    }

    private static TributeBaseSys FromHost(HostSys host)
    {
        if (host == null)
            return null;

        try
        {
            SrvBaseSys srv = host.GetSrvSys(TributeSrvType);
            if (srv != null)
            {
                var casted = srv.TryCast<TributeBaseSys>();
                if (casted != null)
                    return casted;
                // 有的 interop 版本已是派生类型
                if (srv is TributeBaseSys direct)
                    return direct;
            }
        }
        catch { /* ignore */ }

        try
        {
            if (host.TryGetSrvSys(TributeSrvType, out TributeBaseSys typed) && typed != null)
                return typed;
        }
        catch { /* ignore */ }

        return null;
    }
}
