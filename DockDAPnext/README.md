# DockDAPnext — project folder

Source of the DockDAPnext Visual Studio 2022 extension. Features, requirements, installation, usage
and the full build instructions (including why the build merges ReactiveUI with ILRepack) are in the
[repository README](../README.md).

Quick build, from the repository root:

```bat
msbuild DockDAPnext\DockDAPnext.csproj -t:Restore
msbuild DockDAPnext\DockDAPnext.csproj -p:Configuration=Release
```

The VSIX lands in `bin\Release\DockDAPnext.vsix` next to this file.
