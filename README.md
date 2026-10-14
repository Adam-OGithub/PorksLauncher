# PorksLauncher

This C# application keeps a central list of applications to reduce clutter. It launches
executables (`.exe`), runnable Java applications (`.jar`), and other files through
their Windows file associations.

# Current features

- Add, remove, and rename applications in the list
- Launch `.exe` and runnable `.jar` files
- Open other file types with their Windows default application
- Scan running desktop applications, excluding Windows system and built-in applications
- Remember declined executables across restarts and manage them in Settings
- Choose Original, Light, Dark, or Forest color schemes
- Resize the application list through Settings or by dragging the main window
- Enable optional alphabetical sorting

# Future features

<<<<<<< Updated upstream
- Add settings
  - Change color scheme
  - Resize list box
  - Enable/Disable auto executable scan
  - Alphabetical sorting 
=======
- Auto prompt user when launching an application to see if they want to add exectuable to the list.
>>>>>>> Stashed changes

# How to use
* Add an application
    - Click Add App to select an `.exe` or `.jar` file.
    - Choose All files to add another file type with a Windows file association.
    - Double click an entry to launch it.

* Remove an application
    - Select an entry, click Remove App, and confirm with Yes.
    - The executable is also marked as denied so the scanner will not suggest it again.
    - Uncheck it in Settings → Denied executables and click Save to allow future prompts.

* Rename an application
    - Select an entry, click Rename App, enter a new name, then click OK.

## Java applications

JAR files launch with `java.exe -jar "path\to\application.jar"`. Java is searched
for in `%JAVA_HOME%\bin` first, then in `PATH`. Install a compatible Java runtime
and configure either variable, then restart PorksLauncher. JAR launching does not
depend on a Windows `.jar` file association. The JAR must be runnable and declare
a `Main-Class` in its manifest; library JARs cannot be launched on their own.
See the [Java launcher documentation](https://docs.oracle.com/en/java/javase/21/docs/specs/man/java.html).

Applications start with their containing folder as the working directory. Java
applications can display a console window. Automatic scanning continues to detect
running executables; add JAR files manually using Add App.

## Automatic scanning and denied executables

In Settings, check Enable automatic executable scan and click Save. The scanner checks visible
desktop applications every three seconds. Windows installation folders, Windows
shell processes, built-in Windows app packages, and the launcher itself are ignored.
Third-party apps such as Discord, Steam, and Notepad++ remain eligible for prompts.

Choosing Yes saves the executable to your list. Choosing No saves a denial in the
SQLite database, so repeated processes and application restarts do not prompt again.
Entries already in your list are recognized by executable path, even after renaming.
Scanning pauses while Settings or its Denied executables form is open.

Open **Settings → Denied executables** to change saved decisions. Checked entries
are blocked from prompting. Uncheck an entry and click Save to allow it to ask again
on the next scan if the app is still running. Unchecked entries remain in the list
so you can check them again later. Cancel discards unsaved checkbox changes. Windows
system applications remain excluded automatically. You can still add files manually.

## Appearance, list size, and sorting

Settings offers Original, Light, Dark, and Forest color schemes, list width and height
in pixels, the executable scan checkbox, and alphabetical sorting. Click Save to
apply and remember changes; Cancel leaves your saved choices intact. Selecting a
theme previews it in the settings dialog before saving.

You can also drag the main window border to resize the list. That size is remembered
when you finish dragging. Alphabetical sorting keeps additions from the file picker
and scanner in A–Z order, and updates the order after renaming. Turning sorting off
returns to the order in which entries were added. All these preferences persist
across restarts in SQLite.

# Data storage

Launcher entries and settings are stored in a SQLite database at
`%AppData%\pork_launcher\launcher.db`. The database is created automatically on startup;
no database server is needed.

On the first successful startup, existing `launch.csv` and `settings.csv` files in
the same folder are imported together in a transaction. Their original files are
left intact as backups, and subsequent changes are saved only to SQLite. Import
runs once, including when there are no CSV files. Malformed rows stop the import
and show an error; fix the original CSV and restart to retry.

Each launcher has a separate ID, executable path, and display name. Renaming or
removing an entry affects only that entry, even when names are duplicated. The
automatic process scanner checks saved executable paths so renamed entries are
still recognized. New entries and names can contain commas and apostrophes.

# Building

## Downloadable Windows installer

Distribute `PorksLauncher-Setup-<version>.exe` as an asset on a GitHub Release.
Users run setup, then launch PorksLauncher from the Start menu or optional desktop
shortcut. They do not need Visual Studio, the source code, or a separate database
installation. Setup installs the app for the current user and provides an uninstaller.
Upgrading or uninstalling preserves the launcher database in `%AppData%\pork_launcher`.

Setup supports Windows 10 and Windows 11. It bundles Microsoft's offline .NET
Framework 4.8 installer and runs it only if .NET Framework 4.7.2 or later is absent.
That prerequisite may request administrator access or a restart. Java is still
needed when launching Java applications, and can be configured through `JAVA_HOME`
or `PATH` as described above.

To build setup, install [Inno Setup 6 or 7](https://jrsoftware.org/isdl.php), then run:

```powershell
powershell -ExecutionPolicy Bypass -File Installer/Build-Installer.ps1
```

The first build downloads the official .NET Framework runtime and verifies its
Microsoft signature. Generated files stay under `bin` and are excluded from Git.
The finished installer and its SHA-256 checksum are written to `bin\releases`.
Version defaults to the application's file version in `Properties\AssemblyInfo.cs`.
Update that version before building a new release. The installer includes the
offline prerequisite, so its download is larger than the portable ZIP.

On a machine where PorksLauncher is not already installed, an optional installer
test installs a temporary copy under `bin`, runs the regression checks against its
installed files, and uninstalls that copy. It writes the normal Windows uninstall
registration while testing and requires .NET Framework to be already installed:

```powershell
powershell -ExecutionPolicy Bypass -File Installer/Test-Installer.ps1 -InstallerPath bin/releases/PorksLauncher-Setup-1.0.0.exe
```

## Building from source

Open `PorksLauncher.sln` in Visual Studio with the .NET desktop development workload
and .NET Framework 4.7.2 targeting pack installed. Restore NuGet packages before
building. The project uses `Microsoft.Data.Sqlite`, which includes the SQLite
runtime. Distribute the entire build output folder, including its dependency DLLs
and native runtime folders, rather than only `PorksLauncher.exe`.

The SDK project format also supports building from a terminal with a current .NET
SDK (the application still targets .NET Framework 4.7.2):

```powershell
dotnet build PorksLauncher.sln --configuration Release
```

To verify persistence, legacy migration, and EXE/JAR launch configuration using
isolated test files (Java is not needed for these checks):

```powershell
dotnet build Tests/PorksLauncher.StorageTests.csproj --configuration Debug
& .\Tests\bin\Debug\net472\PorksLauncher.StorageTests.exe
```

With a JDK's tools on `PATH`, also test launching an actual runnable JAR:

```powershell
New-Item -ItemType Directory -Force Tests/bin/Debug/net472/jar-probe | Out-Null
javac -d Tests/bin/Debug/net472/jar-probe Tests/Fixtures/JarLaunchProbe.java
jar --create --file "Tests/bin/Debug/net472/Java probe, test.jar" --main-class JarLaunchProbe -C Tests/bin/Debug/net472/jar-probe .
& .\Tests\bin\Debug\net472\PorksLauncher.StorageTests.exe "Tests/bin/Debug/net472/Java probe, test.jar"
```


## Main app list
![screenshot](https://github.com/Adam-OGithub/PorksLauncher/blob/main/images/demo/main_app_screenshot.png?raw=true)

## Auto scan application to add
![screenshot](https://github.com/Adam-OGithub/PorksLauncher/blob/main/images/demo/add_application.png?raw=true)

## Denied Executables list
![screenshot](https://github.com/Adam-OGithub/PorksLauncher/blob/main/images/demo/denied_exe_list.png?raw=true)

## Settings Menu
![screenshot](https://github.com/Adam-OGithub/PorksLauncher/blob/main/images/demo/setting_menu.png?raw=true)
