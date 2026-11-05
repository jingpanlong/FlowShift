# FlowShift

![FlowShift — long-press right-click, release to convert](docs/banner.jpg)

> A local Windows file-format converter: floating dock, Explorer context menu, radial format wheel, and batch processing.
> Everything runs locally — files are never uploaded and originals are preserved.

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE.txt)
[![Release](https://img.shields.io/github/v/release/jingpanlong/FlowShift?label=Release)](https://github.com/jingpanlong/FlowShift/releases)

---

## Overview

**FlowShift** is a **local Windows file-format converter**. A light floating capsule lives on your desktop: drop a file in and it expands in place; click a target format to convert — progress and results stay in the floating layer. It also offers an Explorer context-menu entry, a long-press right-click "format wheel", edge-docking, and batch/parameterized conversion.

**Everything runs locally**: files are never uploaded and originals are preserved. The conversion engine reuses the open-source [Filee](https://github.com/KnifeLemon/Filee) core (MIT).

## Features

- **Edge-docking floating capsule**: drag it to any screen edge and it collapses to a thin tab; click or drop a file to expand. Position is saved automatically.
- **Explorer context menu** "FlowShift · Convert" with common formats and a "choose other format" option (Windows 11: under "Show more options").
- **Radial format wheel**: long-press right-click (~650 ms) on selected files, point at a format, release to convert. A keyboard shortcut is also supported (default `Ctrl+Alt+Space`).
- **Batch & parameterized conversion**, recipe editing, progress cancellation, result and engine installation.
- **Single-instance** reuse, system tray, auto folder, local config & history.
- **255 real file-format definitions** (including an "extract folder" pseudo-format); formats have read-only / write-only / dependency differences — not every pair is convertible.

## Screenshots

![Expanded dock](docs/screenshots/FlowShift21-expanded.png)
![Format wheel settings](docs/screenshots/FlowShift24-wheel-page1.png)
![Conversion result](docs/screenshots/FlowShift21-result.png)
![Installer preview](docs/screenshots/FlowShift2-Setup-preview.png)

## Download & Install

Get builds from [Releases](https://github.com/jingpanlong/FlowShift/releases):

- `FlowShift241.exe`: single-file build, run directly, no install.
- `FlowShift241-Setup.exe`: installer (per-user), with Start Menu / desktop shortcuts; uninstall removes the context-menu entries it owns.

Requires **Windows 10/11 x64**.

## Source & Build

Requires Windows 10/11 x64, .NET Framework 4.8, .NET 10 SDK, and network access for dependency downloads; ~10 GB free space recommended.

1. Fetch the conversion core at the pinned commit (see [`UPSTREAM.txt`](UPSTREAM.txt) and [`Vendor/README.md`](Vendor/README.md)):
   ```powershell
   git clone https://github.com/KnifeLemon/Filee.git Vendor/Filee
   cd Vendor/Filee && git checkout 799e39485f2aa78677f9ba51f692e862a7af76a6 && cd ..
   ```
2. Full build (restore NuGet, build the core, download and verify 7-Zip / rhwp / runtimes and optional engines, produce the single-file build and installer):
   ```powershell
   powershell -NoProfile -ExecutionPolicy Bypass -File .\build-full.ps1 -Dotnet 'C:\Program Files\dotnet\dotnet.exe'
   ```
   Output (one level above the source dir): `FlowShift241.exe`, `FlowShift241-Setup.exe`.
   Smaller optional-engine build: `build-full.ps1 -SkipEngineDownloads`. With an existing `core-payload.zip`, `build.ps1` alone rebuilds the UI/installer without the .NET SDK.

## License

Released under the [MIT License](LICENSE.txt). The Filee conversion core and its open-source components keep their own licenses (see [`THIRD-PARTY-NOTICES.txt`](THIRD-PARTY-NOTICES.txt) and `Vendor/Filee`).

## About

- Author contact: WeChat Official Account "景同志", Xiaohongshu "景同学".
- Source and downloads: see the Releases page and repository home above.

> Repository home: <https://github.com/jingpanlong/FlowShift> · Downloads: <https://github.com/jingpanlong/FlowShift/releases>
