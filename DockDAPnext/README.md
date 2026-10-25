# DockDAPnext

A Visual Studio 2022 tool window that manages a D project's `dub.json` — project metadata, build configurations, source files and builds — without leaving the IDE.

This is the maintained successor to [DockDAP](https://github.com/AripaStudio/DockDAP); the previous version is archived.

## Features

- **Solution project selector** — every D project (or folder with a `dub.json`) in the open solution is listed; each one is managed separately. Nothing is loaded until you pick a project and press **Refresh**.
- **Project tab** — name, description, license, target name and target path, target type, authors, dependencies and `sourceFiles`, edited as lists.
- **Build configurations** — create and edit `configurations` entries with their own `dflags`, `lflags`, `versions`, `buildOptions` and `targetPath` (`Output/debug`, `Output/release` are kept in place automatically).
- **Show Dub.json** — read the current file as it exists on disk.
- **Build With Dub** — debug and release builds, "run after build", and a one-click **Sync source files from .dproj** that copies the `.d` files of the selected project file into `sourceFiles`.
- **AI Assistant** — talk to any OpenAI-compatible endpoint configured in `ai.json`, keep a history of the last requests, edit the prompt template, and apply a returned JSON change as a reviewed diff with a backup of `dub.json`.
- **Ask Copilot** — puts the current prompt on the clipboard and opens the Copilot chat.
- **Help and About Me** tabs, including the running extension version.
- **Save** writes `dub.json` atomically, and a failed save reports the real reason instead of losing data.

## Requirements

| Requirement | Notes |
|---|---|
| Visual Studio 2022 (17.x) | The VSIX targets VS 2022; `devenv.exe` 17.x |
| .NET Framework 4.8 | Runtime requirement of the extension |
| DUB (`dub.exe`) | Needed for the **Build With Dub** tab. Editing `dub.json` works without it. The extension looks for DUB through Visual D's registry entry, `PATH`, and the usual `C:\D` / `D:\D` install folders |
| Visual D (recommended) | The project file used for source-file sync is a `.visualdproj`/`.dproj` |

## Installation

1. Download the latest `DockDAPnext.vsix` from the repository's **Releases** page.
2. Double-click it (or run `VSIXInstaller.exe DockDAPnext.vsix`) and install it for Visual Studio 2022.
3. Restart Visual Studio.

## Usage

1. Open a solution that contains a D project, or a project whose folder has a `dub.json`.
2. Choose **Tools → Open DockDAP**. The tool window opens next to the solution.
3. Pick the project in the **Solution project** list and press **Refresh**. The status line then names the `dub.json` being managed. Opening the window or changing the selection alone never loads or writes a file.
4. Edit the tabs and press **Save File**. `Create or load File` creates `dub.json` in the selected project folder when it does not exist yet.
5. **Build With Dub → Debug / Release** runs `dub build --config=debug|release` in the project folder and streams the output to the **Dub Manager** output pane.

## Build from source

The project is a classic (non-SDK) VSIX project; build it with MSBuild from Visual Studio 2022, not `dotnet build`.

```bat
msbuild DockDAPnext\DockDAPnext.csproj -t:Restore
msbuild DockDAPnext\DockDAPnext.csproj -p:Configuration=Debug
rem or -p:Configuration=Release
```

Outputs, per configuration:

- `bin\<Configuration>\DockDAPnext.dll` — the extension assembly
- `bin\<Configuration>\DockDAPnext.vsix` — the installable package

### The ILRepack step (and why it exists)

`DockDAPnext` uses ReactiveUI 23.x (with System.Reactive, Splat, DynamicData) and Newtonsoft.Json. Other extensions in the same `devenv.exe` process — notably ReSharper — ship older copies of ReactiveUI **with the same assembly identity**. The CLR loads only one copy per identity, so whichever loads first wins, and the extension can fail at package load with `TypeLoadException: Could not load type 'ReactiveUI.RxSchedulers'`.

Renaming files or embedding them with Costura does not help: the identity is in the metadata, not the file name. The `MergeDependencies` target therefore merges those assemblies **into `DockDAPnext.dll`** with ILRepack, so the extension's own code carries the ReactiveUI types and the conflicting identity never has to be loaded. `ExcludeMergedFromVsix` then keeps the separate DLLs out of the VSIX; the merged DLL is the only copy that ships.

Build details worth knowing:

- `MergeDependencies` runs after `CopyFilesToOutputDirectory` and before the VSIX is created. Do not move it after the VSIX step, or the package will contain the unmerged assembly.
- The merged assembly is roughly 4 MB. `Newtonsoft.Json` is merged from the NuGet cache, and its version is kept at **13.0.1** in both the `PackageReference` and the `NewtonsoftVer` property, because `Community.VisualStudio.Toolkit` warns (`CVSTBLD002`) on newer versions.
- `-p:DisableMerge=true` builds the extension **without** merging. That is the control configuration used to prove whether a problem comes from ILRepack; the resulting VSIX then contains ReactiveUI, System.Reactive, DynamicData and Splat* as separate files. (Newtonsoft.Json is not packaged in that control build; Visual Studio already provides 13.0.1, which is why the toolkit warns above that version. The shipping build merges it.)
- `DisableMerge` only affects the post-build targets, so switching it on an incremental build reuses the already-merged assembly. Use `-t:Rebuild` for the control build; the `InvalidateMergeStamp` target then drops the merge stamp so the next normal build merges again instead of packaging the unmerged DLL.
- **Rebuilds are not byte-identical.** ILRepack 2.0.48 has no deterministic switch, so it rewrites the merged assembly (MVID, timestamps) on every run. Comparing hashes is only meaningful between one build and the copy installed from it; two rebuilds of the same source produce different files with the same size.
- `Microsoft.Extensions.DependencyInjection` is pinned to **9.0.0**, the version Visual Studio 2022 already has loaded; 10.x fails with `FileLoadException 0x80131040`.
- `Microsoft.VSSDK.BuildTools` is pinned to **17.0.5232**; 18.x expects newer deploy tasks than VS 2022 provides.

## Project structure

```
.
├─ DockDAPnext.sln
├─ LICENSE.txt
├─ README.md
└─ DockDAPnext/
   ├─ Command/            VS command wiring (Tools → Open DockDAP)
   ├─ ToolWindows/        tool window host and its root control
   ├─ UserControls/       the tab views, including projectManagersUCs/
   ├─ ViewModels/         ReactiveUI view models (partial classes per concern)
   ├─ Service/            dub.json read/write, dub.exe runner, AI client, sync
   │  ├─ Models/          DubConfigAp, BuildConfiguration, OperationResult<T>, …
   │  └─ InterFaces/      service contracts
   ├─ AppConfiguration/   dependency injection container
   ├─ Themes/             shared WPF styles
   ├─ Resources/          icons
   ├─ VSCommandTable.vsct command definitions
   └─ source.extension.vsixmanifest
```

`Archive/`, `bin/`, `obj/`, `*.log`, `*.vsix` and the ILRepack scratch files it writes next to the project (`ILRepack.List`, `*.resources`) are ignored by git; see `.gitignore`.

## Known issues

- **DUB is required for building.** If `dub.exe` is not installed (or not found through Visual D, `PATH`, `C:\D`, `D:\D`), the Build tab reports that instead of running anything. Editing `dub.json` still works.
- **`CVSTBLD002` warning** — `Community.VisualStudio.Toolkit` warns when Newtonsoft.Json is newer than 13.0.1. The project is aligned to 13.0.1, so a build should not show this warning; if it appears, the package reference and `NewtonsoftVer` have drifted apart.
- **ReactiveUI version conflicts** — solved by the ILRepack merge above, not by pinning to an older ReactiveUI. Other extensions that load ReactiveUI first can no longer break this one.
- **Experimental Instance deployment** — `VSIXInstaller.exe /rootSuffix:Exp /quiet /norestart <path>.vsix` works (it may return a non-zero code such as 209 while still installing successfully; confirm by the version folder under the Exp hive and by opening the window). Alternatively expand the VSIX into the Experimental hive and enable it (the helper scripts in `Archive/tools/` do that). Bump the manifest version for every build you install, otherwise the previous folder can keep a stale assembly loaded. Only the Experimental instance is used for testing; the main instance is never touched.
- **Not verified in every host** — the extension is tested against the Experimental instance. Behaviour alongside third-party extensions other than ReSharper is not verified.

## Contributing

1. Read `Archive/AI_Rules.md` before changing code; it defines comment, naming, versioning and reporting rules for this repository.
2. Build Debug and run the console harnesses used during development (`MultiProjectTest.exe`, `ConfigEnsureTest.exe`, `Harness.exe`). Their sources live in `bin/Debug`, which git ignores, so a fresh clone does not contain them.
3. Keep the `MergeDependencies` target and the version pins described above intact.
4. Open an issue or a pull request with the problem, the build configuration used, and the relevant log lines (`[DOCKDAP]` lines from the **Debug** output pane).

## License

MIT — see [LICENSE.txt](LICENSE.txt). Copyright © 2026 Aripa Studio.
