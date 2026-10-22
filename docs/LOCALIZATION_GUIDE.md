# TextToTalkCN 汉化改动规范（给协作代理）

本仓库是 `karashiiro/TextToTalk` 的汉化重构分支，目标：**所有玩家可见文本都走资源文件 `Strings`，并补全简体中文**。

## 你只负责一件事

把你被分配到的 `.cs` 文件里**硬编码的英文界面文本**替换成资源引用，并把新增键写进你专属的 JSON 片段。

## 绝对不要做

- 不要修改任何 `.resx`、`Strings.Designer.cs`、其它 `.cs`、`.csproj`、`.sln`。
- 不要重命名类/方法/变量，不要调整逻辑，不要改缩进风格。
- 不要改 `##` 后面的 ImGui ID 部分（`$"...##{MemoizedId.Create()}"` 必须原样保留）。
- 不要翻译日志（`Log.`）、异常类型名、URL、API 参数、语音 ID、JSON 字段名、格式化占位符 `{0}` 本身。

## 替换写法

### 1. 普通文本

```csharp
// 改前
ImGui.TextColored(ImColor.HintColor, "Credentials secured with Windows Credential Manager");
// 改后
ImGui.TextColored(ImColor.HintColor, Strings.PollyCredentialsSecured);
```

### 2. 带 ImGui ID 的控件

```csharp
// 改前
if (ImGui.Button($"Save and Login##{MemoizedId.Create()}"))
// 改后
if (ImGui.Button($"{Strings.PollySaveAndLogin}##{MemoizedId.Create()}"))
```

### 3. 带占位符的文本

资源里写 `"登录失败：{0}"`，代码里：

```csharp
// 改前
ImGui.TextColored(ImColor.Red, $"Failed to login: {loginError}");
// 改后
ImGui.TextColored(ImColor.Red, string.Format(Strings.PollyLoginFailedFormat, loginError));
```

如文件里已有 `localizer.Format(...)` 之类用法，保持一致即可；没有就用 `string.Format`。

### 4. using

文件顶部没有 `using TextToTalk.Resources;` 的话加在已有 using 之后（保持排序大致不变）。

### 5. 键名命名

`<区域前缀><语义>`，PascalCase，例如 Polly 文件用 `PollyRegion`、`PollySaveAndLogin`、`PollyLoginFailedFormat`。
前缀按文件固定：`Polly` / `Azure` / `GoogleCloud` / `ElevenLabs` / `FishAudio` / `OpenAi` / `Uberduck` / `Kokoro` / `System`(→ `SystemTts`，避免与 `System` 命名空间混淆) / `Websocket` / `Megaphone` / `VoiceStyles`。

同一个文件内不要重复键；跨文件由前缀保证不冲突。

### 6. 复用已有键

`docs/existing-string-keys.txt` 里是**已经存在**的键（一行 `键名 = 英文值`）。如果语义完全一致（例如 "Volume"、"Preset"、"Test"、"Delete preset"），**直接复用已有键**，不要新建。
复用前请确认该键确实在 `docs/existing-string-keys.txt` 中。

## 交付物：JSON 片段

写到仓库根目录下的 `loc\<你的单元名>.json`，UTF-8，格式：

```json
{
  "PollyRegion": { "en": "Region", "zh": "区域" },
  "PollyLoginFailedFormat": { "en": "Failed to login: {0}", "zh": "登录失败：{0}" }
}
```

- `en` 是替换前的**原文**（保持占位符）。
- `zh` 是简体中文译文，用 FFXIV 国服习惯用语，简短（按钮 2–6 字），不要出现「您」。
- 只包含你**新建**的键；复用的已有键不要写进去。

## 中文用词参考

小队 / 团队 / 部队 / 通讯贝 / 悄悄话 / 招募 / 副本 / 以太之光 / 雇员 / 投影 / 治疗 / 伤害 / 增益 / 减益 / 吟唱 / 技能 / 物品 / 系统消息 / 喊话 / 呼喊 / 情感动作。

## 完成后回复

列出：改动文件、新建键数量、JSON 片段路径。不要贴大段代码。
