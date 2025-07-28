# LaunchCore – A Windows-Only C# Game Launcher Template

A clean, customizable Windows game launcher built in C# using WPF. Supports update detection, patch downloading, and dynamic patch notes.

## 🔧 Features
- Auto-download and apply game patches
- Patch notes fetched and displayed in launcher (requires your own APIs)
- Self-relaunch on update completion

## 📦 Tech Stack
- C# (.NET Framework 4.8.2 for launcher, .NET >= 8 for launcher's self-updater)
- WinForms
- SharpZipLib (or System.IO.Compression)
- WebClient
- CefSharp

## 📸 Screenshots
<img width="1274" height="662" alt="image" src="https://github.com/user-attachments/assets/34eab068-dfa8-4d8d-bb7f-859afd1e887e" />
<img width="1275" height="632" alt="image" src="https://github.com/user-attachments/assets/84771936-96be-459b-a6ce-5241e3430f76" />

## 🧰 Getting Started
- Clone the repository
- Change the links in:
- LaunchCoreUpdater -> Program.cs -> launcherFilesLink
- LaunchCore -> MainWindow.xaml.cs -> MainWindow(), all the things to change are in capital letters.

## Things to note:
- Version files are auto-downloaded from the server, as well as the content of the last blog post (title, image-content, excerpt, link, and date)
- Version for the client is stored in Version.txt
- Version for the launcher is stored in LauncherVersion.txt, and needs to be updated for every Launcher version.

## How Auto-Updating the launcher works:
- The launcher self-updates using the Launcher_Updater.exe file that is bundled into the launcher files. 
- The launcher checks online for the version file for the launcher, if it exists and it is not the exact version number as is the one on the server, then we update the launcher by running Launcher_Updater.exe, and then closing the program. 
- The Launcher_Updater.exe then does all the updating, and opens the updated Valor_Launcher.exe.

## How Auto-Updating the client works:
- The launcher checks the server's Version.txt file to see what the version number is. If it doesn't exist, it sets the version number in the local Version.txt file to 0.0.0; if it does not exist, it just downloads the new Version.txt file and updates the client as it's a "fresh install"
- If version number < server's version number, it downloads the new client as a zip, extracts it, and deletes the old client
- It also auto-changes the button based on the state (enum) of the launcher.

# How to perform/prepare an update to the launcher
Note: All of these folders and files need to be EXACTLY named. I will make it auto do all this stuff in the future, auto download the exes, make the folders, etc. However this is easier for right now.

1. Make sure to build for `Release`.

2. Make sure there is a `Prerequisite` folder, and an `Updater` folder in Launcher folder.

3. Prerequisite folder should have `net8.0.1installer.exe`, however, the net8.0.1installer.exe file can be any net 8+ runtime installer executable. It just has to be that name. Will change in the future to just be `net8installer.exe`

4. Put all of the Updater files into the Updater folder in the Launcher folder.

5. Make sure that there is no Version.txt file in the Launcher folder, as the game will not update if it has the most up-to-date client version in Version.txt. It is better to let the launcher download the version.txt from the server and then update the client the first time.

6. Make sure that the server's LauncherVersion.txt matches the server's LauncherVersion.txt, exact same or else it will ask the user to download the update **every time**.

7. Everything should be fine after this - zip it, call it `LaunchCore.zip` explicitly (updater will not work otherwise), upload the launcher and go! 

## 📄 License
MIT
