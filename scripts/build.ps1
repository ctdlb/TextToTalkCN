<#
    一键构建 TextToTalkCN（国服 Dalamud 版）。

    用法：
        pwsh -NoProfile -File scripts\build.ps1                 # Debug 构建
        pwsh -NoProfile -File scripts\build.ps1 -Configuration Release
        pwsh -NoProfile -File scripts\build.ps1 -Install        # 构建后复制到 devPlugins
        pwsh -NoProfile -File scripts\build.ps1 -DotnetPath D:\dotnet\dotnet.exe

    依赖：
        .NET 10 SDK（dotnet 在 PATH 上，或用 -DotnetPath / DOTNET_ROOT 指定）
        Dalamud dev 目录（默认自动探测 %APPDATA%\XIVLauncherCN\addon\Hooks\dev）
#>
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',
    [switch]$Install,
    [string]$DotnetPath
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot

function Resolve-Dotnet {
    $candidates = @(
        $DotnetPath,
        (Get-Command dotnet -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty Source),
        (Join-Path $env:DOTNET_ROOT 'dotnet.exe' -ErrorAction SilentlyContinue),
        (Join-Path $env:DOTNET_INSTALL_DIR 'dotnet.exe' -ErrorAction SilentlyContinue),
        "$env:ProgramFiles\dotnet\dotnet.exe",
        "${env:ProgramFiles(x86)}\dotnet\dotnet.exe"
    ) | Where-Object { $_ -and (Test-Path $_) }

    foreach ($candidate in $candidates) {
        $sdks = & $candidate --list-sdks 2>$null
        if ($sdks -match '^10\.') { return $candidate }
    }

    throw '找不到 .NET 10 SDK。请安装 https://dotnet.microsoft.com/download/dotnet/10.0 或把 dotnet 加入 PATH。'
}

function Resolve-DalamudHome {
    if ($env:DALAMUD_HOME -and (Test-Path (Join-Path $env:DALAMUD_HOME 'Dalamud.dll'))) {
        return $env:DALAMUD_HOME
    }

    $candidates = @(
        (Join-Path $env:APPDATA 'XIVLauncherCN\addon\Hooks\dev'),
        (Join-Path $env:APPDATA 'XIVLauncher\addon\Hooks\dev')
    )

    foreach ($candidate in $candidates) {
        if (Test-Path (Join-Path $candidate 'Dalamud.dll')) { return $candidate }
    }

    throw '找不到 Dalamud。请启动一次 XIVLauncherCN 让 Dalamud 完成安装，或设置 DALAMUD_HOME。'
}

$dotnet = Resolve-Dotnet
$dalamudHome = Resolve-DalamudHome
$env:DALAMUD_HOME = $dalamudHome

Write-Host "dotnet : $dotnet"
Write-Host "Dalamud: $dalamudHome"
Write-Host "配置   : $Configuration"

$project = Join-Path $root 'src\TextToTalk\TextToTalk.csproj'
& $dotnet build $project -c $Configuration "-p:DalamudLibPath=$dalamudHome\"
if ($LASTEXITCODE -ne 0) { throw "构建失败（exit $LASTEXITCODE）" }

$outDir = Join-Path $root "src\TextToTalk\bin\$Configuration"
$dll = Join-Path $outDir 'TextToTalkCN.dll'
if (-not (Test-Path $dll)) {
    throw "没有找到构建产物：$dll"
}

Write-Host ''
Write-Host "构建产物：$outDir"

if ($Install) {
    $devPlugins = Join-Path $env:APPDATA 'XIVLauncherCN\devPlugins'
    New-Item -ItemType Directory -Force -Path $devPlugins | Out-Null
    $target = Join-Path $devPlugins 'TextToTalkCN'
    if (Test-Path $target) { Remove-Item $target -Recurse -Force }
    # 排除打包脚本自己生成的 TextToTalkCN\ 子目录（里面是 latest.zip）
    robocopy $outDir $target /E /XD TextToTalkCN /NFL /NDL /NJH /NJS /NP | Out-Null
    Write-Host "已安装到：$target"
    Write-Host '重启游戏，或执行 /xlplugins 打开插件列表，启用开发插件即可。'
}
