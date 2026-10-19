<#
    检查代码里引用、但资源文件里缺失的字符串键。

    用法：
        pwsh -NoProfile -File scripts\check-loc.ps1

    退出码：0 = 全部命中；1 = 有缺失键（会列出来）。
#>
param(
    [string]$Root = 'E:\DeepSeek Harness\TextToTalkCN'
)

$ErrorActionPreference = 'Stop'

$resxEn = Join-Path $Root 'src\TextToTalk\Resources\Strings.resx'
$resxZh = Join-Path $Root 'src\TextToTalk\Resources\Strings.zh-Hans.resx'
$srcDir = Join-Path $Root 'src\TextToTalk'

function Get-ResxNames([string]$path) {
    $text = [System.IO.File]::ReadAllText($path)
    return [regex]::Matches($text, '<data name="([^"]+)"') | ForEach-Object { $_.Groups[1].Value }
}

$enNames = [System.Collections.Generic.HashSet[string]]::new()
Get-ResxNames $resxEn | ForEach-Object { [void]$enNames.Add($_) }
$zhNames = [System.Collections.Generic.HashSet[string]]::new()
Get-ResxNames $resxZh | ForEach-Object { [void]$zhNames.Add($_) }

$skip = @('Get', 'Culture', 'ResourceManager')

$getRefs = @{}
$propRefs = @{}

foreach ($file in (Get-ChildItem $srcDir -Recurse -Filter *.cs -File | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' })) {
    $text = [System.IO.File]::ReadAllText($file.FullName)

    foreach ($m in [regex]::Matches($text, 'Strings\.Get\("([^"]+)"\)')) {
        $key = $m.Groups[1].Value
        if (-not $getRefs.ContainsKey($key)) { $getRefs[$key] = @() }
        $getRefs[$key] += $file.Name
    }

    foreach ($m in [regex]::Matches($text, 'Strings\.([A-Z][A-Za-z0-9_]*)\b')) {
        $key = $m.Groups[1].Value
        if ($skip -contains $key) { continue }
        if (-not $propRefs.ContainsKey($key)) { $propRefs[$key] = @() }
        $propRefs[$key] += $file.Name
    }
}

$missingEn = @()
foreach ($k in ($getRefs.Keys + $propRefs.Keys | Sort-Object -Unique)) {
    if (-not $enNames.Contains($k)) { $missingEn += $k }
}

$missingZh = @()
foreach ($k in ($getRefs.Keys + $propRefs.Keys | Sort-Object -Unique)) {
    if (-not $zhNames.Contains($k)) { $missingZh += $k }
}

Write-Host ("代码引用键：Get {0} 个 / 属性 {1} 个" -f $getRefs.Count, $propRefs.Count)
Write-Host ("资源键：英文 {0} 个 / 中文 {1} 个" -f $enNames.Count, $zhNames.Count)

if ($missingEn.Count -gt 0) {
    Write-Host ''
    Write-Host ("英文资源缺失 {0} 个：" -f $missingEn.Count) -ForegroundColor Yellow
    foreach ($k in $missingEn) { Write-Host ("  {0}  ({1})" -f $k, ((($getRefs[$k] + $propRefs[$k]) | Sort-Object -Unique) -join ', ')) }
}

if ($missingZh.Count -gt 0) {
    Write-Host ''
    Write-Host ("中文资源缺失 {0} 个：" -f $missingZh.Count) -ForegroundColor Yellow
    foreach ($k in $missingZh) { Write-Host ("  {0}" -f $k) }
}

if ($missingEn.Count -eq 0 -and $missingZh.Count -eq 0) {
    Write-Host '全部键均已命中。' -ForegroundColor Green
    exit 0
}

exit 1
