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
    using Basher.Views;

    using GalaSoft.MvvmLight;
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
    public abstract class MainViewModel : ViewModelBase
    {
        private static readonly SolidColorBrush WhiteColor = new SolidColorBrush(Windows.UI.Colors.White);
        private readonly RecognitionService recognitionService;
        private readonly SpeechService speechService;
        private bool isCtrlKeyPressed;
        private readonly DispatcherTimer marqueeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };

        private List<MarqueeItem> defaultMarqueeItems = new List<MarqueeItem>
        {
            new MarqueeItem("PRESS".ToMarqueeKey(), "'CTRL + H' to show HELP", WhiteColor)
        };

        private readonly List<MarqueeItem> helpMarqueeItems = new List<MarqueeItem>
        {
            new MarqueeItem("DOUBLE-CLICK".ToMarqueeKey(), "ON A WORK-ITEM to open it in the browser", WhiteColor),
            new MarqueeItem("HOVER".ToMarqueeKey(), "ON A WORK-ITEM to view details", WhiteColor),
            new MarqueeItem("SAY".ToMarqueeKey(), "[NAME] OPEN [WORK-ITEM #] to open it in the browser", WhiteColor),
            new MarqueeItem("SAY".ToMarqueeKey(), "[NAME] STATUS / DETAILS OF [WORK-ITEM #] to show the details on screen", WhiteColor),
            new MarqueeItem("SAY".ToMarqueeKey(), "[NAME] STOP LISTENING to stop speech recognition", WhiteColor),
            new MarqueeItem("PRESS".ToMarqueeKey(), "'CTRL + S' to start listening again", WhiteColor),
            new MarqueeItem("PRESS".ToMarqueeKey(), "'CTRL + P' to update preferences", WhiteColor),
            new MarqueeItem("PRESS".ToMarqueeKey(), "'CTRL + R' to refresh", WhiteColor),
            new MarqueeItem("PRESS".ToMarqueeKey(), "'CTRL + U' to show User-Stories window", WhiteColor),
            new MarqueeItem("PRESS".ToMarqueeKey(), "'CTRL + L' to open the log file", WhiteColor),
            new MarqueeItem("PRESS".ToMarqueeKey(), "'CTRL + H' to show this help", WhiteColor)
        };

        private Dictionary<string, SolidColorBrush> colors = new Dictionary<string, SolidColorBrush>(StringComparer.OrdinalIgnoreCase);

        private Brush background = new SolidColorBrush(Color.FromArgb(255, 24, 24, 24));

        public Brush Background
        {
            get => this.background;

            set => this.Set(ref this.background, value);
        }

        private bool listening;

        public bool Listening
        {
            get => this.listening;

            set => this.Set(ref this.listening, value);
        }

        private ObservableCollection<WorkItem> wItems;

        public ObservableCollection<WorkItem> Items
        {
            get => this.wItems;

            set => this.Set(ref this.wItems, value);
        }

        private ObservableCollection<MarqueeItem> marqueeItems;

        public ObservableCollection<MarqueeItem> MarqueeItems
        {
            get => this.marqueeItems;

            set => this.Set(ref this.marqueeItems, value);
        }

        protected CoreDispatcher Dispatcher { get; private set; }

        protected IVstsService VstsService { get; }

        protected NavigationServiceEx NavigationService { get; }

        protected IDialogServiceEx DialogService { get; }

        public Dictionary<string, SolidColorBrush> Colors
        {
            get => this.colors;
            set => this.colors = value;
        }

        //protected CoreDispatcher Dispatcher { get; }

        public MainViewModel(
            IVstsService vstsService,
            NavigationServiceEx navigationService,
            IDialogServiceEx dialogService,
            RecognitionService recognitionService,
            SpeechService speechService)
        {
            this.Dispatcher = Window.Current.Dispatcher;
            this.VstsService = vstsService;
            this.NavigationService = navigationService;
            this.DialogService = dialogService;
            this.recognitionService = recognitionService;
            this.speechService = speechService;
            var appView = ApplicationView.GetForCurrentView();
            appView.TitleBar.BackgroundColor = Windows.UI.Colors.IndianRed;
            appView.TitleBar.ForegroundColor = Windows.UI.Colors.White;

            appView.TitleBar.ButtonBackgroundColor = Windows.UI.Colors.IndianRed;
            appView.TitleBar.ButtonForegroundColor = Windows.UI.Colors.White;
        }

        private Func<bool, Task> postInit;

        public virtual Task Initialize(Func<bool, Task> postInit)
        {
            this.postInit = postInit;
            this.marqueeTimer.Tick += this.MarqueeTimer_Tick;
            return Task.CompletedTask;
        }

        protected async Task InitializeInternal(bool launchFullscreen)
        {
            if (launchFullscreen && !ApplicationView.GetForCurrentView().IsFullScreenMode)
            {
                ApplicationView.GetForCurrentView().TryEnterFullScreenMode();
            }

            await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => this.MarqueeItems = new ObservableCollection<MarqueeItem>(this.helpMarqueeItems));
            await this.SetBackground();
            await this.RefreshItems(true);
            await LaunchSpeechService();

            async Task LaunchSpeechService()
            {
                // Connect the Console Adapter to the Bot.
                await this.recognitionService.Initialize();
            }

            await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () => await this.postInit(false));
        }

        private async Task SetBackground()
        {
            var file = await KnownFolders.DocumentsLibrary.TryGetItemAsync(App.Settings.Background) as StorageFile ?? await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/Background.gif"));
            var bitmap = new BitmapImage { AutoPlay = true };
            await bitmap.SetSourceAsync(await file.OpenReadAsync());
            this.Background = new ImageBrush { ImageSource = bitmap };
        }

        public virtual Task RefreshItems(bool loading = false)
        {
            if (this.Colors.Count == 0)
            {
                this.Colors = this.Items.Select(b => b.Fields.AssignedToFullName).Distinct().ToDictionary(x => x, x => new SolidColorBrush(this.GetRandomColor(x)));
            }

            this.SetMarqueeAssignements();
            return Task.CompletedTask;
        }

        public virtual void SetTitle(string criticalitySuffix)
        {
            var count = this.Items.Count;
            var s1 = this.Items.Count(b => b.Fields.Criticality == 1);
            var s2 = this.Items.Count(b => b.Fields.Criticality == 2);
            var s3 = this.Items.Count(b => b.Fields.Criticality == 3);
            var s4 = this.Items.Count(b => b.Fields.Criticality == 4);

            var title = $"{App.Settings.Account.ToUpperInvariant()} / {App.Settings.Project.ToUpperInvariant()} / GIFTS = {count} / {criticalitySuffix}1 = {s1} / {criticalitySuffix}2 = {s2} / {criticalitySuffix}3 = {s3} / {criticalitySuffix}4 = {s4}";
            var appView = ApplicationView.GetForCurrentView();
            appView.Title = title;
        }

        public Color GetRandomColor(string text = "")
        {
            // Color[] clrs = Enumerable.Range(0, Convert.ToInt32(Math.Pow(count, 2))).Select(x =>
            // Color.FromArgb(rnd.Next(0, Convert.ToInt32(Math.Pow(256, 3))) |
            // 0xFF000000)).ToArray(); colors = clrs.Distinct().Where(c => c.R > 150 & c.G > 150 &
            // c.B > 150).ToArray();

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
            // if (Convert.ToInt32(r + g + b) > 270 && Convert.ToInt32(r + g + b) < 630 &&
            // this.colors.All(c => !c.Value.IsCloseTo(color)))
            if (Convert.ToInt32(r) > 120 && Convert.ToInt32(g) > 120 && Convert.ToInt32(b) > 120 && Convert.ToInt32(r + g + b) < 630 && this.Colors.All(c => !c.Value.Color.IsCloseTo(color)))
            {
                return color;
            }

            return this.GetRandomColor(text);
        }

        public Task StartStopListening(bool listen)
        {
            return this.recognitionService.SpeechRecognitionChanged(listen);
        }

        public void DisplayHelp()
        {
            this.SetMarqueeItems(this.helpMarqueeItems);
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
                        this.SetMarqueeItems("REFRESHING".ToMarqueeKey(), "Work-items...");
                        await this.RefreshItems(true);
                        await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () => await this.postInit(true));
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
                        this.DisplayHelp();
                        break;

                    case VirtualKey.U:
                        if (WindowManagerService.Current.SecondaryViews.Count == 0)
                        {
                            DispatcherHelper.CheckBeginInvokeOnUI(async () => await WindowManagerService.Current.TryShowAsStandaloneAsync("USER STORIES", typeof(UserStoriesPage)));
                        }
                        break;

                    case VirtualKey.P:
                        // this.navigationService.Navigate(this.navigationService.GetNameOfRegisteredPage(typeof(SettingsPage)));
                        await SettingsDisplayService.ShowSettings();
                        break;
                }
            }
        }

        public Task SetMarqueeItems(string workItemId)
        {
            var workItem = this.Items.SingleOrDefault(x => workItemId.Equals(x.Id.ToString()));
            return this.SetMarqueeItems(workItem, true);
        }

        public void SetMarqueeAssignements()
        {
            var items = this.Items?.GroupBy(b => (AssignedTo: b.Fields.AssignedTo, AssignedToFullName: b.Fields.AssignedToFullName)).OrderByDescending(a => a.Count());
            var marqItems = items.Select(this.GetMarqueeItem);
            this.defaultMarqueeItems = this.defaultMarqueeItems.Union(marqItems).ToList();
            this.MarqueeItems = new ObservableCollection<MarqueeItem>(this.defaultMarqueeItems);
        }

        protected abstract MarqueeItem GetMarqueeAssignment(IGrouping<(string AssignedTo, string AssignedToFullName), WorkItem> item);

        private MarqueeItem GetMarqueeItem(IGrouping<(string AssignedTo, string AssignedToFullName), WorkItem> x)
        {
            var item = this.GetMarqueeAssignment(x);
            var assignedTo = x.Key.AssignedToFullName;
            if (!this.Colors.ContainsKey(assignedTo))
            {
                this.Colors.Add(assignedTo, new SolidColorBrush(this.GetRandomColor(assignedTo)));
            }

            item.Color = this.Colors[assignedTo];
            return item;
        }

        public async Task SetMarqueeItems(WorkItem workItem, bool speak)
        {
            var list = workItem?.GetPropertyNamesAndValues(ignoreNames: new[] { "FullName", "Criticality", "Tasks" });
            if (list?.Count > 0)
            {
                this.SetMarqueeItems(list.Select(x => new MarqueeItem(x.Key, x.Value, WhiteColor)).ToList());
                if (speak)
                {
                    await this.speechService.PlaySpeech($"{workItem.Id}, with {App.Settings.CriticalityField} {workItem.Fields.Criticality}, is assigned to {workItem.Fields.AssignedTo}, by {workItem.Fields.CreatedBy}, and currently in {workItem.Fields.State} state");
                }
            }
        }

        public void SetMarqueeItems(string key, string value)
        {
            this.SetMarqueeItems(new List<MarqueeItem> { new MarqueeItem(key, value, WhiteColor) });
        }

        public void SetMarqueeItems(List<MarqueeItem> items)
        {
            if (items?.Count > 0)
            {
                this.MarqueeItems = new ObservableCollection<MarqueeItem>(items);
                this.marqueeTimer.Start();
            }
        }

        private void MarqueeTimer_Tick(object sender, object e)
        {
            this.marqueeTimer.Stop();
            this.MarqueeItems = new ObservableCollection<MarqueeItem>(this.defaultMarqueeItems);
        }

        public void KeyUp(CoreWindow sender, KeyEventArgs e)
        {
            if (e.VirtualKey == VirtualKey.Control)
            {
                this.isCtrlKeyPressed = false;
            }
        }

        protected (int Total, int P1, int P2, int P3, int P4) GetCounts(string assignedToFullName = null)
        {
            var workitems = string.IsNullOrWhiteSpace(assignedToFullName) ? this.Items.ToList() : this.Items.Where(x => x.Fields.AssignedToFullName.Equals(assignedToFullName, StringComparison.OrdinalIgnoreCase)).ToList();
            var count = workitems.Count;
            var s1 = workitems.Count(b => b.Fields.Criticality == 1);
            var s2 = workitems.Count(b => b.Fields.Criticality == 2);
            var s3 = workitems.Count(b => b.Fields.Criticality == 3);
            var s4 = workitems.Count(b => b.Fields.Criticality == 4);
            return (count, s1, s2, s3, s4);
        }
    }
}
