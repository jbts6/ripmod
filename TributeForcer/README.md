# TributeForcer — 刷新指定贡品

MelonLoader 模组：在贡品刷新界面**强制指定下次刷出的贡品**（最多 3 个），支持中文搜索、图鉴解锁/持有上限状态显示。

适用游戏：**REST IN PEACE**（休息吧 / RIP）

当前版本：**1.2.5**

---

## 功能

- **F7**（可配置）打开/关闭选择面板
- 从当前关卡掉落池列出全部贡品，**中文名**可搜索（不依赖「已经刷出来过」）
- 勾选最多 **3** 个，点「应用到下次刷新」后，下次刷新必出这些贡品
- 状态标签：未解锁 / 持有数 / 上限 / 已满（按配置 `MAXTriCount` + 运行时 `CurTriCount`）
- 列表按：可选 → 已满 → 未解锁，同组内稀有度、中文名排序
- 行颜色表示稀有度；未解锁/已满降低透明度
- **ESC** 或右上角「关闭」可关面板（搜索框聚焦时同样有效）

---

## 安装

1. 已安装 [MelonLoader](https://github.com/LavaGang/MelonLoader)（与本游戏其它 Mods 相同）
2. 编译或使用现成 `TributeForcer.dll`，放到游戏目录：

   ```text
   <游戏根目录>/Mods/TributeForcer.dll
   ```

3. 启动游戏，日志中应出现 `TributeForcer v1.2.x 已启用`

### 从源码编译

```powershell
cd ModSource\TributeForcer
dotnet build -c Release
Copy-Item bin\Release\net6.0\TributeForcer.dll ..\..\Mods\TributeForcer.dll -Force
```

---

## 使用

1. 进入可刷新贡品的局内界面
2. 按 **F7** 打开面板
3. 搜索中文名 / ID / 稀有度 / 状态词（如「未解锁」「已满」）
4. 勾选最多 3 个 → **应用到下次刷新**
5. 在游戏里正常点刷新，应刷出指定贡品

| 操作 | 说明 |
|------|------|
| 粘贴 | 向搜索框粘贴剪贴板（中文输入法不顺时可用） |
| 清空搜索 / 清空选择 | 分开清空 |
| ESC / 关闭 | 关闭面板 |
| 配置热键 | 见下方配置 |

---

## 配置

路径：`UserData/TributeForcer.cfg`（首次运行自动生成）

```ini
# 1/true 启用, 0/false 禁用
enabled=1

# 开关界面快捷键（Unity KeyCode 名）
hotkey=F7
```

`hotkey` 示例：`F6`、`F8`、`Alpha0` 等。  
面板打开时 **ESC 始终可关闭**，与 `hotkey` 无关。

---

## 其它数据文件

| 文件 | 说明 |
|------|------|
| `UserData/TributeNames.cache` | 中文名缓存（可手改补充） |
| `UserData/TributeNameDebug.txt` | 名称解析探测（调试用） |
| `UserData/TributeDump.txt` | 当前仓库权重 dump（调试用） |

---

## 原理简述

- Harmony 在 `UserLevelUnit` 刷新前改写当前仓库 `DefaultWeightDict`，把勾选 ID 的权重拉高、其余压低
- 中文名：读 `TributeView` / `TributeView_DLC01` 的 `TributeName` tip，经 `LangLogic` 本地化
- 解锁：`DropSys.CurTributeLockSet`
- 持有：`SrvUtil.GetHostSys()` → `GetSrvSys("TributeBase")` → `CurTriCount` 等

---

## 限制与注意

- 未图鉴解锁的贡品仍可能无法正常获得（游戏侧锁定）；面板会标「未解锁」
- 已达持有上限的会标「已满」；强制模组仍可能叠层，以游戏实际为准
- 仅影响**下一次**刷新；应用后强制列表会清空，需重新选择
- 纯客户端展示/权重修改，联机或反作弊环境请自行评估风险

---

## 快捷键与界面

- 默认 **F7**：关闭时打开；打开时关闭
- **ESC** / **关闭**按钮：仅关闭
- 搜索框聚焦时也可 ESC/热键关闭（在 TextField 处理前拦截 IMGUI 事件）
