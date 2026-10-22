<#
    合并 loc/*.json 键片段到资源文件与 Strings.Designer.cs。

    用法：
        pwsh -NoProfile -File scripts\merge-loc.ps1

    行为：
      * 读取 loc/ 下所有 *.json（每个文件形如 { "Key": { "en": "...", "zh": "..." } }）；
      * 只把【资源里还不存在】的键追加到 Strings.resx（英文回退）与 Strings.zh-Hans.resx（中文）；
      * 为每个新键在 Strings.Designer.cs 里补一个强类型属性；
      * 幂等：重复执行不会重复写入。
#>
param(
    [string]$Root = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'

$resxEn = Join-Path $Root 'src\TextToTalk\Resources\Strings.resx'
$resxZh = Join-Path $Root 'src\TextToTalk\Resources\Strings.zh-Hans.resx'
$designer = Join-Path $Root 'src\TextToTalk\Resources\Strings.Designer.cs'
$locDir = Join-Path $Root 'loc'

function ConvertTo-XmlText([string]$s) {
    if ($null -eq $s) { return '' }
    return $s.Replace('&', '&amp;').Replace('<', '&lt;').Replace('>', '&gt;')
}

function Get-ExistingNames([string]$path) {
    $text = [System.IO.File]::ReadAllText($path)
    $names = New-Object 'System.Collections.Generic.HashSet[string]'
    foreach ($m in [regex]::Matches($text, '<data name="([^"]+)"')) {
        [void]$names.Add($m.Groups[1].Value)
    }
    return , $names
}

# --- 收集片段 ---------------------------------------------------------------
$entries = [ordered]@{}
$conflicts = @()

foreach ($file in (Get-ChildItem $locDir -Filter *.json -File | Sort-Object Name)) {
    $json = [System.IO.File]::ReadAllText($file.FullName) | ConvertFrom-Json
    foreach ($prop in $json.PSObject.Properties) {
        $key = $prop.Name
        $en = $prop.Value.en
        $zh = $prop.Value.zh
        if ([string]::IsNullOrWhiteSpace($en) -or [string]::IsNullOrWhiteSpace($zh)) {
            Write-Warning "跳过空值键：$($file.Name) / $key"
            continue
        }
        if ($entries.Contains($key)) {
            if ($entries[$key].zh -ne $zh) { $conflicts += $key }
            continue
        }

        # 统一通用标签的译法，避免各后端各译一套
        switch -Regex ($en) {
            '^Test$' { $zh = '试听' }
            '^Use gendered' { $zh = '使用分性别语音预设' }
            '^Delete preset$' { $zh = '删除预设' }
            '^Preset name$' { $zh = '预设名称' }
        }

        # 个别键的译法覆盖
        $overrides = @{
            'UberduckApiSecretHint'    = 'API 私有密钥'
            'OpenAiStylesRemoveStyle'  = '移除风格'
            'PollySecretKey'           = '私有访问密钥'
        }
        if ($overrides.ContainsKey($key)) { $zh = $overrides[$key] }

        $entries[$key] = @{ en = $en; zh = $zh; from = $file.Name }
    }
}

Write-Host ("片段键总数：{0}（来自 {1} 个文件）" -f $entries.Count, (Get-ChildItem $locDir -Filter *.json -File).Count)
if ($conflicts.Count -gt 0) {
    Write-Warning ("以下键在多个片段中译文不一致（取首次出现）：{0}" -f ($conflicts -join ', '))
}

# --- 写入 resx --------------------------------------------------------------
$existingEn = Get-ExistingNames $resxEn
$existingZh = Get-ExistingNames $resxZh

$addEn = [System.Text.StringBuilder]::new()
$addZh = [System.Text.StringBuilder]::new()
$newKeys = @()

foreach ($key in $entries.Keys) {
    $e = $entries[$key]
    if (-not $existingEn.Contains($key)) {
        [void]$addEn.AppendLine(('  <data name="{0}" xml:space="preserve">' -f $key))
        [void]$addEn.AppendLine(('    <value>{0}</value>' -f (ConvertTo-XmlText $e.en)))
        [void]$addEn.AppendLine('  </data>')
    }
    if (-not $existingZh.Contains($key)) {
        [void]$addZh.AppendLine(('  <data name="{0}" xml:space="preserve">' -f $key))
        [void]$addZh.AppendLine(('    <value>{0}</value>' -f (ConvertTo-XmlText $e.zh)))
        [void]$addZh.AppendLine('  </data>')
    }
    if (-not $existingEn.Contains($key) -or -not $existingZh.Contains($key)) {
        $newKeys += $key
    }
}

function Add-BeforeClosingRoot([string]$path, [string]$payload) {
    if ([string]::IsNullOrWhiteSpace($payload)) { return }
    $text = [System.IO.File]::ReadAllText($path)
    $idx = $text.LastIndexOf('</root>')
    if ($idx -lt 0) { throw "找不到 </root>：$path" }
    $text = $text.Substring(0, $idx) + $payload + $text.Substring($idx)
    $utf8Bom = New-Object System.Text.UTF8Encoding($true)
    [System.IO.File]::WriteAllText($path, $text, $utf8Bom)
}

Add-BeforeClosingRoot $resxEn $addEn.ToString()
Add-BeforeClosingRoot $resxZh $addZh.ToString()

# --- 写入 Strings.Designer.cs ----------------------------------------------
$designerText = [System.IO.File]::ReadAllText($designer)
$propLines = @()
foreach ($key in ($newKeys | Sort-Object)) {
    $propLines += ('    internal static string {0} => Get(nameof({0}));' -f $key)
}

if ($propLines.Count -gt 0) {
    $anchor = '    internal static string Get(string key) =>'
    $pos = $designerText.IndexOf($anchor)
    if ($pos -lt 0) { throw 'Strings.Designer.cs 中找不到 Get(string key) 定义' }
    $block = ($propLines -join "`r`n") + "`r`n`r`n"
    $designerText = $designerText.Substring(0, $pos) + $block + $designerText.Substring($pos)
    $utf8Bom = New-Object System.Text.UTF8Encoding($true)
    [System.IO.File]::WriteAllText($designer, $designerText, $utf8Bom)
}

Write-Host ("新增键：{0} 个" -f $newKeys.Count)
if ($newKeys.Count -gt 0) {
    Write-Host (($newKeys | Sort-Object) -join ', ')
}
