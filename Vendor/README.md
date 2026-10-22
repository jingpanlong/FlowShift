# Vendor / Filee（转换内核依赖）

本目录在构建时需要包含 **Filee** 转换内核的源码（MIT 许可证，固定提交）。
FlowShift 不内嵌 Filee 源码，请按下方步骤获取。

## 它是谁

- 项目：Filee 转换内核
- 仓库：<https://github.com/KnifeLemon/Filee>
- 许可证：MIT，Copyright (c) 2026 Myeonggu Kim (KnifeLemon) and Filee contributors
- 固定提交：`799e39485f2aa78677f9ba51f692e862a7af76a6`

FlowShift 复用 `Filee.Core` 与 `Filee.Engines`，通过一个独立的 .NET 10 自包含宿主进程
（`Bridge/FlowShift.CoreHost.csproj`）运行；WPF 悬浮界面使用系统自带的 .NET Framework 4.8，
二者通过 JSON 任务、进度行与取消标记通信。所有外部工具均以独立进程运行。

## 获取（固定提交）

```powershell
git clone https://github.com/KnifeLemon/Filee.git Vendor/Filee
cd Vendor/Filee
git checkout 799e39485f2aa78677f9ba51f692e862a7af76a6
cd ..
```

## 说明

- 固定提交与本地修改记录见仓库根目录 [`UPSTREAM.txt`](../UPSTREAM.txt)。
- 完整 MIT 许可证：`Vendor/Filee/LICENSE`；第三方声明：`Vendor/Filee/THIRD-PARTY-NOTICES.md`。
- 获取后运行 `build-full.ps1`（或已有 `core-payload.zip` 时运行 `build.ps1`）即可构建。
- 本目录的 `Vendor/Filee/` 已在 `.gitignore` 中忽略，不会被提交到本仓库。
