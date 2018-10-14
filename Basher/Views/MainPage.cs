namespace Basher.Views
{
    using System;
    using System.Threading.Tasks;

    using Basher.Models;
    using Basher.Services;
    using Basher.ViewModels;

    using CommonServiceLocator;

    using Windows.Storage;
    using Windows.UI;
    using Windows.UI.Core;
    using Windows.UI.ViewManagement;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Controls.Primitives;
    using Windows.UI.Xaml.Markup;
    using Windows.UI.Xaml.Media.Animation;
    using Windows.UI.Xaml.Navigation;

    public class MainPage : Page
    {
        private readonly SpeechService speechService;

        private DispatcherTimer timer = null;

        public MainPage()
        {
            this.speechService = ServiceLocator.Current.GetInstance<SpeechService>();
        }

        public MainViewModel Vm => (MainViewModel)this.DataContext;

        public Storyboard MarqueeStoryboard { get; private set; }

        public Grid MainGrid { get; private set; }

        public Popup AssignedPopup { get; private set; }

        public TextBlock AssignedPopupText { get; private set; }

        public Popup ResolvedPopup { get; private set; }

        public TextBlock ResolvedPopupText { get; private set; }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/MainPage.xml"));
            var xml = await FileIO.ReadTextAsync(file);
            var content = XamlReader.Load(xml) as Grid;
            this.Content = content;
            this.MarqueeStoryboard = content.FindName(nameof(this.MarqueeStoryboard)) as Storyboard;
            this.MainGrid = content.FindName(nameof(this.MainGrid)) as Grid;
            this.AssignedPopup = content.FindName(nameof(this.AssignedPopup)) as Popup;
            this.AssignedPopupText = content.FindName(nameof(this.AssignedPopupText)) as TextBlock;
            this.ResolvedPopup = content.FindName(nameof(this.ResolvedPopup)) as Popup;
            this.ResolvedPopupText = content.FindName(nameof(this.ResolvedPopupText)) as TextBlock;

            //if (e.NavigationMode == NavigationMode.New)
            //{
            this.Vm.Initialize(() =>
            {
                this.SetTimer();
                return this.PopulateWorkItems(true);
            });

            this.MarqueeStoryboard.Begin();
            //}

            CoreWindow.GetForCurrentThread().KeyDown += this.Vm.KeyDown;
            CoreWindow.GetForCurrentThread().KeyUp += this.Vm.KeyUp;
            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            CoreWindow.GetForCurrentThread().KeyDown -= this.Vm.KeyDown;
            CoreWindow.GetForCurrentThread().KeyUp -= this.Vm.KeyUp;
            base.OnNavigatingFrom(e);
        }

        protected async Task PopUp(Popup popup, TextBlock popupText, string text, string sound, string speechText, bool loading = false)
        {
            if (!loading)
            {
                popupText.Text = text;
                popup.IsOpen = true;
                await this.speechService.Play(sound, speechText, loading);
                await Task.Delay(5000);
                popup.IsOpen = false;
            }
        }

        private void SetTimer()
        {
            this.timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(App.Settings.RefreshIntervalInSecs) };
            this.timer.Tick += this.Timer_Tick;
            this.timer.Start();
        }

        private async void Timer_Tick(object sender, object e)
        {
            await this.Vm.RefreshBugs(false);
            await this.PopulateWorkItems(false);
        }

        protected virtual Task PopulateWorkItems(bool loading = false)
        {
            return Task.FromResult(loading);
        }

        protected void AddWorkItem(WorkItem bug, (double Left, double Top) randomLocation, bool flip, Color color)
        {
            var bugControl = new BugControl(randomLocation.Left, randomLocation.Top, bug, color, this.ActualWidth, this.ActualHeight, flip)
            {
                Tag = bug
            };
            this.MainGrid.Children.Add(bugControl);
        }

        private void Grid_DoubleTapped(object sender, Windows.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {
            var appView = ApplicationView.GetForCurrentView();
            appView.TryEnterFullScreenMode();
        }
    }
}
