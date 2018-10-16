namespace Basher.Views
{
    using System;
    using System.Collections.Generic;
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

        public virtual MainViewModel ViewModel => (MainViewModel)this.DataContext;

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
            this.SizeChanged += this.MainPage_SizeChanged;
            this.MarqueeStoryboard = content.FindName(nameof(this.MarqueeStoryboard)) as Storyboard;
            this.MainGrid = content.FindName(nameof(this.MainGrid)) as Grid;
            this.AssignedPopup = content.FindName(nameof(this.AssignedPopup)) as Popup;
            this.AssignedPopupText = content.FindName(nameof(this.AssignedPopupText)) as TextBlock;
            this.ResolvedPopup = content.FindName(nameof(this.ResolvedPopup)) as Popup;
            this.ResolvedPopupText = content.FindName(nameof(this.ResolvedPopupText)) as TextBlock;

            //if (e.NavigationMode == NavigationMode.New)
            //{
            await this.ViewModel.Initialize(() =>
            {
                this.SetTimer();
                return this.PopulateWorkItems(true);
            });

            this.MarqueeStoryboard.Begin();
            //}

            CoreWindow.GetForCurrentThread().KeyDown += this.ViewModel.KeyDown;
            CoreWindow.GetForCurrentThread().KeyUp += this.ViewModel.KeyUp;
            base.OnNavigatedTo(e);
        }

        private void MainPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Math.Abs(e.NewSize.Height - e.PreviousSize.Height) >= 100 || Math.Abs(e.NewSize.Width - e.PreviousSize.Width) >= 150)
            {
                var items = this.MainGrid.Children.Cast<ItemControl>().ToList();
                var randomLocations = this.GetRandomLocations(items.Count);
                for (var i = 0; i < items.Count; i++)
                {
                    this.ReAnimateItem(items[i], randomLocations[i]);
                }
            }
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            CoreWindow.GetForCurrentThread().KeyDown -= this.ViewModel.KeyDown;
            CoreWindow.GetForCurrentThread().KeyUp -= this.ViewModel.KeyUp;
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
            await this.ViewModel.RefreshItems(false);
            await this.PopulateWorkItems(false);
        }

        protected virtual async Task PopulateWorkItems(bool loading = false)
        {
            var items = this.ViewModel.Items;
            if (items == null)
            {
                return;
            }

            var count = items.Count;
            var criticalitySuffix = App.Settings.Criticality;
            this.ViewModel.SetTitle(criticalitySuffix);
            var randomLocations = this.GetRandomLocations(count);
            for (var i = 0; i < count; i++)
            {
                var item = items[i];
                var itemControl = this.MainGrid.Children?.SingleOrDefault(x => ((WorkItem)(x as UserControl).Tag).Id.Equals(item.Id)) as ItemControl;
                var user = item.Fields.AssignedToFullName;
                if (itemControl != null)
                {
                    this.ReAnimateItem(itemControl, randomLocations[i]);
                    var prevState = (WorkItem)itemControl.Tag;
                    if (item.Fields.State == "Resolved" || item.Fields.State == "Closed") // if (i % 2 == 0)
                    {
                        if (item.Fields.State == "Resolved")
                        {
                            user = item.Fields.ResolvedBy;
                        }
                        else
                        {
                            user = item.Fields.ClosedBy;
                        }

                        await this.PopUp(this.ResolvedPopup, this.ResolvedPopupText, user.ToUpperInvariant() + $", YOU ROCK!\n({item.Fields.AssignedTo}: {criticalitySuffix}{item.Fields.Criticality} - {item.Id})", "applause", $"{user} {item.Fields.State}: {item.Id}", loading);
                        itemControl.Disappear();
                        this.MainGrid.Children.Remove(itemControl);
                        itemControl = null;
                    }
                    else
                    {
                        if (!user.Equals(prevState.Fields.AssignedToFullName))
                        {
                            itemControl.SetText(item.GetText());
                            await this.PopUp(this.AssignedPopup, this.AssignedPopupText, item.Fields.AssignedTo.ToUpperInvariant() + $" HAS A GIFT ASSIGNED!\n({item.Fields.ChangedBy}: {criticalitySuffix}{item.Fields.Criticality} - {item.Id})", "kidding", $"{item.Fields.AssignedTo} has an assigned {criticalitySuffix}{item.Fields.Criticality} gift: {item.Id}", loading);
                        }

                        if (item.Fields.Severity != null && !item.Fields.Severity.Equals(prevState.Fields.Severity))
                        {
                            itemControl.SetCriticality(item.Fields.Criticality);
                            if (item.Fields.Criticality == 1)
                            {
                                await this.PopUp(this.AssignedPopup, this.AssignedPopupText, item.Fields.AssignedTo.ToUpperInvariant() + $" HAS A GIFT!\n({item.Fields.ChangedBy}: {criticalitySuffix}{item.Fields.Criticality} - {item.Id})", "busy", $"{item.Fields.AssignedTo} has one {criticalitySuffix}1 gift: {item.Id}", loading);
                            }
                        }

                        itemControl.Tag = item;
                    }
                }
                else
                {
                    if (!this.ViewModel.Colors.ContainsKey(user))
                    {
                        this.ViewModel.Colors.Add(user, new SolidColorBrush(this.ViewModel.GetRandomColor(user)));
                    }

                    this.AddWorkItem(item, randomLocations[i], i % 2 == 0, this.ViewModel.Colors[user].Color);
                    await this.PopUp(this.AssignedPopup, this.AssignedPopupText, item.Fields.AssignedTo.ToUpperInvariant() + $" HAS A NEW GIFT!\n({item.Fields.CreatedBy}: {criticalitySuffix}{item.Fields.Criticality} - {item.Id})", "alarm", $"{item.Fields.AssignedTo} has a new {criticalitySuffix}{item.Fields.Criticality} gift: Bug {item.Id}", loading);
                }
            }
        }

        private List<(double Left, double Top)> GetRandomLocations(int count)
        {
            var lefts = this.ActualWidth.ToParts(count).ToList();
            var tops = this.ActualHeight.ToParts(count).ToList();
            var randomLocations = lefts.Select(x => (Left: lefts[this.random.Next(count)], Top: tops[this.random.Next(count)])).ToList();
            return randomLocations;
        }

        protected void ReAnimateItem(ItemControl itemControl, (double Left, double Top) randomLocation)
        {
            itemControl.Animate(randomLocation.Left, randomLocation.Top, this.ActualWidth, this.ActualHeight);
        }

        protected void AddWorkItem(WorkItem bug, (double Left, double Top) randomLocation, bool flip, Color color)
        {
            var itemControl = new ItemControl(this.ViewModel, randomLocation.Left, randomLocation.Top, bug, color, this.ActualWidth, this.ActualHeight, flip)
            {
                Tag = bug
            };
            this.MainGrid.Children.Add(itemControl);
        }

        private void Grid_DoubleTapped(object sender, Windows.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {
            var appView = ApplicationView.GetForCurrentView();
            appView.TryEnterFullScreenMode();
        }
    }
}
