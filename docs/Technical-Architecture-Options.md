# MonoGame Hub — Technical Architecture Options

Date: 2025-12-19  
Status: Draft v0.1

## Executive Summary
MonoGame Hub needs a **modern, maintained, cross-platform desktop UI** with strong **.NET integration**, reliable **packaging** for Windows/macOS/Linux, and the ability to run and parse `dotnet`/NuGet tooling.

**Recommendation: Avalonia UI (with .NET 10 target)**
- Best balance of: true cross-platform desktop, modern UI capabilities, strong .NET tooling fit, and production maturity.
- Enables a Unity-Hub-like UI (navigation rail, list/detail layouts, rich theming) without a heavy browser runtime.
- Packaging story is straightforward (self-contained builds, native installers via community tooling), and the app remains “just .NET”.

**Strong alternate path: Tauri (Rust + Web UI) if you want a web-tech UI**
- Extremely lightweight runtime compared to Electron.
- But increases complexity: Rust toolchain + JS build + .NET process integration + more moving parts.

This document includes non-.NET stacks (Rust-native UI, React Native Desktop, etc.) because they *can* orchestrate the required `dotnet`/NuGet operations cross-platform; however, they generally do so via IPC/process execution rather than native .NET APIs, which increases complexity for a community-driven MonoGame showcase.

## Constraints & Evaluation Criteria
Hard constraints:
- Runs on **Windows, macOS, Linux**.
- Integrates with **.NET** tooling and processes (`dotnet`, NuGet).
- Must be **actively maintained** and suitable for a flagship MonoGame experience.
- Targets **modern .NET** (prefer .NET 10; .NET 9 acceptable). Avoid framing .NET 8 as baseline.

Key criteria:
1. Cross-platform desktop reach
2. UI richness and theming (Hub-like navigation, lists, cards, rich text)
3. Packaging/distribution experience
4. .NET integration depth (native .NET vs interop)
5. Ecosystem maturity and community sentiment
6. Accessibility support
7. Community contributor accessibility (how approachable the stack is for typical MonoGame contributors)
8. Performance and footprint (explicitly *lower-weighted* for this product)
9. Long-term maintainability

## Options Matrix (scored)
Scoring: 1 (poor) → 5 (excellent). “Sentiment” is a qualitative read from typical community adoption patterns and maintainers’ stability.

### Weighting model (Contributor-heavy)
This weighting intentionally prioritizes a stack that MonoGame’s mostly-.NET contributor base can build and maintain.

Weights (sum = 100%):
- Cross-platform: 15%
- .NET fit: 20%
- UI/theming: 15%
- Packaging: 10%
- Maturity: 10%
- Accessibility: 10%
- Contributor accessibility: 15%
- Performance/footprint: 5%

Weighted Score formula:
- Weighted Score (/100) = $20 \times \sum_i (w_i \times s_i)$ where $s_i \in [1,5]$ and $w_i$ is the weight fraction.

| Framework | Cross-Platform | .NET Fit | UI/Theming | Packaging | Maturity | Accessibility | Contributor Accessibility | Perf/Footprint | Sentiment | Weighted Score (/100) |
|---|---:|---:|---:|---:|---:|---:|---:|---|---:|
| **Avalonia UI** | 5 | 5 | 5 | 4 | 5 | 4 | 5 | 4 | Very positive in .NET desktop OSS | **95** |
| **.NET MAUI** | 4 | 5 | 4 | 4 | 4 | 4 | 4 | 3 | Mixed-to-positive; desktop varies | **83** |
| **Uno Platform** | 4 | 4 | 4 | 3 | 4 | 4 | 3 | 3 | Positive; stronger in enterprise | **74** |
| **Electron (JS/TS)** | 5 | 2 | 5 | 5 | 5 | 4 | 3 | 2 | Positive but heavy | **77** |
| **Tauri (Rust + Web UI)** | 5 | 2 | 5 | 4 | 4 | 3 | 2 | 4 | Positive; modern/lean | **70** |
| **Wails (Go + Web UI)** | 5 | 2 | 4 | 4 | 4 | 3 | 2 | 4 | Positive; pragmatic, leaner than Electron | **67** |
| **Neutralinojs (Web UI + native bridge)** | 4 | 2 | 4 | 3 | 3 | 3 | 3 | 4 | Mixed-to-positive; very lightweight | **63** |
| **Rust-native UI (egui/iced/slint)** | 5 | 2 | 4 | 4 | 3 | 2 | 2 | 5 | Positive but fragmented ecosystem | **64** |
| **React Native Desktop** | 4 | 2 | 4 | 4 | 3 | 3 | 3 | 3 | Mixed; strong JS ecosystem, desktop varies | **64** |
| **Flutter (Dart)** | 5 | 2 | 4 | 4 | 4 | 3 | 2 | 4 | Positive; non-.NET stack | **67** |
| **Qt (C++ / QtQuick)** | 5 | 1 | 4 | 4 | 5 | 4 | 1 | 5 | Positive; non-.NET stack | **65** |
| **Compose Multiplatform (Kotlin)** | 4 | 1 | 4 | 3 | 4 | 3 | 1 | 4 | Positive; non-.NET stack | **55** |

Notes:
- Scores reflect “best fit for a MonoGame flagship hub” rather than general popularity.
- Non-.NET stacks can still work, but increase complexity and reduce contributor accessibility for the MonoGame community.

## Framework Summaries

### 1) Avalonia UI (Recommended)
**What it is:** A cross-platform .NET UI framework with a XAML-based declarative UI model.

**Pros**
- Native .NET end-to-end (easy integration with `dotnet` CLI, NuGet APIs, file IO).
- Excellent for “hub” UX patterns: navigation, list/detail, cards, rich theming.
- Runs well on Windows/macOS/Linux.
- Strong OSS community and long-term momentum.

**Cons**
- Not “native widget” look by default (it is its own rendering stack), but that’s usually a benefit for consistent branding.
- Requires designing MonoGame’s visual system (a plus for a showcase, but time investment).

**Best-fit architecture**
- MVVM app structure.
- Service layer for: project scanning, NuGet querying, templates management, IDE launching, blog/resources ingestion.


### 2) .NET MAUI
**What it is:** Microsoft’s cross-platform UI framework for mobile + desktop.

**Pros**
- First-party .NET alignment.
- Shared code across platforms.
- Decent packaging paths.

**Cons**
- Desktop experience varies and can be less predictable than a desktop-first framework.
- Achieving a Unity-Hub-like desktop UX may require more customization and careful platform handling.

**When to choose**
- If you also want a credible future mobile companion app.


### 3) Uno Platform
**What it is:** Cross-platform UI based on WinUI/XAML concepts; targets multiple platforms.

**Pros**
- Strong XAML story.
- Enterprise adoption and good tooling.

**Cons**
- Packaging and platform coverage depends on target choices.
- Community contributor familiarity may be lower than Avalonia in OSS desktop circles.


### 4) Electron (Web UI)
**What it is:** Chromium + Node.js desktop shell for web apps.

**Pros**
- Best-in-class UI flexibility and web ecosystem.
- Mature packaging and auto-update patterns.

**Cons**
- Heavy runtime footprint (memory/disk) for a “hub”.
- .NET integration is via IPC (Node ↔ .NET), adding complexity.

**When to choose**
- If the team is primarily web-focused and runtime weight is acceptable.


### 5) Tauri (Web UI + Rust)
**What it is:** A lightweight alternative to Electron using OS webviews and Rust backend.

**Pros**
- Much lighter than Electron.
- Strong security posture by default compared to many web shells.

**Cons**
- More complex toolchain (Rust + JS + .NET integration).
- Requires careful design for IPC and cross-platform process management.

**When to choose**
- If you want a web UI but cannot accept Electron’s footprint.


### 5.5) Wails (Go + Web UI)
**What it is:** A desktop app framework that pairs a Go backend with a web frontend and uses system webviews.

**Pros**
- Lighter than Electron in typical deployments (uses OS webview rather than bundling Chromium).
- Straightforward “backend + web UI” architecture and IPC patterns.
- Cross-platform desktop targeting is a first-class goal.

**Cons**
- Not a .NET-native stack: `dotnet`/NuGet integration is via process execution and/or IPC to a .NET component.
- Requires adopting a Go toolchain (and still a JS toolchain for the frontend).
- Accessibility and advanced UI behavior depend heavily on webview capabilities and chosen frontend.

**When to choose**
- If you want a web-tech UI with a relatively lean runtime and can accept a multi-toolchain build.


### 5.6) Neutralinojs (Web UI + native bridge)
**What it is:** A very lightweight desktop app approach that runs a web UI in a webview and exposes native OS capabilities through a bridge.

**Pros**
- Very small footprint compared to Electron-class stacks.
- Web UI development model; can be fast to iterate.

**Cons**
- Not a .NET-native stack: the Hub’s core operations would be implemented by spawning `dotnet` processes, or by coordinating with a .NET backend via IPC.
- Ecosystem and “batteries included” experience can be thinner than more established desktop stacks.
- Packaging and auto-update patterns are typically more DIY.

**When to choose**
- If minimum footprint is a top priority and you’re comfortable assembling more of the platform glue yourself.


### 6) Flutter (Dart)
**What it is:** Cross-platform UI toolkit with its own rendering engine.

**Pros**
- Very strong UI and animations.
- Consistent look across platforms.

**Cons**
- Not a .NET stack; contributors need Dart/Flutter.
- .NET integration again becomes IPC.


### 6.5) Rust-native UI (egui / iced / slint)
**What it is:** A set of Rust GUI approaches (immediate-mode and retained-mode) capable of producing cross-platform desktop apps.

**Pros**
- Excellent performance/footprint.
- Strong systems-level control; good security story when done carefully.

**Cons**
- No native .NET runtime: MonoGame Hub functionality would require either:
  - spawning `dotnet` processes and parsing output, and/or
  - a separate .NET “backend service” communicating over IPC.
- UI ecosystem is fragmented: different frameworks have different strengths and maturity.
- Accessibility support varies significantly by UI framework.

**When to choose**
- If you want the lightest possible desktop runtime and are comfortable with a multi-toolchain build and IPC architecture.


### 6.6) React Native Desktop
**What it is:** React Native targeting desktop platforms (notably Windows and macOS), enabling a React-style UI model.

**Pros**
- Familiar React development model; strong ecosystem for UI patterns.
- Can build a polished hub UI quickly with a web-adjacent workflow.

**Cons**
- Linux support is not as consistently “first-class” as Windows/macOS across the ecosystem, so the cross-platform story can be uneven.
- Not a .NET-native stack: orchestration of `dotnet`/NuGet operations is via process execution and/or IPC to a .NET component.
- Packaging and distribution can be more involved than “single stack .NET” approaches.

**When to choose**
- If the contributors are primarily React/JS-focused and you can accept a more complex integration story for `dotnet` operations.


### 7) Qt (C++ / QML)
**What it is:** Mature cross-platform native toolkit.

**Pros**
- Very mature and performant.
- Excellent for desktop UI.

**Cons**
- Not .NET; significantly increases barrier to entry for MonoGame contributors.
- Licensing/commercial considerations may matter depending on usage.


### 8) Compose Multiplatform (Kotlin)
**What it is:** Kotlin/JetBrains UI toolkit for desktop.

**Pros**
- Modern declarative UI.

**Cons**
- Not .NET.
- Smaller ecosystem for cross-platform desktop distribution compared to top options.

## Recommended Technical Direction (Avalonia)

### App Structure
- **UI layer**: Avalonia (MVVM)
- **Core services (pure .NET)**:
  - ProjectScanner: scans folders, parses `*.csproj`/`*.sln`, extracts signals
  - MonoGameVersionResolver: resolves wildcard versions via NuGet queries
  - TemplateManager: list/install/uninstall templates through `dotnet new` + optional NuGet direct checks
  - DotNetInfoService: `dotnet --info`, SDK detection
  - NuGetConfigService: manages `NUGET_PACKAGES` and relevant settings for child process execution
  - IDELauncher: Visual Studio / VS Code / Rider launching
  - FeedService: blog/resources ingestion + caching

### NuGet “registry” approach
- Prefer using official NuGet APIs via `NuGet.Protocol` for version listing.
- Respect configured sources (from NuGet config and `dotnet nuget list source`).
- Cache per-source responses locally.

### Packaging
- Ship self-contained builds per OS and architecture.
- Provide native installer formats (MSI/EXE on Windows, DMG/PKG on macOS, AppImage/DEB/RPM on Linux) based on community packaging tools and CI workflows.

## Open Questions / Decisions Needed
- Exact list of MonoGame template package IDs and how template install should be pinned.
- Exact rules for determining “legacy MGCB” vs “new pipeline” across template vintages.
- Preferred approach for Blog/Resources ingestion (RSS/JSON availability vs HTML parsing).
- Whether Hub should modify system-wide env vars or only apply per-process settings.

---

## Final Recommendation
Choose **Avalonia UI** for MonoGame Hub.
- It best satisfies the cross-platform requirement while staying entirely in the .NET ecosystem.
- It supports a polished, brandable hub UI and long-term maintainability for a community-led project.

If you want, I can next turn this into an implementation starter (solution structure + services + a minimal working UI shell) once the document direction is approved.
