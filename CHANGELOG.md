# 流转 FlowShift 2.4.1

浅色桌面悬浮格式转换器。默认启动悬浮胶囊；拖入文件就地展开，点击目标即转换，进度和结果都留在悬浮层。旧版工作台仍可从右键菜单打开。

## 2.4.1 启动工作台

正常双击 EXE、桌面快捷方式或开始菜单快捷方式，现在首次启动就显示工作台；核心在后台初始化，不阻塞工作台出现。再次双击会恢复已有工作台，包括隐藏/最小化状态，不再只弹出悬浮坞。启动器可按进程身份恢复旧版已存在的工作台，避免后台实例吞掉打开操作。

资源管理器右键转换（--shell）和带文件参数启动仍显示悬浮面板。--workspace 保留为显式打开工作台参数。无文件的 open 请求不会因为转换任务忙碌而排队。

验证首次无参数启动、隐藏后重复启动、最小化后重复启动、单实例复用，以及 Shell 启动保留悬浮入口。测试使用独立 FLOWSHIFT_HOME 与 FLOWSHIFT_INSTANCE_KEY，未结束用户已有实例。FLOWSHIFT_NO_INTEGRATION=1 可在诊断时跳过真实菜单注册。

## 2.4 翻页修复

默认不再只生成一页。用户预设保持在前，剩余所有共同可用目标自动按每页 7 个追加，固定保留“更多格式”。旧版只保存一页也无需重新设置。中心始终显示当前页/总页数；只有一页时明确说明“仅此一页”。

原先 WPF 的滚轮/按键事件依赖窗口焦点。现在鼠标底层钩子将活动轮盘的垂直/水平滚轮直接路由到轮盘；独立 WH_KEYBOARD_LL 钩子处理上下左右、PageUp/PageDown 和 Esc。仅在轮盘打开时接收导航输入，关闭后继续原系统行为。软件生成的导航事件同样受支持。鼠标抬起仍继续传递，不回退之前的鼠标交接修复。

高精度滚轮按 120 单位累计；WPF Preview 事件作为后备，避免重复翻页。所有页面均由实际共同路线生成，未加入新转换引擎。

`WheelNavigation.cs` 管理导航路由与键盘钩子；`WheelNavigationTests.cs` 验证默认页覆盖、焦点在其他窗口时的路由、实际 Windows 钩子以及 SendInput 滚轮/方向键流程。

```powershell
Start-Process ..\FlowShift241.exe -ArgumentList '--wheel-navigation-test','C:\temp\navigation24' -Wait
```

导航测试中 SendInput 需要可访问的交互输入桌面；受限沙箱可能拒绝输入注入。此限制属于测试环境，运行本地程序处理用户实际输入不需要 SendInput。

## 鼠标交接修复与页面预设

修复从轮盘进入“更多格式”后的鼠标交接：物理右键释放事件继续传给系统与 WPF；轮盘消费自己的右键事件避免重复菜单；关闭后在 ContextIdle 执行选择，释放本线程鼠标捕获后再打开悬浮窗口，并聚焦搜索框。没有使用全局模拟鼠标释放。

在“格式罗盘”页直接编辑预设。按文件类型选择页面，每页 7 个格式，顶部开始顺时针排列；可新增、删除、恢复默认，每类最多 8 页。设置保存在 Preferences.WheelLayouts，旧配置自动使用单页默认值。不支持的预设自动跳过补位，“更多格式”固定保留。混合类型使用独立预设。

长按右键期间用滚轮翻页；也可按左右方向键或 PageUp/PageDown；键盘唤出后可点击圆心页码两侧箭头。

`WheelPresets.cs` 包含页面模型和编辑器；`WheelRegression.cs` 提供鼠标交接、搜索/按钮响应、预设保存及增删恢复的回归入口。

```powershell
Start-Process ..\FlowShift241.exe -ArgumentList '--wheel-regression','C:\temp\wheel23' -Wait
Start-Process ..\FlowShift241.exe -ArgumentList '--render-wheel-settings','C:\temp\settings.png' -Wait
```

## 环形格式轮盘

在桌面或资源管理器选中一个或多个文件，长按右键约 650 毫秒，轮盘出现在指针附近。指向格式后松开右键执行转换；在圆心或环外松开、按 Esc、切换到其他窗口均取消。普通短按仍是系统菜单。

轮盘使用浅色分区、紫色高亮和选中扇区外扩，中心显示文件数量和类型。最多 7 个常用格式，加固定“更多格式”。所有目标均来自所选文件共同可用路线。选择“更多格式”展开桌面悬浮窗口并直接显示全部共同目标，可搜索和调整转换参数。

键盘快捷键也打开轮盘；此模式点击格式确认。右键菜单、贴边悬浮、关于、托盘及现有转换内核继续保留。

`RadialCompass.cs` 包含环形几何、命中、窗口定位、鼠标释放与主程序转换衔接；`WheelTests.cs` 提供轮盘测试、预览和实际转换流程验证。`GestureHook.cs` 将真实鼠标释放坐标传给轮盘，并保留短按右键。

```powershell
Start-Process ..\FlowShift241.exe -ArgumentList '--wheel-test','C:\temp\wheel-tests' -Wait
Start-Process ..\FlowShift241.exe -ArgumentList '--render-wheel','C:\temp\wheel.png','--hover','webp','C:\temp\photo.jpg' -Wait
Start-Process ..\FlowShift241.exe -ArgumentList '--wheel-flow-test','webp','C:\temp\out','C:\temp\photo.jpg','C:\temp\second.png' -Wait
```

## 保留的桌面入口

- 拖动悬浮图标或面板标题到屏幕边缘，自动收成 18 DIP 薄标签；点击或拖入文件展开，收起后回到边缘。四个方向均可，位置自动保存。
- 文件和文件夹右键菜单新增“流转 · 转换格式”，包括常用格式和“选择其他格式”。Windows 11 位于“显示更多选项”。经典静态菜单多选最多 15 项；大量文件请直接拖入悬浮坞。
- 默认在资源管理器或桌面长按右键 650 毫秒唤出格式轮盘；短按保持普通右键行为。设置可修改鼠标、修饰键、时长、距离及键盘快捷键（默认 Ctrl+Alt+Space）。
- 悬浮面板、右键菜单和托盘均可打开“关于”。作者联系：微信公众号“景同志”，小红书“景同学”。
- 托盘采用内嵌、多尺寸、BMP 编码 ICO，修复异常空白/通用图标。Windows 托盘溢出区域由系统管理。

菜单只注册到当前用户 HKCU，退出程序后保留。设置中可以关闭；安装版卸载时移除本程序拥有的菜单。跨进程 JSON 收件箱将右键多选请求合并到已运行窗口。

源码新增 EdgeDock.cs、DesktopIntegration.cs、TriggerSettings.cs、NativeGesture.cs、InstanceInbox.cs、AboutWindow.cs、AppVisual.cs、DesktopTests.cs。

`build.ps1 -Name FlowShift241-Update -Thin` 可构建无内核更新程序，复用已有 `runtime-2.0.0.0`。未运行过完整 2.x 的电脑须先运行完整包。

## 转换内核

本版直接复用 Filee.Core 和 Filee.Engines。完整上游源码与 MIT 许可证位于 `Vendor/Filee`，固定提交及本地修改记录见 `UPSTREAM.txt`。共 255 种真实文件格式定义，另有解压文件夹这一伪格式。格式有只读、只写及依赖差异，并非任何两种格式都能互转。

`Bridge/FlowShift.CoreHost.csproj` 将内核发布为自带 .NET 10 的独立进程。WPF 悬浮界面使用 Windows 自带 .NET Framework 4.8，和内核通过 JSON 任务、进度行及取消标记通信。所有外部工具以独立进程运行。

## 完整构建

要求 Windows 10/11 x64、.NET Framework 4.8、.NET 10 SDK，以及下载依赖的网络连接。构建空间建议至少 10 GB。

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build-full.ps1 -Dotnet 'C:\Program Files\dotnet\dotnet.exe'
```

该脚本还原 NuGet，构建内核，下载并校验 7-Zip/rhwp/运行库与所有可选引擎，创建 `core-payload.zip`，再编译单文件程序和安装版。

产物在源码目录上一级：`FlowShift241.exe`、`FlowShift241-Setup.exe`。
只构建较小的可选安装引擎版，可加 `-SkipEngineDownloads`；它仍包含完整托管内核及 7-Zip/rhwp，其他引擎通过界面安装。全量交付采用预置引擎模式。

已有 `core-payload.zip` 时，只修改界面或安装器可以直接运行 `build.ps1`，无需 .NET SDK。源码 ZIP 不重复包含数百 MB 的二进制内核，使用 `build-full.ps1` 从固定依赖重建。

## 源码结构

- `FloatingDock.cs`：胶囊、拖放展开、常用格式/搜索、配方编辑、进度取消、结果与引擎安装。
- `GestureHook.cs`：可配置鼠标按键、拖动距离与长按手势。
- `Backend.cs`：内核安全解包、格式表、JSON 桥接、任务与下载的取消。
- `FlowShift.cs`：保留的工作台、Shell 选区/全局手势、自动文件夹、配置、历史、本地基础转换与自测。
- `Bridge/Program.cs`：Filee 引擎注册、路线规划、队列执行、所有已展示选项的映射、包状态和安装。
- `Installer.cs`：用户级安装和小型卸载器。
- `Vendor/Filee`：固定提交的上游源码与第三方声明（构建前获取，见 `Vendor/README.md`）。
- `Build/fetch-bundled.ps1`：固定 SHA-256 的基础引擎准备。

## 配置与内核缓存

默认 `%LOCALAPPDATA%\FlowShift`，可用环境变量 `FLOWSHIFT_HOME` 定向到专用测试目录。
首次启动自动解包内核至 `runtime-2.0.0.0`；转换和配置在本机完成。启动不自动联网。
`CoreData` 保存内核临时任务、可选下载引擎和引擎路径配置。
开发时可用 `FLOWSHIFT_CORE` 指向已发布的 `FlowShift.CoreHost.exe`，跳过资源解包。

## 验证入口

```powershell
# 界面渲染（不安装、不注册热键）
Start-Process ..\FlowShift241.exe -ArgumentList '--render-float','C:\temp\dock.png' -Wait
Start-Process ..\FlowShift241.exe -ArgumentList '--render-float','C:\temp\formats.png','--sample','C:\temp\photo.png' -Wait

# 悬浮层完整转换流程，输出结果 PNG 及路径日志
Start-Process ..\FlowShift241.exe -ArgumentList '--float-test','C:\temp\paper.pdf','docx','C:\temp\out','C:\temp\result.png' -PassThru -Wait

# 单文件 CLI；失败时返回 1，详情在 %TEMP%\flowshift-cli-error.txt
Start-Process ..\FlowShift241.exe -ArgumentList '--convert','C:\temp\paper.pdf','--to','docx','--output','C:\temp\out' -PassThru -Wait
```

参数含空格时请在参数内为路径加引号。基础引擎的 28 项自测保留为 `--self-test 新目录`；完整转换矩阵脚本位于 `Tests/integration_matrix.py`，运行要求 Pillow、pypdf、python-docx、openpyxl、python-pptx、reportlab。

本次验证情况见交付的 FlowShift241-验证报告.txt。轮盘 59 项检查，桌面保留 106 项检查，包括真实 WPF 窗口四边隐藏、展开、收起，以及隔离注册表分支菜单注册/移除。未执行真实用户注册表的安装/卸载周期，也未对所有 Windows 多显示器组合做人工拖动验收。加密、DRM、损坏及特定私有格式仍受其自身限制。
