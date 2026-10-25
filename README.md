# DockDAPnext

A DUB manager for Visual Studio 2022. It opens a tool window where you can read and edit the
`dub.json` of a D project, keep its build configurations in order, sync source files from the
project file, and run `dub build` — all without leaving the IDE.

This is the maintained version. The older DockDAP extension is archived at
[AripaStudio/DockDAP](https://github.com/AripaStudio/DockDAP).

## What it does

- **Solution project selector.** Every D project in the open solution — or any project folder that
  already has a `dub.json` — is listed. Nothing is read or written until you pick a project and
  press **Refresh**, so opening the window is always safe.
- **Project tab.** Name, description, license, target name, target path, target type, authors,
  dependencies and `sourceFiles`, edited as plain lists.
- **Build configurations.** Create and edit `configurations` entries with their own `dflags`,
  `lflags`, `versions`, `buildOptions` and `targetPath`. `Output/debug` and `Output/release` are
  created and kept when a project needs them.
- **Show Dub.json.** Opens the file exactly as it is on disk, which is handy when something in the
  UI does not match what you expect.
- **Build With Dub.** Debug and release builds, an optional run after a successful build, and a
  one-click *Sync source files from .dproj* that copies the `.d` files of the selected project
  file into `sourceFiles`.
- **AI Assistant.** Talks to any OpenAI-compatible endpoint configured in `ai.json`, keeps the last
  requests, lets you edit the prompt template, and applies a returned JSON change as a reviewed
  diff with a backup of `dub.json`.
- **Ask Copilot.** Puts the current prompt on the clipboard and opens Copilot Chat.
- **Help and About tabs**, the About tab showing the running version.

Saving writes `dub.json` atomically, and a failed save reports the real reason instead of quietly
dropping your edits.

## Requirements

| Requirement | Notes |
|---|---|
| Visual Studio 2022 (17.x) | The VSIX targets VS 2022 |
| .NET Framework 4.8 | Runtime the extension is built against |
| DUB (`dub.exe`) | Only needed for **Build With Dub**. Editing `dub.json` works without it. DUB is found through Visual D's registry entry, `PATH`, and the usual `C:\D` / `D:\D` install folders |
| Visual D (recommended) | Source-file sync reads a `.dproj` / `.visualdproj` file |

## Install

1. Download the latest `DockDAPnext.vsix` from the repository's **Releases** page.
2. Double-click it, or run `VSIXInstaller.exe DockDAPnext.vsix`.
3. Restart Visual Studio.

## Use

1. Open a solution that contains a D project.
2. **Tools → Open DockDAP**. The window docks next to Solution Explorer.
3. Pick the project in **Solution project** and press **Refresh**. The status line then shows the
   `dub.json` being managed.
4. Edit the tabs and press **Save File**. If the project has no `dub.json` yet, **Create or load File**
   creates one in the selected project folder.
5. **Build With Dub → Debug / Release** runs `dub build --config=debug|release` in the project folder
   and streams the output into the **Dub Manager** output pane.

## Build from source

This is a classic (non-SDK) VSIX project. Build it with MSBuild from Visual Studio 2022; `dotnet build`
does not resolve the VSSDK references.

```bat
msbuild DockDAPnext\DockDAPnext.csproj -t:Restore
msbuild DockDAPnext\DockDAPnext.csproj -p:Configuration=Debug
rem or -p:Configuration=Release
```

Outputs per configuration:

- `DockDAPnext\bin\<Configuration>\DockDAPnext.dll` — the extension assembly
- `DockDAPnext\bin\<Configuration>\DockDAPnext.vsix` — the installable package

### Why the build merges assemblies

DockDAPnext uses ReactiveUI 23.x (with System.Reactive, Splat and DynamicData) and Newtonsoft.Json.
Other extensions in the same `devenv.exe` process ship their own copies of ReactiveUI **with the same
assembly identity** — ReSharper, for example, carries 18.3.1. The CLR loads one assembly per identity,
so whichever copy loads first wins, and the package used to die at load with
`TypeLoadException: Could not load type 'ReactiveUI.RxSchedulers'`.

Renaming files does not help: identity lives in the metadata, not in the file name. The
`MergeDependencies` target therefore merges those assemblies into `DockDAPnext.dll` with ILRepack, and
`ExcludeMergedFromVsix` removes the separate DLLs from the package. The result is one ~4 MB assembly
and a VSIX with 25 entries and no separate ReactiveUI/System.Reactive/Splat/DynamicData.

A few build details that matter:

- `MergeDependencies` runs after `CopyFilesToOutputDirectory` and before the VSIX is built. Moving it
  after the VSIX step packages the unmerged assembly.
- `-p:DisableMerge=true` builds the same code without merging (use `-t:Rebuild`). That control build is
  useful when you need to prove whether a problem comes from ILRepack; its VSIX carries ReactiveUI,
  System.Reactive, DynamicData and Splat* as separate files.
- Rebuilds are **not** byte-identical: ILRepack 2.0.48 has no deterministic switch and rewrites the
  merged assembly on every run. Comparing hashes only makes sense between one build and the copy
  installed from it.
- `Newtonsoft.Json` is pinned to **13.0.1** in both the package reference and the `NewtonsoftVer`
  property, because `Community.VisualStudio.Toolkit` warns (`CVSTBLD002`) above that version.
- `Microsoft.Extensions.DependencyInjection` is pinned to **9.0.0**, the version VS 2022 already has
  loaded; 10.x fails with `FileLoadException 0x80131040`.
- `Microsoft.VSSDK.BuildTools` stays on **17.0.5232**; 18.x expects newer deploy tasks than VS 2022
  provides.
- The version lives in two places and both have to change together: `Identity@Version` in
  [`source.extension.vsixmanifest`](DockDAPnext/source.extension.vsixmanifest) and `Vsix.Version` in
  [`source.extension.cs`](DockDAPnext/source.extension.cs).

## Repository layout

```
.
├─ DockDAPnext.sln
├─ LICENSE.txt
├─ README.md
└─ DockDAPnext/
   ├─ Command/            Tools → Open DockDAP command
   ├─ ToolWindows/        tool window host and its root control
   ├─ UserControls/       tab views, including projectManagersUCs/
   ├─ ViewModels/         ReactiveUI view models (partial classes per concern)
   ├─ Service/            dub.json read/write, dub.exe runner, AI client, .dproj sync
   │  ├─ Models/          DubConfigAp, BuildConfiguration, OperationResult<T>, …
   │  └─ InterFaces/      service contracts
   ├─ AppConfiguration/   dependency injection container
   ├─ Themes/             shared WPF styles
   ├─ Resources/          icons
   ├─ VSCommandTable.vsct command definitions
   └─ source.extension.vsixmanifest
```

`bin/`, `obj/`, `*.vsix`, `*.log` and the scratch files ILRepack leaves next to the project are
ignored by git.

## Known limitations

- **DUB is required to build.** Without `dub.exe` the Build tab says so and runs nothing; editing
  `dub.json` still works.
- **The package ships debug symbols.** `IncludeDebugSymbolsInVSIXContainer` is on, which makes the
  VSIX larger and keeps source paths inside the PDB. Turn it off for a public release if that matters.
- **Tested against the Experimental instance.** Behaviour next to other extensions is not verified
  beyond the ReactiveUI identity conflict, which the merge removes.
- **Nothing loads by itself.** That is deliberate: a window that reads a project on open would sync
  files before you asked for it.

## If something goes wrong

- **“The package did not load correctly.”** Check the size of `DockDAPnext.dll` in the installed
  extension folder. Around 270 KB means the merged build was not what got installed; rebuild Release
  and install again.
- **The window opens but the list is empty.** The status line says why: no D project in the solution,
  or the solution could not be read. Add a project with a `dub.json` and press **Refresh**.
- **After an update the old behaviour is still there.** Visual Studio can keep the previous copy
  loaded. Close VS, bump the version in the two places above, rebuild, install, and start VS again.

## Contributing

Issues and pull requests are welcome. For a bug report, include the VS version, the build
configuration, and any `[DOCKDAP]` lines from **Output → Debug** or the `Dub Manager` pane — those
usually point at the failing step immediately. Please keep the `MergeDependencies` /
`ExcludeMergedFromVsix` targets and the version pins above intact; they exist for reasons described
in this file.

## License

MIT — see [LICENSE.txt](LICENSE.txt). Copyright © 2026 Aripa Studio.
