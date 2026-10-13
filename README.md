# PorksLauncher

This C# application keeps a central list of applications to reduce clutter. It launches
executables (`.exe`), runnable Java applications (`.jar`), and other files through
their Windows file associations.

# Current features

- Add, remove, and rename applications in the list
- Launch `.exe` and runnable `.jar` files
- Open other file types with their Windows default application

# Future features

- Add settings
  - Change color scheme
  - Resize list box
  - Enable/Disable auto prompt
  - Enable/Disable auto executable scan
- Auto prompt user when launching an application to see if they want to add exectuable to the list.
- Scan for exetuables automatically
- Alphabetical sorting 

# How to use
* Add an application
    - Click Add App to select an `.exe` or `.jar` file.
    - Choose All files to add another file type with a Windows file association.
    - Double click an entry to launch it.

* Remove an application
    - Select an entry, click Remove App, and confirm with Yes.

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


![screenshot](https://github.com/Adam-OGithub/PorksLauncher/blob/main/images/demo/main_app_screenshot.png?raw=true)
