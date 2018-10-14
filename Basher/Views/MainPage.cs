namespace Basher.Views
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using Basher.Helpers;
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
    using Windows.UI.Xaml.Media;
    using Windows.UI.Xaml.Media.Animation;
    using Windows.UI.Xaml.Navigation;

    public class MainPage : Page
    {
        protected Random random = null;
        private readonly SpeechService speechService;
        private DispatcherTimer timer = null;

        public MainPage()
        {
            this.speechService = ServiceLocator.Current.GetInstance<SpeechService>();
            this.random = new Random();
        }

        public virtual MainViewModel Vm => (MainViewModel)this.DataContext;

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

        protected virtual async Task PopulateWorkItems(bool loading = false)
        {
            var bugs = this.Vm.Bugs;
            if (bugs == null)
            {
                return;
            }

            var count = bugs.Count;
            var criticalitySuffix = App.Settings.Criticality;
            this.Vm.SetTitle(criticalitySuffix);

            var lefts = this.ActualWidth.ToParts(count).ToList();
            var tops = this.ActualHeight.ToParts(count).ToList();
            var randomLocations = lefts.Select(x => (Left: lefts[this.random.Next(count)], Top: tops[this.random.Next(count)])).ToList();
            for (var i = 0; i < count; i++)
            {
                var bug = bugs[i];
                var bugControl = this.MainGrid.Children?.SingleOrDefault(x => ((WorkItem)(x as UserControl).Tag).Id.Equals(bug.Id)) as BugControl;
                var user = bug.Fields.AssignedToFullName;
                if (bugControl != null)
                {
                    var prevState = (WorkItem)bugControl.Tag;
                    if (bug.Fields.State == "Resolved" || bug.Fields.State == "Closed") // if (i % 2 == 0)
                    {
                        if (bug.Fields.State == "Resolved")
                        {
                            user = bug.Fields.ResolvedBy;
                        }
                        else
                        {
                            user = bug.Fields.ClosedBy;
                        }

                        await this.PopUp(this.ResolvedPopup, this.ResolvedPopupText, user.ToUpperInvariant() + $", YOU ROCK!\n({bug.Fields.AssignedTo}: {criticalitySuffix}{bug.Fields.Criticality} - {bug.Id})", "applause", $"{user} {bug.Fields.State}: {bug.Id}", loading);
                        bugControl.Disappear();
                        this.MainGrid.Children.Remove(bugControl);
                        bugControl = null;
                    }
                    else
                    {
                        if (!user.Equals(prevState.Fields.AssignedToFullName))
                        {
                            bugControl.SetText(bug.GetText());
                            await this.PopUp(this.AssignedPopup, this.AssignedPopupText, bug.Fields.AssignedTo.ToUpperInvariant() + $" HAS A GIFT ASSIGNED!\n({bug.Fields.ChangedBy}: {criticalitySuffix}{bug.Fields.Criticality} - {bug.Id})", "kidding", $"{bug.Fields.AssignedTo} has an assigned {criticalitySuffix}{bug.Fields.Criticality} gift: {bug.Id}", loading);
                        }

                        if (!bug.Fields.Severity.Equals(prevState.Fields.Severity))
                        {
                            bugControl.SetCriticality(bug.Fields.Criticality);
                            if (bug.Fields.Criticality == 1)
                            {
                                await this.PopUp(this.AssignedPopup, this.AssignedPopupText, bug.Fields.AssignedTo.ToUpperInvariant() + $" HAS A GIFT!\n({bug.Fields.ChangedBy}: {criticalitySuffix}{bug.Fields.Criticality} - {bug.Id})", "busy", $"{bug.Fields.AssignedTo} has one {criticalitySuffix}1 gift: {bug.Id}", loading);
                            }
                        }

                        bugControl.Tag = bug;
                    }
                }
                else
                {
                    if (!this.Vm.Colors.ContainsKey(user))
                    {
                        this.Vm.Colors.Add(user, new SolidColorBrush(this.Vm.GetRandomColor(user)));
                    }

                    this.AddWorkItem(bug, randomLocations[i], i % 2 == 0, this.Vm.Colors[user].Color);
                    await this.PopUp(this.AssignedPopup, this.AssignedPopupText, bug.Fields.AssignedTo.ToUpperInvariant() + $" HAS A NEW GIFT!\n({bug.Fields.CreatedBy}: {criticalitySuffix}{bug.Fields.Criticality} - {bug.Id})", "alarm", $"{bug.Fields.AssignedTo} has a new {criticalitySuffix}{bug.Fields.Criticality} gift: Bug {bug.Id}", loading);
                }
            }
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
