# MoreInfo — 功法数值显示（实时读取）

为《了不起的修仙模拟器》的功法/秘术提示框追加**从游戏数据实时计算**的数值，而不是把数字写死在 XML 里。

## 功能

- 保留原版风味描述，在后面追加数值区（金色小字）
- 数值、属性名、单位、百分比格式全部来自游戏本体
- 覆盖所有带 `Modifier` 的定义：固定功法、DLC、秘籍、法宝、神通等
- 覆盖随机生成功法/秘术：显示该实例的精确值（不再是“少量/较多/大量/巨量”）
- 兼容语言包（官方英文属性名直接生效）与后续游戏更新、DLC
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

- `XiaWorld.EsotericaDef.GetEffectDesc`：按 `Modifier` 名取 `ModifierDef`，调用游戏自带的 `ModifierDef.GetSimpleDesc()`，把属性行追加到原描述后
- `XiaWorld.EsotericaTemplateData.GetEffectDesc`：随机生成功法单元，用 `EsotericaData.Value` + `IsAddv` 调用 `PropertyMgr.GetPropertyAddtionDesc()` 生成精确数值行

未重新实现属性计算逻辑，也没有复制游戏代码；全部通过公开 API/反射在运行时读取。
“本角色实际增量”**未实现**：游戏的 `GetPropertyAddtionDesc` 对 `*AddP` 属性直接返回原始 AddP、对纯 AddV 忽略角色状态，无法作为可靠的“实际增量”；真正的最终属性差需要复刻整套属性计算，存在误导风险，因此只显示定义值。

## 许可

MIT，见 [LICENSE](LICENSE)。第三方组件见 [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)。
