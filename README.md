# RIP ModSource

[REST IN PEACE](https://store.steampowered.com/)（休息吧）的 **MelonLoader** 模组源码仓库。  
编译产物放到游戏根目录的 `Mods/`，配置一般在 `UserData/`。

---

## 模组一览

| 模组 | 说明 |
|------|------|
| **TributeForcer** | 贡品刷新时用可搜索中文列表最多指定 3 个，强制下次刷新刷出它们 |
| **QualityBoostMod** | 贡品品质/权重、属性倍率、现金获取等数值向调整（见 `QualityBoost.cfg`） |
| **RIPGameplayTweaks** | 局内体验小改；当前主要是 **F 键全图吸取** |
| **RIPOracleYinlu** | 甲骨/阴炉相关：融合与 **G 键级联合成** 等 |

各模组可独立启用，互不强制依赖。

---

## 环境

- 游戏：REST IN PEACE + MelonLoader（net6）
- 构建：.NET 6 SDK
- 引用：本机游戏目录下的 `MelonLoader/`、`Il2CppAssemblies/`（各 `.csproj` 内相对路径）

---

## 编译与安装

在仓库根目录（`ModSource`）下按项目编译，例如：

```powershell
dotnet build TributeForcer\TributeForcer.csproj -c Release
Copy-Item TributeForcer\bin\Release\net6.0\TributeForcer.dll ..\Mods\ -Force
```

其它子项目同理：

```text
RIPGameplayTweaks\ → Mods\RIPGameplayTweaks.dll
RIPOracleYinlu\    → Mods\RIPOracleYinlu.dll
QualityBoostMod.csproj（仓库根）→ Mods\QualityBoostMod.dll
```

---

## TributeForcer 速查

- 默认热键 **F7** 开关面板（`UserData/TributeForcer.cfg` 可改 `hotkey=`）
- 打开时 **ESC** 或右上角「关闭」可关（搜索框聚焦时也有效）
- 勾选最多 3 个 →「应用到下次刷新」→ 游戏内正常刷新

---

## 配置文件（游戏 `UserData/`）

| 文件 | 对应模组 |
|------|----------|
| `TributeForcer.cfg` | TributeForcer |
| `QualityBoost.cfg` | QualityBoostMod |
| `RIPGameplayTweaks.cfg` | RIPGameplayTweaks |
| `RIPOracleYinlu.cfg` | RIPOracleYinlu |

---

## 目录结构（简）

```text
ModSource/
  README.md                 ← 本文件
  QualityBoostMod.cs        # 品质/倍率等
  RIPGameplayTweaks/        # F 键吸取等
  RIPOracleYinlu/           # 甲骨阴炉
  TributeForcer/            # 强制指定贡品
  *Tests/                   # 单元测试
```

---

## 说明

- 仅供单机/私人娱乐，联机与反作弊环境请自行评估风险。
- 游戏更新可能导致 Il2Cpp 接口变化，需重新适配。
