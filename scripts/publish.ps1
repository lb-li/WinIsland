# WinIsland.Avalonia 发布脚本
# 用于创建发布包

param(
    [Parameter(Mandatory=$false)]
    [string]$Version = "1.0.0",
    
    [Parameter(Mandatory=$false)]
    [ValidateSet('win-x64', 'win-x86', 'all')]
    [string]$Runtime = 'all',
    
    [Parameter(Mandatory=$false)]
    [string]$OutputDir = "publish",
    
    [Parameter(Mandatory=$false)]
    [switch]$CreateArchive
)

$ErrorActionPreference = "Stop"
$ProjectPath = "WinIsland.Avalonia/WinIsland.Avalonia.csproj"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "WinIsland.Avalonia 发布脚本" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "版本: $Version" -ForegroundColor Cyan
Write-Host "运行时: $Runtime" -ForegroundColor Cyan
Write-Host "输出目录: $OutputDir" -ForegroundColor Cyan
Write-Host ""

# 创建输出目录
if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir | Out-Null
}

# 发布函数
function Publish-Runtime {
    param(
        [string]$RuntimeId
    )
    
    Write-Host "📦 发布 $RuntimeId..." -ForegroundColor Yellow
    
    $runtimeOutputDir = Join-Path $OutputDir $RuntimeId
    
    dotnet publish $ProjectPath `
        --configuration Release `
        --runtime $RuntimeId `
        --self-contained true `
        --output $runtimeOutputDir `
        /p:Version=$Version `
        /p:PublishSingleFile=true `
        /p:PublishTrimmed=true `
        /p:PublishReadyToRun=true `
        /p:IncludeNativeLibrariesForSelfExtract=true `
        /p:DebugType=none `
        /p:DebugSymbols=false
    
    if ($LASTEXITCODE -ne 0) { 
        Write-Host "❌ 发布 $RuntimeId 失败" -ForegroundColor Red
        exit $LASTEXITCODE 
    }
    
    Write-Host "✅ 发布 $RuntimeId 完成" -ForegroundColor Green
    
    # 显示文件大小
    $exePath = Get-ChildItem -Path $runtimeOutputDir -Filter "*.exe" | Select-Object -First 1
    if ($exePath) {
        $sizeMB = [math]::Round($exePath.Length / 1MB, 2)
        Write-Host "   文件大小: $sizeMB MB" -ForegroundColor Cyan
    }
    
    Write-Host ""
    
    return $runtimeOutputDir
}

# 创建压缩包函数
function Create-Archive {
    param(
        [string]$SourceDir,
        [string]$RuntimeId
    )
    
    Write-Host "📦 创建压缩包 $RuntimeId..." -ForegroundColor Yellow
    
    $archiveName = "WinIsland-v$Version-$RuntimeId.zip"
    $archivePath = Join-Path $OutputDir $archiveName
    
    # 删除已存在的压缩包
    if (Test-Path $archivePath) {
        Remove-Item $archivePath -Force
    }
    
    # 创建压缩包
    Compress-Archive -Path "$SourceDir\*" -DestinationPath $archivePath -CompressionLevel Optimal
    
    if ($LASTEXITCODE -ne 0) { 
        Write-Host "❌ 创建压缩包失败" -ForegroundColor Red
        exit $LASTEXITCODE 
    }
    
    # 显示压缩包大小
    $archive = Get-Item $archivePath
    $sizeMB = [math]::Round($archive.Length / 1MB, 2)
    Write-Host "✅ 压缩包创建完成: $archiveName ($sizeMB MB)" -ForegroundColor Green
    Write-Host ""
    
    return $archivePath
}

# 计算校验和函数
function Calculate-Checksum {
    param(
        [string]$FilePath
    )
    
    $hash = (Get-FileHash -Path $FilePath -Algorithm SHA256).Hash
    return $hash
}

# 发布运行时
$publishedDirs = @()
$archives = @()

if ($Runtime -eq 'all' -or $Runtime -eq 'win-x64') {
    $dir = Publish-Runtime -RuntimeId 'win-x64'
    $publishedDirs += @{ Runtime = 'win-x64'; Path = $dir }
}

if ($Runtime -eq 'all' -or $Runtime -eq 'win-x86') {
    $dir = Publish-Runtime -RuntimeId 'win-x86'
    $publishedDirs += @{ Runtime = 'win-x86'; Path = $dir }
}

# 创建压缩包
if ($CreateArchive) {
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "创建压缩包" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""
    
    foreach ($item in $publishedDirs) {
        $archivePath = Create-Archive -SourceDir $item.Path -RuntimeId $item.Runtime
        $archives += @{ Runtime = $item.Runtime; Path = $archivePath }
    }
    
    # 生成校验和文件
    Write-Host "🔐 生成校验和..." -ForegroundColor Yellow
    
    $checksumFile = Join-Path $OutputDir "checksums.txt"
    $checksumContent = "# SHA256 Checksums`n`n"
    
    foreach ($archive in $archives) {
        $hash = Calculate-Checksum -FilePath $archive.Path
        $fileName = Split-Path $archive.Path -Leaf
        $checksumContent += "## $fileName`n"
        $checksumContent += "``````n"
        $checksumContent += "$hash`n"
        $checksumContent += "``````n`n"
    }
    
    $checksumContent | Out-File -FilePath $checksumFile -Encoding UTF8
    Write-Host "✅ 校验和文件已生成: checksums.txt" -ForegroundColor Green
    Write-Host ""
}

# 完成
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "✅ 发布成功完成！" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "输出目录: $OutputDir" -ForegroundColor Cyan

if ($CreateArchive) {
    Write-Host ""
    Write-Host "发布包:" -ForegroundColor Cyan
    foreach ($archive in $archives) {
        $fileName = Split-Path $archive.Path -Leaf
        Write-Host "  - $fileName" -ForegroundColor Cyan
    }
}
