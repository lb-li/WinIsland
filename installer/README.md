# 📦 WinIsland Installer

Build and packaging instructions for WinIsland installer (Inno Setup based).

[📘 中文安装文档](README.zh-CN.md)

## 🧱 Prerequisites
- Windows
- Inno Setup 6+
- Published artifacts available (for example `publish/win-x64/`)

## 🛠️ Build Flow
### 1. Generate publish output
```powershell
dotnet publish ..\WinIsland.csproj -c Release -r win-x64 --self-contained true -o ..\publish\win-x64
```

### 2. Compile installer script
```powershell
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" setup.iss
```

### 3. Output folder
Default output path: `installer/output/`

## 🤫 Silent Install
```powershell
WinIsland-Setup-v1.0.0.exe /SILENT
WinIsland-Setup-v1.0.0.exe /VERYSILENT
WinIsland-Setup-v1.0.0.exe /SILENT /DIR="C:\MyApps\WinIsland"
```

## 🧹 Silent Uninstall
```powershell
"C:\Program Files\WinIsland\unins000.exe" /SILENT
```

## 🧪 Troubleshooting
### Compile failed
- Verify Inno Setup is installed
- Verify paths in `setup.iss` are valid

### Install failed
- Verify target directory permissions
- Check antivirus/security software blocking
