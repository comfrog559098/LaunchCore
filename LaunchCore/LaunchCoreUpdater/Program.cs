using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Net.Cache;
using static System.Runtime.InteropServices.JavaScript.JSType;

//Setup file names and dir names
DirectoryInfo directoryInfo = new(AppDomain.CurrentDomain.BaseDirectory);
var rootPath = "";
try
{
    rootPath = directoryInfo.Parent.FullName;
}
catch (Exception ex)
{
    Console.WriteLine("Error with finding the launcher folder: " + ex.ToString());
}

const string launcherFilesLink = "LINK TO YOUR LAUNCHER FILES URL HERE";
var launcherZip = "LaunchCore.zip";
var launcherTempFolder = Path.Combine(rootPath, "Temp");




//Setup files to keep and files in root BEFORE downloading the launcher update.
string[] filesToKeep = ["LauncherVersion.txt", "userData.xml"];
var filesInRoot = Directory.GetFiles(rootPath);




//1. Create the temp folder
Console.WriteLine("Updater started.");
Console.WriteLine("Creating Temp folder if it doesn't exist...");
if (!Directory.Exists(launcherTempFolder))
    Directory.CreateDirectory(launcherTempFolder);




//2. download the launcher files as LaunchCore.zip, put into ./Temp
using (var client = new WebClient())
{
    Console.WriteLine("Downloading new launcher files (this may take a minute)...");
    client.DownloadFile(launcherFilesLink, Path.Combine(launcherTempFolder, launcherZip));
    Console.WriteLine("Launcher files downloaded!");
}




//3. Check if we're even okay to start deleting (check if LaunchCore is still open)
//   Using a loop method for this, check if launcher open, if it is, retry, 6 retries, 10 sec intervals.
Console.WriteLine("Checking if launcher is open...");
var retryCount = 0;
const string processName = "LaunchCore";
var processes = Process.GetProcessesByName(processName);

while (processes.Length > 0)
{
    if (retryCount >= 6)
    {
        Console.WriteLine("Maximum retries reached. Update cannot proceed.");
        return;
    }

    Console.WriteLine($"{processName} is still running. Please close it to continue with the update.");
    Console.WriteLine($"Waiting for 5 seconds before retrying... (Attempt {retryCount + 1} of 6)");

    Thread.Sleep(5 * 1000);
    retryCount++;
}




//4. Start deleting old files!
try
{
    Console.WriteLine("Deleting old files...");
    if (filesInRoot.Select(x => x == "!LaunchCore.exe").ToList().Count > 0)
    {
        foreach (var file in filesInRoot)
        {
            var fileName = Path.GetFileName(file);

            if (!filesToKeep.Contains(fileName))
            {
                File.Delete(rootPath + $"/{fileName}");
                if (!File.Exists(file))
                    Console.WriteLine($"Deleted:\t{fileName}");
            }
        }
    }
    else
    {
        Console.WriteLine("Launcher was in the wrong directory. Exiting. Please open a ticket and let Comfrog know about this!");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error attempting to delete old files: {ex}");
}

Console.WriteLine($"Old files deleted successfully!");




//5. Extract the zip from launcherTempFolder into the root folder
Console.WriteLine("Extracting new launcher files...");
ZipFile.ExtractToDirectory(Path.Combine(launcherTempFolder, launcherZip), rootPath, true);
Console.WriteLine("Launcher files extracted successfully!");




//6. Update LauncherVersion.txt
Console.WriteLine("Updating launcher version...");
using (var client = new WebClient())
{
    client.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);
    var version = client.DownloadString("https://client-api.realmdex.workers.dev/api/launcher/version");

    //Extract the version number from the json data without json :scheming:
    int start = version.IndexOf("\"version\":\"") + "\"version\":\"".Length;
    int end = version.IndexOf("\",\"", start);
    string versionFinal = version.Substring(start, end - start);

    File.WriteAllText(rootPath + "/LauncherVersion.txt", versionFinal);
}




//6. Clean up the temp folder + zip file if it exists still for some reason
Console.WriteLine("Cleaning up...");
if (File.Exists(Path.Combine(launcherTempFolder, launcherZip)))
    File.Delete(Path.Combine(launcherTempFolder, launcherZip));

if (Directory.Exists(launcherTempFolder))
    Directory.Delete(launcherTempFolder, true);

Console.WriteLine("Clean-up completed!");




//7. Rename LaunchCore.exe to !LaunchCore.exe
if (File.Exists(Path.Combine(rootPath, "LaunchCore.exe")))
    File.Move(Path.Combine(rootPath, "LaunchCore.exe"), Path.Combine(rootPath, "!LaunchCore.exe"));




//8. Run the new launcher
Console.WriteLine("Running updated launcher.");
Process.Start(new ProcessStartInfo(Path.Combine(rootPath, "!LaunchCore.exe"))
{
    WorkingDirectory = Path.Combine(rootPath),
    Verb = "runas"
});




//9. Close the updater (this) because update was successful.
Console.WriteLine("Update successful!");
Console.WriteLine("Closing the updater...");
Environment.Exit(0);