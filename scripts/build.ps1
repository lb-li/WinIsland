# WinIsland.Avalonia 构建脚本
# 用于本地构建和测试

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipTests,
    
    [Parameter(Mandatory=$false)]
    [switch]$Clean
)

$ErrorActionPreference = "Stop"
$ProjectPath = "WinIsland.Avalonia/WinIsland.Avalonia.csproj"
$TestProjectPath = "WinIsland.Avalonia.Tests/WinIsland.Avalonia.Tests.csproj"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "WinIsland.Avalonia 构建脚本" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 清理
if ($Clean) {
    Write-Host "🧹 清理项目..." -ForegroundColor Yellow
    dotnet clean $ProjectPath --configuration $Configuration
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    Write-Host "✅ 清理完成" -ForegroundColor Green
    Write-Host ""
}

# 恢复依赖
Write-Host "📦 恢复 NuGet 依赖..." -ForegroundColor Yellow
dotnet restore $ProjectPath
if ($LASTEXITCODE -ne 0) { 
    Write-Host "❌ 依赖恢复失败" -ForegroundColor Red
    exit $LASTEXITCODE 
}
Write-Host "✅ 依赖恢复完成" -ForegroundColor Green
Write-Host ""

# 构建
Write-Host "🔨 构建项目 ($Configuration)..." -ForegroundColor Yellow
dotnet build $ProjectPath --configuration $Configuration --no-restore
if ($LASTEXITCODE -ne 0) { 
    Write-Host "❌ 构建失败" -ForegroundColor Red
    exit $LASTEXITCODE 
}
Write-Host "✅ 构建完成" -ForegroundColor Green
Write-Host ""

# 运行测试
if (-not $SkipTests) {
    Write-Host "🧪 运行测试..." -ForegroundColor Yellow
    
    if (Test-Path $TestProjectPath) {
        dotnet test $TestProjectPath --configuration $Configuration --no-build --verbosity normal
        if ($LASTEXITCODE -ne 0) { 
            Write-Host "❌ 测试失败" -ForegroundColor Red
            exit $LASTEXITCODE 
        }
        Write-Host "✅ 测试通过" -ForegroundColor Green
    } else {
        Write-Host "⚠️  测试项目不存在，跳过测试" -ForegroundColor Yellow
    }
    Write-Host ""
}

# 完成
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "✅ 构建成功完成！" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "输出目录: WinIsland.Avalonia/bin/$Configuration/" -ForegroundColor Cyan
