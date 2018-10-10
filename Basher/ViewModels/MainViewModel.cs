namespace Basher.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;

    using Basher.Helpers;
    using Basher.Models;
    using Basher.Services;

    using GalaSoft.MvvmLight;
    using GalaSoft.MvvmLight.Messaging;
    using GalaSoft.MvvmLight.Threading;

    using Windows.Storage;
    using Windows.System;
    using Windows.UI;
    using Windows.UI.Core;
    using Windows.UI.ViewManagement;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Media;
    using Windows.UI.Xaml.Media.Imaging;

    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para> See http://www.mvvmlight.net </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private readonly IVstsService vstsService;
        private readonly NavigationServiceEx navigationService;
        private readonly IDialogServiceEx dialogService;
        private readonly RecognitionService recognitionService;
        private readonly SpeechService speechService;
        private bool isCtrlKeyPressed;
        private DispatcherTimer marqueeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        private List<KeyValuePair<string, string>> defaultMarqueeItems = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("PRESS".ToMarqueeKey(), "'CTRL + H' to show HELP")
        };
        private List<KeyValuePair<string, string>> helpMarqueeItems = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("DOUBLE-CLICK".ToMarqueeKey(), "ON A BUG to open it in the browser"),
            new KeyValuePair<string, string>("HOVER".ToMarqueeKey(), "ON A BUG to view details"),
            new KeyValuePair<string, string>("SAY".ToMarqueeKey(), "[NAME] OPEN [WORK-ITEM #] to open it in the browser"),
            new KeyValuePair<string, string>("SAY".ToMarqueeKey(), "[NAME] STATUS / DETAILS OF [WORK-ITEM #] to show the details on screen"),
            new KeyValuePair<string, string>("SAY".ToMarqueeKey(), "[NAME] STOP LISTENING to stop speech recognition"),
            new KeyValuePair<string, string>("PRESS".ToMarqueeKey(), "'CTRL + S' to start listening again"),
            new KeyValuePair<string, string>("PRESS".ToMarqueeKey(), "'CTRL + P' to update preferences"),
            new KeyValuePair<string, string>("PRESS".ToMarqueeKey(), "'CTRL + R' to refresh"),
            new KeyValuePair<string, string>("PRESS".ToMarqueeKey(), "'CTRL + H' to show this help")
        };

        public Dictionary<string, Color> Colors = new Dictionary<string, Color>();

        private Brush background = new SolidColorBrush(Color.FromArgb(255, 24, 24, 24));
        public Brush Background
        {
            get
            {
                return this.background;
            }

            set
            {
                this.Set(ref this.background, value);
            }
        }

        private bool listening;
        public bool Listening
        {
            get
            {
                return this.listening;
            }

            set
            {
                this.Set(ref this.listening, value);
            }
        }

        private ObservableCollection<WorkItem> bugs;
        public ObservableCollection<WorkItem> Bugs
        {
            get
            {
                return this.bugs;
            }

            set
            {
                this.Set(ref this.bugs, value);
            }
        }

        private ObservableCollection<KeyValuePair<string, string>> marqueeItems;
        public ObservableCollection<KeyValuePair<string, string>> MarqueeItems
        {
            get
            {
                return this.marqueeItems;
            }

            set
            {
                this.Set(ref this.marqueeItems, value);
            }
        }

        public MainViewModel(
            IVstsService vstsService,
            NavigationServiceEx navigationService,
            IDialogServiceEx dialogService,
            RecognitionService recognitionService,
            SpeechService speechService)
        {
            this.vstsService = vstsService;
            this.navigationService = navigationService;
            this.dialogService = dialogService;
            this.recognitionService = recognitionService;
            this.speechService = speechService;
            var appView = ApplicationView.GetForCurrentView();
            appView.TitleBar.BackgroundColor = Windows.UI.Colors.IndianRed;
            appView.TitleBar.ForegroundColor = Windows.UI.Colors.White;

            appView.TitleBar.ButtonBackgroundColor = Windows.UI.Colors.IndianRed;
            appView.TitleBar.ButtonForegroundColor = Windows.UI.Colors.White;
        }

        private Func<Task> postInit;
        public void Initialize(Func<Task> postInit)
        {
            this.postInit = postInit;
            this.MarqueeItems = new ObservableCollection<KeyValuePair<string, string>>(this.helpMarqueeItems);
            this.marqueeTimer.Tick += this.Timer_Tick;
            this.MessengerInstance.Register<NotificationMessageAction<bool>>(this, async reply =>
            {
                await this.InitializeInternal();
                reply.Execute(true);
            });
        }

        private async Task InitializeInternal()
        {
            ApplicationView.GetForCurrentView().TryEnterFullScreenMode();
            await this.SetBackground();
            await this.RefreshBugs(true);
            await LaunchSpeechService();

            async Task LaunchSpeechService()
            {
                // Connect the Console Adapter to the Bot.
                await this.recognitionService.Initialize();
            }

            DispatcherHelper.CheckBeginInvokeOnUI(async () => await this.postInit());
        }

        private async Task SetBackground()
        {
            var file = await KnownFolders.DocumentsLibrary.TryGetItemAsync(App.Settings.Background) as StorageFile ?? await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/Background.gif"));
            var bitmap = new BitmapImage { AutoPlay = true };
            await bitmap.SetSourceAsync(await file.OpenReadAsync());
            this.Background = new ImageBrush { ImageSource = bitmap };
        }

        public async Task RefreshBugs(bool loading = false)
        {
            var ids = loading ? new List<int>() : this.Bugs?.Select(x => x.Id)?.ToList();
            this.Bugs = new ObservableCollection<WorkItem>(await this.vstsService.GetBugs(ids));
            this.SetMarqueeBugAssignements();
        }

        public void SetTitle(string criticalitySuffix)
        {
            var count = this.Bugs.Count;
            var s1 = this.Bugs.Count(b => b.Fields.Criticality == 1);
            var s2 = this.Bugs.Count(b => b.Fields.Criticality == 2);
            var s3 = this.Bugs.Count(b => b.Fields.Criticality == 3);
            var s4 = this.Bugs.Count(b => b.Fields.Criticality == 4);

            var title = $"{App.Settings.Account.ToUpperInvariant()} / {App.Settings.Project.ToUpperInvariant()} / GIFTS = {count} / {criticalitySuffix}1 = {s1} / {criticalitySuffix}2 = {s2} / {criticalitySuffix}3 = {s3} / {criticalitySuffix}4 = {s4}";
            var appView = ApplicationView.GetForCurrentView();
            appView.Title = title;
        }

        public Color GetRandomColor(string text = "")
        {
            // Color[] clrs = Enumerable.Range(0, Convert.ToInt32(Math.Pow(count, 2))).Select(x => Color.FromArgb(rnd.Next(0, Convert.ToInt32(Math.Pow(256, 3))) | 0xFF000000)).ToArray();
            // colors = clrs.Distinct().Where(c => c.R > 150 & c.G > 150 & c.B > 150).ToArray();

            if (text.Equals("None"))
            {
                return Windows.UI.Colors.WhiteSmoke;
            }

            var rnd = new Random();
            var byt = new byte[3];
            rnd.NextBytes(byt);
            var r = byt[0];
            var g = byt[1];
            var b = byt[2];

            var color = Color.FromArgb(255, r, g, b);
            // if (Convert.ToInt32(r + g + b) > 270 && Convert.ToInt32(r + g + b) < 630 && this.colors.All(c => !c.Value.IsCloseTo(color)))
            if (Convert.ToInt32(r) > 120 && Convert.ToInt32(g) > 120 && Convert.ToInt32(b) > 120 && Convert.ToInt32(r + g + b) < 630 && this.Colors.All(c => !c.Value.IsCloseTo(color)))
            {
                return color;
            }

            return this.GetRandomColor(text);
        }

        public Task StartStopListening(bool listen)
        {
            return this.recognitionService.SpeechRecognitionChanged(listen);
        }

        public Task DisplayHelp()
        {
            return this.dialogService.ShowMessage("START/STOP SPEECH RECOGNITION:\nCTRL + S", "HELP");
        }

        public async void KeyDown(CoreWindow sender, KeyEventArgs e)
        {
            if (e.VirtualKey == VirtualKey.Control)
            {
                this.isCtrlKeyPressed = true;
            }
            else if (this.isCtrlKeyPressed)
            {
                switch (e.VirtualKey)
                {
                    case VirtualKey.S:
                        await this.StartStopListening(true);
                        break;
                    case VirtualKey.R:
                        this.SetMarqueeItems("REFRESHING".ToMarqueeKey(), "Bugs...");
                        await this.RefreshBugs(true);
                        DispatcherHelper.CheckBeginInvokeOnUI(async () => await this.postInit());
                        break;
                    case VirtualKey.L:
                        this.SetMarqueeItems("OPENING".ToMarqueeKey(), "Log file...");
                        var logsFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("Logs");
                        var log = (await logsFolder.GetFilesAsync())?.OrderByDescending(x => x.DateCreated)?.FirstOrDefault();
                        if (log != null)
                        {
                            await Launcher.LaunchFileAsync(log);
                        }
                        break;
                    case VirtualKey.H:
                        this.SetMarqueeItems(this.helpMarqueeItems);
                        break;
                    case VirtualKey.P:
                        // this.navigationService.Navigate(this.navigationService.GetNameOfRegisteredPage(typeof(SettingsPage)));
                        // await WindowManagerService.Current.TryShowAsViewModeAsync("Settings", typeof(SettingsPage), ApplicationViewMode.Default);
                        await SettingsDisplayService.ShowSettings();
                        break;
                }
            }
        }

        public Task SetMarqueeItems(string bugId)
        {
            var bug = this.Bugs.SingleOrDefault(x => bugId.Equals(x.Id.ToString()));
            return this.SetMarqueeItems(bug, true);
        }

        public void SetMarqueeBugAssignements()
        {
            this.defaultMarqueeItems = this.defaultMarqueeItems.Union(this.Bugs?.GroupBy(b => b.Fields.AssignedTo).OrderByDescending(a => a.Count()).Select(x => new KeyValuePair<string, string>(x.Key.ToMarqueeKey(), x.Count().ToString()))).ToList();
            this.MarqueeItems = new ObservableCollection<KeyValuePair<string, string>>(this.defaultMarqueeItems);
        }

        public async Task SetMarqueeItems(WorkItem bug, bool speak)
        {
            var list = bug?.GetPropertyNamesAndValues(ignoreNames: new[] { "FullName", "Criticality" });
            if (list?.Count > 0)
            {
                this.SetMarqueeItems(list);
                if (speak)
                {
                    await this.speechService.PlaySpeech($"Bug {bug.Id}, with {App.Settings.CriticalityField} {bug.Fields.Criticality}, is assigned to {bug.Fields.AssignedTo}, by {bug.Fields.CreatedBy}, and currently in {bug.Fields.State} state");
                }
            }
        }

        public void SetMarqueeItems(string key, string value)
        {
            this.SetMarqueeItems(new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>(key, value) });
        }

        public void SetMarqueeItems(List<KeyValuePair<string, string>> items)
        {
            if (items?.Count > 0)
            {
                this.MarqueeItems = new ObservableCollection<KeyValuePair<string, string>>(items);
                this.marqueeTimer.Start();
            }
        }

        private void Timer_Tick(object sender, object e)
        {
            this.marqueeTimer.Stop();
            this.MarqueeItems = new ObservableCollection<KeyValuePair<string, string>>(this.defaultMarqueeItems);
        }

        public void KeyUp(CoreWindow sender, KeyEventArgs e)
        {
            if (e.VirtualKey == VirtualKey.Control)
            {
                this.isCtrlKeyPressed = false;
            }
        }
    }
}
