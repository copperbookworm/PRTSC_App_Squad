# SquadShot · Squad 截图快捷工具

按一下 **PrtSc**，把当前屏幕（或 Squad 游戏窗口）截下来，自动存成图片并弹出右下角预览。
托盘常驻、开机自启可选，不打断游戏。

- 适用：Windows 10 / 11，Squad（SquadGame-Win64-Shipping）
- 成品：`SquadShot.exe` 单文件自包含发布版 —— **免安装、免 .NET、免编译，一个文件复制到哪都能双击运行**，见下方「下载」

## 下载

单文件版在 Releases：

<https://github.com/copperbookworm/PRTSC_App_Squad/releases/latest>

| 项目 | 说明 |
| --- | --- |
| 文件名 | `SquadShot.exe` |
| 大小 | 约 48 MB（单文件，自带 .NET 运行时，不用另装任何东西） |
| 系统 | Windows 10 / 11，64 位（x64） |
| 安装 | 不需要。下载后放哪都行（桌面、U 盘、任意文件夹），双击运行 |

第一次运行可能被 SmartScreen / 杀软拦一下 —— exe 没有代码签名，选“更多信息 → 仍要运行”即可。
程序只在 `%APPDATA%\SquadShot\` 和 `我的图片\SquadShots\` 下写文件，不写注册表（除非你手动打开“开机自启”）。

> 仓库里只放源码（16 个 `.cs` + 工程文件 + 图标 + 文档）；`bin/`、`obj/`、`publish/` 里的产物和调试文件都不进 git，成品统一以 Releases 附件发布。
## 快速开始

1. 双击 `SquadShot.exe`，右下角托盘出现蓝圈相机图标，程序开始待命。
2. 进游戏，按 **PrtSc**。
3. 听到提示音、右下角弹出缩略图，图片已经在 `我的图片\SquadShots` 里。

托盘图标操作：

| 操作 | 效果 |
| --- | --- |
| 左键双击 | 立即截一张 |
| 右键 | 菜单：立即截图 / 打开截图文件夹 / 设置… / 开机自启 / 退出 |

## 截图存到哪

默认目录：`%USERPROFILE%\Pictures\SquadShots\`

文件名格式：

```
Squad_2026-10-24_23-40-03.png                没识别到地图时
Squad_2026-10-24_23-40-03_Gorodok_RAAS_v1.png  识别到地图/层时
```

同一秒内重复截图会自动加 `_2`、`_3` 后缀，不会覆盖。

**地图名是怎么来的**：程序读取 `%LOCALAPPDATA%\SquadGame\Saved\Logs\SquadGame.log`，
取最后一条 "Bringing World /Game/Maps/..." 里的地图与层名（如 `Gorodok_RAAS_v1`）。
没开过游戏、日志被清空、或当前在加载界面时，会自动省略地图名——不影响截图。

## 设置

托盘右键 →「设置…」，或启动时加 `--settings`。

- **截图热键**：点一下输入框，直接按想要的组合键（如 `Ctrl+Alt+F10`）。纯修饰键会被忽略。默认 `PrintScreen`。
- **保存位置**：可浏览选择，保存时自动创建目录。
- **图片格式**：PNG（无损，推荐）/ JPG 高质量 / JPG 标准 / JPG 小体积。
- **截图范围**：整个显示器（默认，推荐）/ 仅游戏窗口。
- **文件名附加地图/层名**：默认开。
- **截图后复制到剪贴板**：开了可以直接在聊天软件里 Ctrl+V。
- **播放提示音**：默认开（系统提示音）。
- **显示右下角缩略图提示**：默认开，显示 3 秒后淡出。
- **开机自动启动**：写入 `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`，不需要管理员权限。

保存时会先试注册热键，若被别的程序占用会提示且保留原设置。

## 配置文件与日志

| 内容 | 路径 |
| --- | --- |
| 配置 | `%APPDATA%\SquadShot\config.json` |
| 日志 | `%APPDATA%\SquadShot\logs\app-yyyyMMdd.log` |
| 最近一次截图结果 | `%APPDATA%\SquadShot\last-capture.json` |

每次截图（热键、托盘菜单或命令行）都会刷新 `last-capture.json`，可用于自检与自动化：

```json
{
  "ok": true,
  "path": "C:\\Users\\you\\Pictures\\SquadShots\\Squad_2026-10-24_23-41-10.png",
  "width": 1920,
  "height": 1080,
  "map": null,
  "source": "显示器（主屏）",
  "elapsedMs": 151,
  "bytes": 374584
}
```

## 命令行

| 参数 | 说明 |
| --- | --- |
| （无参数）/ `--tray` | 托盘常驻 |
| `--settings` | 启动并打开设置窗口 |
| `--capture-now` / `-c` | 不弹界面，直接截一张后退出，结果写入 `last-capture.json`，成功返回 0 |

单实例：已经在托盘运行时再双击，只会唤起已有实例（不会再开一个）。

## 原理与限制

- 截图用 GDI `BitBlt`（`SRCCOPY | CAPTUREBLT`）从屏幕 DC 抓整屏。对 Squad 默认的「无边框窗口全屏」（`FullscreenMode=1`）有效，且天然不会把鼠标指针画进去。
- 「仅游戏窗口」模式走 `PrintWindow(PW_RENDERFULLCONTENT)`；如果 D3D 画面抓到全黑，会自动回退到整屏，保证你总有图。
- 本机（GTX 1660 Ti + Intel 核显的混合显卡）实测 `IDXGIOutput1::DuplicateOutput` 返回 `DXGI_ERROR_UNSUPPORTED`，所以没有采用 DXGI 桌面复制；Windows Graphics Capture 在 Win10 上会带黄色边框，也没有采用。
- 程序是 PerMonitorV2 DPI 感知：125% 缩放下仍按 1920×1080 物理像素截图，不会存成 1536×864。
- 已知限制：
  - 游戏若切成「独占全屏」，部分驱动下整屏抓取可能得到黑图 —— 用无边框全屏即可。
  - 开启 HDR 时，抓到的可能是 SDR 映射后的画面。
  - 以管理员权限运行的游戏/程序，其前台窗口在非管理员的本程序下「仅窗口」抓取可能失败（整屏抓取不受影响）。
  - Windows 11 若打开了「使用 PrtSc 打开截图工具」，需要先在 设置 → 辅助功能 → 键盘 里关掉，否则 PrtSc 会被系统抢走；也可以把热键改成 `Ctrl+Alt+F10`。
  - 极少数游戏会独占键盘或拦截 PrtSc，若游戏内按了没反应，把热键换成 `Ctrl+Alt+F10` 之类即可。

## 从源码构建

需要 .NET SDK 9 或更高。

```powershell
cd PRTSC_App_Squad

# 调试构建/运行
dotnet build
.\bin\Debug\net9.0-windows\SquadShot.exe

# 发布单文件（自包含，约 48 MB）
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=none -o publish
```

若只想发布「框架依赖」的轻量版（目标机需装 .NET 9 桌面运行时）：

```powershell
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o publish-lite
```

## 代码结构

| 文件 | 作用 |
| --- | --- |
| `Program.cs` | 入口、单实例、命令行分发 |
| `TrayApp.cs` | 托盘图标与菜单、截图编排（热键 → 抓图 → 保存 → 提示） |
| `SettingsForm.cs` | 深色设置窗口（热键捕获、目录、格式、开关） |
| `ToastForm.cs` | 右下角不抢焦点的缩略图提示 |
| `CaptureService.cs` | 截图主流程、文件命名、PNG/JPG 编码 |
| `ScreenCapture.cs` | BitBlt 抓屏、PrintWindow 抓窗、缩略图 |
| `SquadInfo.cs` | 找 Squad 窗口、从日志解析当前地图/层 |
| `HotkeyManager.cs` / `HotkeyBinding.cs` | 全局热键注册与解析 |
| `AppConfig.cs` / `AppPaths.cs` / `AutoStart.cs` / `Logger.cs` / `Native.cs` / `AppIcon.cs` | 配置、路径、自启、日志、Win32、图标 |
| `app.ico` | 程序图标（16–256 px 多尺寸） |
| `make-icon.ps1` | 图标生成脚本（改配色/细节后重新生成 app.ico） |

## 已验证

- `--capture-now` 在 1920×1080、125% 缩放环境下产出 1920×1080 PNG，耗时约 150 ms。
- 模拟按 PrtSc → 托盘程序注册的热键触发 → 生成新文件 → 弹出缩略图提示，全链路通过。
- PNG 与 JPG（质量 85）两种输出、设置窗口、深色标题栏、托盘菜单、图标内嵌均已实测。
- 单文件 `SquadShot.exe` 发布版实测：托盘启动 → 注入 PrtSc → 新图片生成（1920×1080、约 115 ms）。
- 把发布版 exe 单独复制到一个空文件夹（旁边没有任何其它文件）再运行 `--capture-now`：返回 0，产出 1920×1080 JPG（343 KB，135 ms），说明它确实不依赖安装目录或 .NET 运行时。

## 许可证

[MIT](LICENSE) © 2026 copperbookworm
