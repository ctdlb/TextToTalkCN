# TextToTalk 中文版

《最终幻想 14》聊天与剧情对话朗读插件，基于 [karashiiro/TextToTalk](https://github.com/karashiiro/TextToTalk) 重构并完成简体中文本地化。

插件运行在 [Dalamud](https://github.com/goatcorp/Dalamud) 上，可以把游戏内的聊天、NPC 对话、战斗对话用 TTS（语音合成）念出来，支持触发器与排除项、按玩家/NPC 绑定不同音色、以及十余种语音后端。

- 上游英文版说明保留在 [`docs/README.upstream.en.md`](docs/README.upstream.en.md)。
- 本项目针对**国服 XIVLauncherCN**（配置目录 `%APPDATA%\XIVLauncherCN`）做了构建适配。

## 功能一览

- 朗读聊天、情感动作、系统消息、NPC 与战斗对话（可逐频道开关）
- 触发器 / 排除项（支持正则），可按内容筛选要念什么
- 玩家语音预设、NPC 语音预设、频道预设（可绑定快捷键）
- 多条语音后端的统一界面：**Edge TTS（免密钥，推荐）**、**免费网页语音（百度/谷歌，免密钥）**、系统语音、Megaphone、WebSocket、AWS Polly、Azure、Google Cloud、ElevenLabs、OpenAI、Fish Audio、Kokoro、Uberduck
- 发音词典（Lexicon），可修正游戏专有名词的读音
- WebSocket 接口，可把朗读请求转给外部程序

## 与上游相比的改动

1. **全面汉化**：界面、按钮、提示、错误信息、命令说明、插件描述全部走 `Strings` 资源；`zh-Hans` 为完整翻译，英文资源保留作为回退。
2. **国服构建适配**：新增 `src/Directory.Build.props`，按 `DALAMUD_HOME` → `XIVLauncherCN` → `XIVLauncher` 的顺序自动定位 Dalamud，不再写死国际服路径。
3. **一键构建脚本**：`scripts/build.ps1`，可自动探测 .NET 10 SDK 与 Dalamud，并支持 `-Install` 直接部署到开发插件目录。
4. **汉化工具链**：`docs/LOCALIZATION_GUIDE.md` + `scripts/merge-loc.ps1`，新增中文条目可以按片段合并进 `.resx` 与强类型访问器。
5. 源码、资源与构建脚本统一按 UTF-8（带 BOM）处理，避免 Windows 默认代码页在中文系统下造成乱码。

## 免密钥朗读（Edge TTS）

不想折腾 API Key 的话，在「语音合成设置 → 语音后端」里选 **Edge TTS（免密钥）**：

- 新安装默认就用它，开箱即可朗读；**不需要任何账号或密钥**
- 覆盖 300+ 个语音，默认只显示中文（大陆/台湾/香港/吴语/粤语），可取消勾选看全部
- 支持语速调节、试听（可输入自定义文本）
- 需要联网；接口由微软提供，不保证长期可用
## 命令

| 命令 | 说明 |
| --- | --- |
| `/tttconfig` | 打开设置窗口 |
| `/tttstyles` | 打开语音风格窗口 |
| `/toggletts` | 开关朗读 |
| `/enabletts` / `/disabletts` | 开启 / 关闭朗读 |
| `/canceltts` | 取消所有已排队的朗读 |
| `/tttpreset [名称]` | 切换频道预设；不带参数时列出所有预设 |
| `/tttvolume [值]` | 调整全局音量；支持 `80`、`+10`、`-10` 三种写法 |

## 安装

> 不想自己编译？直接下载打好包的压缩包：[Releases · v1.43.4-cn](https://github.com/ctdlb/TextToTalkCN/releases)

### 方式一：使用已构建好的开发插件

仓库里已附带一份构建产物：`dist\TextToTalkCN\`。

1. 关闭游戏。
2. 把 `dist\TextToTalkCN\` 整个文件夹复制到 `%APPDATA%\XIVLauncherCN\devPlugins\`。
3. 启动游戏，在 `/xlplugins` →「开发插件」中启用 **TextToTalkCN**。
4. 本插件内部名称为 `TextToTalkCN`，可与官方版 TextToTalk 并存；若不想保留官方版，在插件列表里停用即可。

也可以直接用脚本一步完成（构建 + 复制）：

```powershell
powershell -ExecutionPolicy Bypass -File scripts\build.ps1 -Configuration Release -Install
```

## 通过插件仓库安装（推荐分享给别人）

把下面地址加到卫月（Dalamud）的「自定义插件仓库」里，就能在插件安装器里直接搜到、一键安装和更新。

```
https://raw.githubusercontent.com/ctdlb/DalamudPlugins/main/pluginmaster.json
```

如果直连 GitHub 打不开，用系统代理或加速器，不要用 jsDelivr 镜像——它的分支缓存最长 12 小时，新版本会迟迟不出现。

添加方法：`/xlplugins` → 设置 → 自定义插件仓库 → 添加，粘贴地址并保存，回到「全部插件」搜索 `TextToTalkCN`。

## 从源码构建

环境要求：

- Windows 10/11 x64
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- 已安装并可正常启动的 XIVLauncherCN（需要其中的 Dalamud dev 目录）

```powershell
# Debug 构建
powershell -ExecutionPolicy Bypass -File scripts\build.ps1

# Release 构建并安装到 devPlugins
powershell -ExecutionPolicy Bypass -File scripts\build.ps1 -Configuration Release -Install
```

脚本会依次尝试 `DALAMUD_HOME`、`%APPDATA%\XIVLauncherCN\addon\Hooks\dev`、`%APPDATA%\XIVLauncher\addon\Hooks\dev`。

也可以直接用 dotnet：

```powershell
$env:DALAMUD_HOME = "$env:APPDATA\XIVLauncherCN\addon\Hooks\dev"
dotnet build src\TextToTalk\TextToTalk.csproj -c Release
```

产物位于 `src\TextToTalk\bin\<配置>\TextToTalk\`。

## 汉化机制

- 文本资源：`src/TextToTalk/Resources/Strings.resx`（英文，回退）与 `Strings.zh-Hans.resx`（简体中文）。
- 代码里通过 `Strings.键名`（强类型）或 `Strings.Get("键名")` 取文本；缺失时显示 `[键名]`，便于发现漏翻。
- 语言由 Dalamud 的界面语言决定：`zh` → `zh-Hans`，`tw` → `zh-Hant`（见 `Localization/Localizer.cs`）。
- 新增条目流程见 [`docs/LOCALIZATION_GUIDE.md`](docs/LOCALIZATION_GUIDE.md)，键片段放在 `loc/`，执行：

```powershell
powershell -ExecutionPolicy Bypass -File scripts\merge-loc.ps1
```

## 目录结构

```
src/
  TextToTalk/              插件主体（UI、后端、朗读调度）
  TextToTalk.Data/         本地数据库（LiteDB）
  TextToTalk.Lexicons*/    发音词典解析与仓库
  TextToTalk.UI.*/         UI 组件与源生成器
  TextToTalk.UngenderedOverrides/
  VoiceUnlocker/           解锁 Windows 隐藏语音
  websocket-sharp/         WebSocket 实现
docs/                      说明文档、上游英文 README
loc/                       汉化键片段（合并前）
scripts/                   build.ps1 / merge-loc.ps1
```

## WebSocket 接口

插件可开启 WebSocket 服务器向外推送朗读消息，包含 `Say`、`Cancel`、`Event` 三种 JSON 消息。字段说明见上游文档 [`docs/README.upstream.en.md`](docs/README.upstream.en.md) 的「WebSocket interfacing」一节，或直接看 `src/TextToTalk/Backends/Websocket/IpcMessage.cs`。

## 常见问题

**界面还是英文？**
确认 Dalamud 界面语言是「简体中文」。资源按 `zh-Hans` 加载；若是繁中客户端会走 `zh-Hant`（上游翻译，未在本项目维护）。

**提示缺少 Dalamud.dll / 构建失败？**
先启动一次 XIVLauncherCN 让 Dalamud 完成安装，或手动设置 `DALAMUD_HOME` 指向 `...\addon\Hooks\dev`。

**插件列表里有两个 TextToTalk？**
本插件内部名为 `TextToTalkCN`，与官方 `TextToTalk` 不相冲突，两者可以同时安装。

## 许可

MIT License，原作者 karashiiro。本项目为其汉化重构分支，遵循同一许可；详见 [LICENSE](LICENSE)。
