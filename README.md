# WinIsland

> Make Windows feel alive at the top of your screen.
> WinIsland brings notifications, media, focus, and reminders into one smooth dynamic island.

[中文文档](README.zh-CN.md)

## Why People Like WinIsland
- Cleaner desktop: important updates appear in one focused place.
- Faster workflow: media, todo, and timer actions without app switching.
- Better focus: Pomodoro + reminder system designed for real daily work.
- Lightweight interaction: quick to use, easy to keep on all day.

## Feature Highlights
- Notification takeover and unified display
- Global media session display and controls
- Focus mode (Pomodoro)
- Water reminders (interval mode / custom schedule)
- Todo reminders (date + time + content)
- System status and progress display

## Screenshots
### File Transfer Hub
![File Gravity Hole](assets/feature_blackhole.png)

### Media Control
![Media Control](assets/feature_media.png)

### Notifications and Device Events
![Hardware Notification](assets/feature_notify.png)

### Focus and Wellness
![Health Assistant](assets/feature_health.png)


## Community
Join the WinIsland user group:

![WinIsland Community Group](assets/705fa98888b161b6c205756a11aeebfa.jpg)

## Download
- Installer package: `installer/output/WinIsland-Setup-v1.0.0.exe`
- Installer docs: `installer/README.md`

## Build From Source
```powershell
dotnet build WinIsland.csproj -v minimal
dotnet run --project WinIsland.csproj
```

## Pack Installer (Non-Single-File)
```powershell
dotnet publish WinIsland.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o publish/win-x64
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\setup.iss
```

## Support This Project
If WinIsland helps you, please give this repository a star.
Your star helps more people discover and use it.

