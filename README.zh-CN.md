# WinIsland（中文文档）

> 让 Windows 顶部真正“活”起来。
> WinIsland 把通知、媒体、专注和提醒整合到一个顺手的动态交互入口。

## 为什么值得下载
- 桌面更清爽：重要信息统一在一个位置展示。
- 操作更高效：媒体、待办、计时无需频繁切窗口。
- 专注更稳定：番茄钟 + 提醒机制覆盖真实工作场景。
- 日常更顺手：轻量、快速、可长期常驻使用。

## 核心能力
- 通知接管与统一展示
- 全局媒体会话展示与控制
- 专注模式（番茄钟）
- 喝水提醒（间隔模式 / 自定义时间）
- 待办提醒（日期 + 时间 + 内容）
- 系统状态与进度显示

## 功能预览
### 文件中转
![File Gravity Hole](assets/feature_blackhole.png)

### 媒体控制
![Media Control](assets/feature_media.png)

### 通知与硬件事件
![Hardware Notification](assets/feature_notify.png)

### 专注与健康提醒
![Health Assistant](assets/feature_health.png)

## 下载方式
- 安装包：`installer/output/WinIsland-Setup-v1.0.0.exe`
- 安装文档：`installer/README.zh-CN.md`

## 源码运行
```powershell
dotnet build WinIsland.csproj -v minimal
dotnet run --project WinIsland.csproj
```

## 打包安装程序（非单文件）
```powershell
dotnet publish WinIsland.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o publish/win-x64
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\setup.iss
```

## 支持项目
如果 WinIsland 对你有帮助，欢迎点一个 Star。
你的 Star 会让更多人看到并下载这个项目。
