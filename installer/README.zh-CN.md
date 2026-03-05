# 📦 WinIsland 安装程序文档（中文）

## 🧱 前置要求
- Windows
- Inno Setup 6+
- 已生成发布产物（例如 `publish/win-x64/`）

## 🛠️ 构建流程
### 1. 生成发布文件
```powershell
dotnet publish ..\WinIsland.csproj -c Release -r win-x64 --self-contained true -o ..\publish\win-x64
```

### 2. 编译安装脚本
```powershell
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" setup.iss
```

### 3. 输出目录
默认输出到：`installer/output/`

## 🤫 静默安装
```powershell
WinIsland-Setup-v1.0.0.exe /SILENT
WinIsland-Setup-v1.0.0.exe /VERYSILENT
WinIsland-Setup-v1.0.0.exe /SILENT /DIR="C:\MyApps\WinIsland"
```

## 🧹 静默卸载
```powershell
"C:\Program Files\WinIsland\unins000.exe" /SILENT
```

## 🧪 常见问题
### 编译失败
- 检查 Inno Setup 是否安装
- 检查 `setup.iss` 内路径是否存在

### 安装失败
- 检查目标目录权限
- 检查安全软件拦截

## 🔐 发布建议
- 更新版本时同步修改 `setup.iss` 版本定义
- 发布前计算 SHA256 校验值
