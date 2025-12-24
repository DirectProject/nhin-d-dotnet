# Direct Project Installer - Build Instructions

Last Updated: December 21, 2025

## Prerequisites

### 1. **Inno Setup 6** (Required)
The NuGet package for Inno Setup is no longer reliable with modern project styles. Install manually:

- **Download**: https://jrsoftware.org/isdl.php
- **Version**: Inno Setup 6.x (Unicode version recommended)
- **Install Location**: `C:\Program Files (x86)\Inno Setup 6\` (default)

The build script will automatically detect Inno Setup in these locations:
1. `C:\Program Files (x86)\Inno Setup 6\iscc.exe`
2. `C:\Program Files\Inno Setup 6\iscc.exe`
3. `build\packages\Tools.InnoSetup.5.5.4\tools\iscc.exe` (legacy)

### 2. **MSBuild / Visual Studio**
- Visual Studio 2019 or later (or MSBuild tools)
- MSBuild must be in your PATH
- Use a **Developer Command Prompt** or **Developer PowerShell**

### 3. **NuGet** (included)
- Already present at: `build\.nuget\nuget.exe`

---

## Quick Start - Build the Installer

### **For Testing (No Source Control Operations)**
```bat
cd c:\Source\GitHub\DirectProject\nhin-d-dotnet-framework\csharp\installer
build-installer.bat test Debug
```

### **For Release Build**
```bat
cd c:\Source\GitHub\DirectProject\nhin-d-dotnet-framework\csharp\installer
build-installer.bat test Release
```

### **For Production (With Mercurial Tagging)**
```bat
cd c:\Source\GitHub\DirectProject\nhin-d-dotnet-framework\csharp\installer
build-installer.bat
```
⚠️ This will commit version changes and create Mercurial tags

---

## Build Process Details

### What Happens During Build

1. **Prompts for Version** - Enter version (e.g., `2.0.0.0`)
2. **Cleans Build Artifacts** - Removes old `bin` directory
3. **Prepares Installer** via `msbuild ..\build.xml -t:prepare-installer`:
   - Cleans all projects
   - Builds gateway components (x64 only)
   - Publishes ConfigUI web application
4. **Updates Version Info** in:
   - `GlobalAssemblyInfo.cs`
   - `Direct.iss`
5. **Restores NuGet Packages** for the solution
6. **Compiles Installer** using Inno Setup
7. **Creates Output**: `Direct-{VERSION}-NET45_Beta.exe`

### Command Line Options

```bat
build-installer.bat [mode] [configuration]
```

**Mode:**
- *(empty)* - Full build with source control operations
- `test` - Build only, skip Mercurial commit/tagging

**Configuration:**
- `Debug` (default)
- `Release`

**Examples:**
```bat
build-installer.bat                    # Debug build with hg operations
build-installer.bat test               # Debug build, no hg
build-installer.bat test Release       # Release build, no hg
build-installer.bat Release Release    # Release build with hg operations
build-installer.bat help               # Show help
```

---

## Manual Build (Advanced)

If you need more control over the build process:

```bat
cd c:\Source\GitHub\DirectProject\nhin-d-dotnet-framework\csharp

REM Step 1: Prepare (build all components)
msbuild build.xml /p:Configuration=Debug /t:prepare-installer

REM Step 2: Build the installer
cd installer
msbuild installer-build.xml /p:VERSION=2.0.0.0 /p:Configuration=Debug /t:build-installer
```

### **Customizing .NET Target Framework**

The installer supports different .NET versions. By default it uses `net48`. To build for a different target framework:

```bat
cd installer
"C:\Program Files (x86)\Inno Setup 6\iscc.exe" Direct.iss /DConfiguration=Debug /DNetVersion=net8.0
```

Or update the default in [`Direct.iss`](Direct.iss) lines 26-28:
```pascal
#ifndef NetVersion
# define NetVersion "net48"  # Change to "net8.0", "net10.0", etc.
#endif
```

---

## Troubleshooting

### "Inno Setup compiler not found"
**Solution**: Install Inno Setup 6 from https://jrsoftware.org/isdl.php

### "MSBuild is not recognized"
**Solution**: Use **Developer Command Prompt** or **Developer PowerShell** from Visual Studio

### Build fails on `prepare-installer`
**Solution**: Check that all project dependencies are restored:
```bat
cd c:\Source\GitHub\DirectProject\nhin-d-dotnet-framework\csharp\build
.nuget\nuget restore DirectProject.sln
```

### Old packages folder not needed?
**Correct!** Modern SDK-style projects use a global NuGet cache. However, this project still uses `packages.config` for some components, so the `build\packages` folder is still used for legacy dependencies like xUnit, Entity Framework, etc.

---

## Project Structure

**Key Files:**
- [`build-installer.bat`](build-installer.bat) - Main build script (entry point)
- [`installer-build.xml`](installer-build.xml) - MSBuild tasks for installer compilation
- [`Direct.iss`](Direct.iss) - Inno Setup installer definition (2973 lines)
- [`../build.xml`](../build.xml) - Main build file with `prepare-installer` target
- [`../GlobalAssemblyInfo.cs`](../GlobalAssemblyInfo.cs) - Shared assembly version info

**Output Location:**
- Installer executable: `installer\Direct-{VERSION}-NET45_Beta.exe`
- Source archive (if tagged): `installer\Direct-{VERSION}-NET45-Source.zip`

---

## Architecture Notes

### Platform Support
Currently builds **x64 only**. The 32-bit build is commented out in `build.xml` line 210:

```xml
<!-- OLD: build-gateway32;build-gateway64 -->
<Target Name="prepare-installer" DependsOnTargets="clean;build-gateway64;publish-configui" />
```

To enable 32-bit support, uncomment line 209 in `build.xml`.

### Components Included
The installer can install these components (configured in `Direct.iss`):
- **DNS Responder** - DNS resolution service
- **DNS Web Service** - DNS management API
- **Config Web Services** - Configuration management API
- **UI Web Admin** - Administrative web interface
- **Direct Gateway** - SMTP gateway
- **Monitor Server** - Monitoring service
- **DirectConfig Database** - Configuration database

---

## Getting Help

```bat
build-installer.bat help
```

Shows usage information and examples.
