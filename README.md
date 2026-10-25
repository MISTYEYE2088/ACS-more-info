# MoreInfo — 功法数值显示（实时读取）

为《了不起的修仙模拟器》的功法/秘术提示框追加**从游戏数据实时计算**的数值，而不是把数字写死在 XML 里。

## 功能

- 保留原版风味描述，在后面追加数值区（金色小字）
- 数值、属性名、单位、语言全部来自游戏本体（倍率 0.1 = +10%）
- **四项数值分别标注**，不再只看得懂原始键名：

| 标签 | 原始字段 | 含义 |
|---|---|---|
| 基础加值 | `BAddV` | 加到基础值上的固定值 |
| 基础倍率 | `BAddP` | 只作用于基础值的倍率 |
| 总值加值 | `AddV` | 加到最终结果上的固定值 |
| 总值倍率 | `AddP` | 作用于基础合计的倍率 |

示例：

- `灵气最大值（总值倍率）+25倍`（`AddP=25`，即 +2500%）
- `灵气最大值（基础倍率）-0.1倍`（`BAddP=-0.1`）
- `寿元（基础加值）+100年`（`BAddV=100`）
- `灵气最大值（总值加值）+40.00`（`AddV=40`）

- 标签按游戏语言自动切换：中文 `基础/总值/加值/倍率`，英文 `base/total/addition/multiplier`，属性名与单位也用游戏本地化文本
- 倍率统一显示为 `+x倍` / `+xx`，不再出现长百分比或错误的 `+0.00%`
- 覆盖所有带 `Modifier` 的定义：固定功法、DLC、秘籍、法宝、神通等，不针对某个属性写死
- 覆盖随机生成功法/秘术：显示该实例的精确值（不再是“少量/较多/大量/巨量”）
- 与旧版“功法数值显示”同时启用时自动跳过旧 mod 的 `AddP=/AddV=` 文本，避免重复

## 依赖

- [ModLoaderLite](https://steamcommunity.com/sharedfiles/filedetails/?id=1997170546)（提供 Harmony 运行时）

## 安装

1. 把 `MoreInfo` 文件夹放进游戏目录的 `Mods` 下
2. 在游戏内模组管理器启用 `功法数值显示 MoreInfo` 和 `ModLoaderLite`
3. 不要同时启用旧版“功法数值显示”

## 构建

需要 .NET SDK 8（或更新）：

```powershell
pwsh -File scripts/build.ps1
```

产物在 `dist/MoreInfo` 与 `dist/MoreInfo.zip`。部署到游戏副本：

```powershell
pwsh -File scripts/build.ps1 -Deploy "C:\ACS\game\Mods"
```

回归测试（用模拟的游戏 API 验证补丁逻辑）：

```powershell
dotnet run --project tests/MoreInfo.Tests/MoreInfo.Tests.csproj -c Release
```

GitHub Actions 会自动跑测试、构建并上传 artifact；打 `v*` 标签时附带 Release 资产。CI 不需要游戏文件。

## 发布到 Steam 创意工坊

游戏的创意工坊上传是**游戏内置**的，不能由 GitHub Actions 直接完成：

1. 把构建好的 `MoreInfo` 文件夹放进 `Mods`
2. 进游戏 → 模组管理器 → 找到自制 MOD → 上传
3. 首次上传后记下工坊 ID，后续更新用同一账号在游戏内更新

（SteamCMD 不支持该游戏的上传流程；CI 只负责构建与 GitHub Release。）

## 实现说明

补丁只有两个，均通过 Harmony（由 ModLoaderLite 加载）挂载：

- `XiaWorld.EsotericaDef.GetEffectDesc`：按 `Modifier` 名取 `ModifierDef`，逐项读取 `Properties` 的四个原始值，自己格式化后追加到原描述后；`BaseFive` / `Skills` 仍沿用游戏自带的 `GetSimpleDesc()` 文本
- `XiaWorld.EsotericaTemplateData.GetEffectDesc`：随机生成功法单元，用 `EsotericaData.Value` + `IsAddv` 生成精确数值行

公式（与游戏一致）：

```
最终值 = (基础值 * (1 + BAddP) + BAddV) * (1 + AddP) + AddV
```

因此：

- `BAddP` 只作用于基础值，标为“基础倍率”
- `BAddV` 加到基础值上，标为“基础加值”
- `AddP` 作用于基础合计，标为“总值倍率”
- `AddV` 加到最终结果上，标为“总值加值”
- 倍率统一显示为 `+x倍`（`0.1倍 = +10%`），不会折算成依赖基础值的绝对值
- 加值使用属性自带的显示格式（`{0:N2}`、`{0}年` 等）
- 标签用 `基础/总值` + `加值/倍率` 两段拼接；英文用 `base/total` + `addition/multiplier`，不针对单个属性写死文案
- 语言判断用游戏内置的 `TFMgr.Instance.Language`：**`cn` → 中文，其他（英文 / 本地化 mod）→ 英文**；读不到时再读游戏根目录 `language.txt`，仍读不到则默认英文（不再看系统区域）
- 属性名、单位来自游戏本地化数据，本地化 mod 改过的名字会直接生效
- 游戏没有为这四个字段提供现成标签，所以标签词根是 mod 仅有的新增文案（每种语言 4 个短词），不是逐条复制

没有复制游戏代码，也没有重新实现属性计算；全部通过公开 API/反射在运行时读取。
“本角色实际增量”**未实现**：游戏的 `GetPropertyAddtionDesc` 对 `*AddP` 属性直接返回原始 AddP、对纯 AddV 忽略角色状态，且 `BAddP/BAddV` 单独存在时会算出 `0`，不能作为可靠的“实际增量”；只显示定义值。

## 许可

MIT，见 [LICENSE](LICENSE)。第三方组件见 [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)。
