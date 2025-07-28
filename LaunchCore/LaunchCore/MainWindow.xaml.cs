using CefSharp;
using CefSharp.Wpf;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using System.Xml.Linq;

namespace LaunchCore
{
    internal enum LauncherStatus
    {
        Ready,
        Failed,
        DownloadingGame,
        DownloadingUpdate
    }

    public partial class MainWindow
    {
        private string _rootPath;
        private string _gamePath;
        private string _versionFile;
        private string _gameZip;
        private string _gameExe;
        private string _gameURI;
        private string _clientVersionAPILink;
        private string _launcherVersionAPILink;
        private string _launcherVersionFile;

        // The link to the blog posts API
        private const string BlogPostsLink = "BLOG POSTS API LINK HERE";

        private LauncherStatus _status;

        private LauncherStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                switch (_status)
                {
                    case LauncherStatus.Ready:
                        PlayButton.FontSize = 32;
                        PlayButton.Content = "Play";
                        break;
                    case LauncherStatus.Failed:
                        PlayButton.FontSize = 12;
                        PlayButton.Content = "Update Failed - Retry";
                        break;
                    case LauncherStatus.DownloadingGame:
                        PlayButton.FontSize = 14;
                        PlayButton.Content = "Downloading Game...";
                        break;
                    case LauncherStatus.DownloadingUpdate:
                        PlayButton.FontSize = 14;
                        PlayButton.Content = "Downloading Update...";
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            // Please enter in the correct path names for everything
            _rootPath = AppDomain.CurrentDomain.BaseDirectory;
            _gamePath = Path.Combine(_rootPath, "GAME FOLDER NAME HERE");
            _versionFile = Path.Combine(_rootPath, "Version.txt");
            _gameZip = Path.Combine(_rootPath, "Build.zip");
            _gameExe = Path.Combine(_rootPath, "GAME FOLDER NAME HERE", "GAME EXE NAME HERE");
            _gameURI = "GAME CLIENT DOWNLOAD LINK";
            _clientVersionAPILink = "CLIENT VERSION API LINK HERE";
            _launcherVersionAPILink = "LAUNCHER VERSION API LINK HERE";
            _launcherVersionFile = Path.Combine(_rootPath, "LauncherVersion.txt");

            if (Directory.Exists(Path.Combine(_rootPath, "UpdaterOld")))
                Directory.Delete(Path.Combine(_rootPath, "UpdaterOld"), true);

            var jsonData = FetchJsonDataAsync(BlogPostsLink);
            var blogPosts = ParseJson(ExtractPostsJson(jsonData));
            string htmlString = string.Empty;

            if (blogPosts != null)
                htmlString = AddBlogPostsToHtml(blogPosts.ToList());

            Browser.LoadHtml(htmlString);

            LoadingScreenFade();
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            LauncherUpdate();
            ClientUpdate();
            _ = UpdatePlayerCount();
        }

        private void LauncherUpdate()
        {
            if (File.Exists(_launcherVersionFile))
                LauncherVersionText.Text = File.ReadAllText(_launcherVersionFile).TrimEnd('\n');
            else
                LauncherVersionText.Text = "N/A";

            var localVersion = "";
            var onlineVersion = "";

            using (var client = new WebClient())
            {
                if (File.Exists(_launcherVersionFile))
                    localVersion = File.ReadAllText(_launcherVersionFile).TrimEnd('\n');
                else
                    localVersion = "N/A";

                onlineVersion = GetLauncherVersionFromServer();
                if (localVersion == "N/A" || int.Parse(localVersion.Split('.')[2]) < int.Parse(onlineVersion.Split('.')[2]) || localVersion == onlineVersion)
                    return;
            }
            var userChoice = MessageBox.Show($"Version {onlineVersion} of the launcher is available!\n" +
                $"Would you like to update from version {localVersion}?", $"New Update - {onlineVersion}", MessageBoxButton.YesNo);
            if (userChoice != MessageBoxResult.Yes)
                return;

            //Checking if the .net 8.0.1 runtime is installed, if not run the installer
            bool isDotNetInstalled;
            using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedhost"))
            {
                if (key != null)
                    isDotNetInstalled = key.GetValue("Version") is string installedVersion && installedVersion.StartsWith("8.");
                else
                    isDotNetInstalled = false;
            }

            if (!isDotNetInstalled)
            {
                Process process = new Process
                {
                    StartInfo = new ProcessStartInfo(Path.Combine(Path.Combine(_rootPath, "Prerequisites"), "net8.0.1installer.exe"))
                };
                MessageBoxResult userResult = MessageBox.Show(".NET version 8 or higher is not installed, it is needed for the updater.\n" +
                    "The installer will start after pressing YES. If you press NO, updating will cancel.", ".NET 8 Runtime Needed", MessageBoxButton.YesNo);

                if (userResult != MessageBoxResult.Yes)
                    return;

                try
                {
                    process.Start();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error running .net installer: {ex.Message}");
                }

                while (!process.HasExited)
                {
                    if (!process.WaitForExit(5000))
                        continue;
                }

                MessageBox.Show("Installation closed, loading the updater!" +
                    "\nIf the installation was not successful, the updater will not open.", "Installation Result", MessageBoxButton.OK);
            }

            Directory.Move(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Updater"), 
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UpdaterOld"));
            Process.Start(new ProcessStartInfo(Path.Combine(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UpdaterOld"), "Launcher_Updater.exe"))
            {
                WorkingDirectory = Path.Combine(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UpdaterOld"), "Launcher_Updater.exe"),
                Verb = "runas"
            });
            Close();
        }

        private void ClientUpdate()
        {
            if (File.Exists(_versionFile))
            {
                var localVersion = new Version(File.ReadAllText(_versionFile));
                VersionText.Text = localVersion.ToString();
                try
                {
                    var client = new WebClient
                    {
                        CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore)
                    };
                    var onlineVersion = new Version(GetClientVersionFromServer());
                    if (onlineVersion.IsDifferentThan(localVersion))
                    {
                        InstallGameFiles(true, onlineVersion);
                    }
                    else
                    {
                        Status = LauncherStatus.Ready;
                    }
                }
                catch (Exception ex)
                {
                    Status = LauncherStatus.Failed;
                    MessageBox.Show($"Error checking for game updates: {ex}");
                }
            }
            else
            {
                InstallGameFiles(false, Version.Zero);
            }
        }

        private void LoadingScreenFade()
        {
            var fadeOutStoryboard = new Storyboard();
            var fadeOutAnimation = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = new Duration(TimeSpan.FromSeconds(3)),
                EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseIn, Exponent = 20 }
            };
            fadeOutAnimation.Completed += FadeOutAnimation_Completed;
            Storyboard.SetTarget(fadeOutAnimation, LoadingImage);
            Storyboard.SetTargetProperty(fadeOutAnimation, new PropertyPath("Opacity"));
            fadeOutStoryboard.Children.Add(fadeOutAnimation);
            Resources.Add("FadeOutStoryboard", fadeOutStoryboard);
            var storyboard = FindResource("FadeOutStoryboard") as Storyboard;
            storyboard?.Begin(LoadingImage);
        }

        public class BlogPost
        {
            public string title { get; set; }
            public string url { get; set; }
            public string feature_image { get; set; }
            public string excerpt { get; set; }
        }

        private string AddBlogPostsToHtml(List<BlogPost> blogPosts)
        {
            var counter = 0;
            var htmlCode = File.ReadAllText(@".\main.html");

            foreach (var post in blogPosts)
            {
                counter++;
                htmlCode = htmlCode.Replace(string.Concat("{BLOG_TITLE_", counter, "}"), post.title);
                htmlCode = htmlCode.Replace(string.Concat("{BLOG_TITLE_LINK_", counter, "}"), post.url);
                htmlCode = htmlCode.Replace(string.Concat("{BLOG_CONTENT_", counter, "}"), post.excerpt);
                htmlCode = htmlCode.Replace(string.Concat("{BLOG_IMAGE_", counter, "}"), post.feature_image);
            }
            return htmlCode;
        }

        private IEnumerable<BlogPost> ParseJson(string jsonData)
        {
            return JsonConvert.DeserializeObject<List<BlogPost>>(jsonData);
        }

        private class WebClientNew : System.Net.WebClient
        {
            public int Timeout { get; set; }

            protected override WebRequest GetWebRequest(Uri uri)
            {
                WebRequest lWebRequest = base.GetWebRequest(uri);
                lWebRequest.Timeout = Timeout;

                if (lWebRequest is HttpWebRequest httpWebRequest)
                    httpWebRequest.ReadWriteTimeout = Timeout;

                return lWebRequest;
            }
        }

        private string FetchJsonDataAsync(string url)
        {
            using (var client = new WebClientNew())
            {
                client.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);
                try
                {
                    client.Timeout = 15000; // 15s
                    Console.WriteLine("Downloading latest blog post... Timeout in 15 seconds.");
                    return client.DownloadString(url);
                }
                catch (Exception)
                {
                    return string.Empty;
                }
            }
        }

        private string ExtractPostsJson(string jsonString)
        {
            try
            {
                var jsonObject = JObject.Parse(jsonString);
                var postsArray = jsonObject["posts"]?.ToString();

                return postsArray ?? string.Empty;
            }
            catch (Exception)
            {
                MessageBox.Show($"Could not retrieve latest blog post. This is probably our issue, please report this to a developer!");
                return string.Empty;
            }
        }

        private void FadeOutAnimation_Completed(object sender, EventArgs e)
        {
            LoadingImage.Visibility = Visibility.Collapsed;
        }

        private void InstallGameFiles(bool isUpdate, Version onlineVersion)
        {
            try 
            {
                using (var client = new WebClient())
                {
                    client.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);
                    if (isUpdate)
                    {
                        Status = LauncherStatus.DownloadingUpdate;
                    }
                    else
                    {
                        Status = LauncherStatus.DownloadingGame;
                        onlineVersion = new Version(GetClientVersionFromServer());
                    }

                    client.DownloadFileCompleted += DownloadGameCompletedCallback;
                    if (Directory.Exists(_gamePath))
                        Directory.Delete(_gamePath, true);
                    client.DownloadFileAsync(new Uri(_gameURI), _gameZip, onlineVersion);
                }
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.Failed;
                MessageBox.Show($"Error installing game files: {ex}");
            }
        }

        private void DownloadGameCompletedCallback(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                var onlineVersion = ((Version)e.UserState).ToString();
                
                if (Directory.Exists(_gamePath))
                    Directory.Delete(_gamePath, true);

                ZipFile.ExtractToDirectory(_gameZip, _rootPath);
                File.Delete(_gameZip);

                File.WriteAllText(_versionFile, onlineVersion);

                VersionText.Text = onlineVersion;
                Status = LauncherStatus.Ready;
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.Failed;
                MessageBox.Show($"Error finishing download: {ex}");
            }
        }
        
        private async Task UpdatePlayerCount()
        {
            using (var client = new HttpClient())
            {
                try
                {
                    var response = await client.GetAsync("API HERE FOR PLAYER COUNT NUMBER");
                    if (!response.IsSuccessStatusCode)
                        throw new Exception("Could not retrieve player count.");
                    
                    var playerCount = await response.Content.ReadAsStringAsync();

                    PlayerCountNumberLabel.Dispatcher.Invoke(() =>
                    {
                        PlayerCountNumberLabel.Text = playerCount.Trim();
                    });
                }
                catch (Exception ex)
                {
                    PlayerCountNumberLabel.Dispatcher.Invoke(() =>
                    {
                        PlayerCountNumberLabel.Text = "Error retrieving player count.";
                    });
                    Console.WriteLine(ex.Message);
                }
            }
        }

        public string GetClientVersionFromServer()
        {
            VersionJsonData jsonData;
            try
            {
                jsonData = JsonConvert.DeserializeObject<VersionJsonData>
                    (FetchJsonDataAsync(_clientVersionAPILink));

                if (jsonData == null)
                {
                    MessageBox.Show($"Error checking for client version, client version URL was not correct!");
                    return "N/A";
                }

                return jsonData.Version;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error checking for client version: {ex}");
                return "Error";
            }
        }

        public string GetLauncherVersionFromServer()
        {
            VersionJsonData jsonData;
            try
            {
                jsonData = JsonConvert.DeserializeObject<VersionJsonData>
                    (FetchJsonDataAsync(_launcherVersionAPILink));

                if (jsonData == null)
                {
                    MessageBox.Show($"Error checking for launcher version, launcher version URL was not correct!");
                    return "N/A";
                }

                return jsonData.Version;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error checking for launcher version: {ex}");
                return "Error";
            }
        }

        public class VersionJsonData
        {
            public string Version { get; set; }
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(_gameExe) && Status == LauncherStatus.Ready)
            {
                Process process = new Process
                {
                    StartInfo = new ProcessStartInfo(_gameExe) { WorkingDirectory = Path.Combine(_rootPath, "Game") }
                };

                try
                {
                    process.Start();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error running the game: {ex.Message}");
                }

                Hide();

                //Checks to see whether the game exe is still open
                while (!process.HasExited)
                {
                    if (!process.WaitForExit(5000))
                        continue;
                }

                Show();
            }
            else if (Status == LauncherStatus.Failed)
            {
                ClientUpdate();
            }
        }

        private void DiscordButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("LINK TO DISCORD");
        }

        private void LatestUpdatesButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("LINK TO BLOG POSTS");
        }

        private void GuildLookupButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("LINK TO GUILD SEARCH ETC");
        }

        private void PlayerLookupButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("LINK TO PLAYER LOOKUP");
        }

        private void LeaderboardButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("LINK TO LEADERBOARD");
        }

        private void WebsiteButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("LINK TO WEBSITE");
        }

        private void WikiButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("LINK TO WIKI");
        }

        private void ShopButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("LINK TO SHOP");
        }

        private void FAQButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("LINK TO FAQ");
        }

        private void BanAppealButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("LINK TO BAN APPEAL FORM");
        }

        private void BanTrackerButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("LINK TO BAN TRACKER");
        }
    }

    internal struct Version
    {
        internal static Version Zero = new Version(0, 0, 0);

        private short _major;
        private short _minor;
        private short _subMinor;

        private Version(short major, short minor, short subMinor)
        {
            _major = major;
            _minor = minor;
            _subMinor = subMinor;
        }

        internal Version(string version)
        {
            var versionStrings = version.Split('.');
            if (versionStrings.Length != 3)
            {
                _major = 0;
                _minor = 0;
                _subMinor = 0;
                return;
            }

            _major = short.Parse(versionStrings[0]);
            _minor = short.Parse(versionStrings[1]);
            _subMinor = short.Parse(versionStrings[2]);
        }

        internal bool IsDifferentThan(Version otherVersion)
        {
            if (_major != otherVersion._major)
            {
                return true;
            }
            else
            {
                if (_minor != otherVersion._minor)
                {
                    return true;
                }
                else
                {
                    if (_subMinor != otherVersion._subMinor)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public override string ToString()
        {
            return $"{_major}.{_minor}.{_subMinor}";
        }
    }

}
