# LaunchCore – A Windows-Only Game Launcher Template

**LaunchCore** is a production-ready, customizable game launcher for Windows built with C#. Designed to simplify game distribution and updating, it includes built-in version tracking, auto-updating (both launcher and client), and patch note delivery.

---

## ✨ Features

- Auto-download and apply client patches
- Self-updating launcher using a bundled updater executable
- Patch notes fetched and displayed via configurable blog endpoints
- Dynamic launch button text/states based on patching state
- Fully customizable and lightweight

---

## 🧰 Tech Stack

- C# (.NET Framework 4.8.2 for launcher, .NET 8+ for updater)
- WinForms
- SharpZipLib / System.IO.Compression
- WebClient
- CefSharp (embedded browser support)

---

## 📸 Screenshots

<img width="1274" height="662" alt="Launcher UI" src="https://github.com/user-attachments/assets/34eab068-dfa8-4d8d-bb7f-859afd1e887e" />
<img width="1275" height="632" alt="Patch Notes UI" src="https://github.com/user-attachments/assets/84771936-96be-459b-a6ce-5241e3430f76" />

---

## 🚀 Getting Started

1. Clone the repository
2. Modify the following:
   - `LaunchCoreUpdater/Program.cs` → `launcherFilesLink`
   - `LaunchCore/MainWindow.xaml.cs` → Replace placeholders in **ALL CAPS**
3. Build and test

---

## 📌 Versioning Logic

- `Version.txt` stores the current **game client** version
- `LauncherVersion.txt` stores the current **launcher** version
- These files are checked at runtime against server copies to determine if an update is needed

---

## 🔄 How Launcher Auto-Updating Works

1. Launcher compares `LauncherVersion.txt` with server's version
2. If out of date, it launches `LaunchCoreUpdater.exe`, closes itself, and replaces its files
3. The updater then relaunches the new `LaunchCore.exe`

---

## 🔁 How Game Auto-Updating Works

1. Compares local `Version.txt` with server’s version
2. If newer version found, downloads new client `.zip`, extracts it, deletes the old version
3. Updates button state and version file automatically

---

## 🛠️ Preparing an Update Release

1. Build in `Release` mode
2. Create two folders in the launcher build directory:
   - `Prerequisite/` → must contain `net8.0.1installer.exe`
   - `Updater/` → contains all updater files
3. Ensure `Version.txt` is **not** present in final build (it will be downloaded)
4. Match `LauncherVersion.txt` exactly between local and server copies
5. Zip all files and name the archive **`LaunchCore.zip`**

> ⚠️ The updater expects that filename explicitly. This will be configurable in a future version.

---

## 📝 License

[MIT](LICENSE)

---

## 📣 Notes

- Blog/patch notes are fetched dynamically from your API endpoint (not included)
- Naming conventions are strict for now but planned to be configurable
- This project is actively maintained and used in production environments
