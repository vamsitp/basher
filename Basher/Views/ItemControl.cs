namespace Basher.Views
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Basher.Helpers;
    using Basher.Models;
    using Basher.ViewModels;
    using CommonServiceLocator;
    using Windows.Storage;
    using Windows.UI;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Input;
    using Windows.UI.Xaml.Markup;
    using Windows.UI.Xaml.Media;
    using Windows.UI.Xaml.Media.Animation;
    using Windows.UI.Xaml.Media.Imaging;

    public abstract class ItemControl : UserControl
    {
        private const string WorkItemsApi = "_apis/wit/workItems";
        private static readonly string WorkItemEdit = $"{App.Settings.Project}/_workitems/edit";
        private static int[] times = Enumerable.Range(1, 10).ToArray();

        public WorkItem WorkItem { get; }

        protected abstract int ControlWidth { get; }

        public IList<WorkItem> ActiveTasks => (this.WorkItem as UserStory).Tasks.Where(x => !x.Fields.State.Equals("Removed")).ToList();

        private readonly MainViewModel viewModel;
        private double left;
        private double top;
        private readonly string text;
        private readonly bool flip;
        private readonly Color color;

        private double maxWidth;
        private double maxHeight;
        // private string animationState = Extensions.Playing;

        protected ItemControl(MainViewModel viewModel, double left, double top, WorkItem item, Color color, double maxWidth, double maxHeight, bool flip = false)
        {
            this.Tag = item;
            this.WorkItem = item;
            this.viewModel = viewModel;
            this.left = left;
            this.top = top;
            this.text = item.GetText();
            this.flip = flip;
            this.Criticality = item.Fields.Criticality;
            this.color = color;
            this.maxWidth = maxWidth;
            this.maxHeight = maxHeight;
        }

        protected int Criticality { get; }

        public StackPanel MainControl { get; private set; }

        public TextBlock AssignedTo { get; private set; }

        public TextBlock Age { get; private set; }

        public Image Item { get; private set; }

        public async Task Initialize()
        {
            var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/ItemControl.xml"));
            var xml = await FileIO.ReadTextAsync(file);
            var content = XamlReader.Load(xml) as Canvas;
            this.Content = content;

            this.MainControl = content.FindName(nameof(this.MainControl)) as StackPanel;
            this.MainControl.Loaded += this.MainControl_Loaded;

            this.AssignedTo = content.FindName(nameof(this.AssignedTo)) as TextBlock;
            this.AssignedTo.Loaded += this.AssignedTo_Loaded;

            this.Age = content.FindName(nameof(this.Age)) as TextBlock;
            this.Age.Loaded += this.Age_Loaded;

            this.Item = content.FindName(nameof(this.Item)) as Image;
            this.Item.Loaded += this.Item_Loaded;
        }

        protected async void ItemControl_DoubleTapped(object sender, Windows.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri(this.WorkItem.Url.Replace(WorkItemsApi, WorkItemEdit)));
        }

        protected async void ItemControl_Tapped(object sender, TappedRoutedEventArgs e)
        {
            await this.viewModel.SetMarqueeItems(this.WorkItem, false);
        }

        protected void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            this.SetTooltips();
        }

        protected abstract void SetTooltips();

        private void Item_Loaded(object sender, RoutedEventArgs e)
        {
            var img = sender as Image;
            var bitmapImage = new BitmapImage { AutoPlay = true };
            img.Source = bitmapImage;
            this.SetBitmap(img, this.Criticality);
            img.Width = bitmapImage.DecodePixelWidth = this.ControlWidth;
        }

        protected void AssignedTo_Loaded(object sender, RoutedEventArgs e)
        {
            this.SetText(this.text);
        }

        protected void MainControl_Loaded(object sender, RoutedEventArgs e)
        {
            this.Animate();
        }

        public void Animate(double left, double top, double width, double height)
        {
            this.left = left;
            this.top = top;
            this.maxWidth = width;
            this.maxHeight = height;
            this.Animate();
        }

        private void Animate()
        {
            this.MainControl.SetValue(Canvas.LeftProperty, this.left);
            this.MainControl.SetValue(Canvas.TopProperty, this.top);
            this.MainControl.Animate(times, this.maxWidth, this.maxHeight);
        }

        public void SetText(string text)
        {
            this.AssignedTo.Text = text;
            this.SetForeground(this.Criticality);
        }

        public void SetCriticality(int criticality)
        {
            this.SetBitmap(this.Item, criticality);
            this.SetForeground(criticality);
        }

        private void SetForeground(int severity)
        {
            //if (severity == 1)
            //{
            //    this.AssignedTo.Foreground = new SolidColorBrush(Colors.OrangeRed);
            //}
            //else if (severity == 2)
            //{
            //    this.AssignedTo.Foreground = new SolidColorBrush(Colors.IndianRed);
            //}
            //else
            //{
            this.AssignedTo.Foreground = new SolidColorBrush(this.color);
            //}
        }

        protected virtual void SetBitmap(Image img, int criticality)
        {
            var suffix = this.GetSuffix(criticality);
            var bitmap = (img.Source as BitmapImage);
            var prefix = this.GetType().Name.Replace("Control", string.Empty);
            bitmap.UriSource = new Uri($"ms-appx:///Assets/{prefix}{suffix}.gif");
        }

        private string GetSuffix(int criticality)
        {
            var suffix = criticality.ToString();
            return suffix; // + (criticality == 4 ? (this.flip ? "_" : string.Empty) : string.Empty);
        }

        protected abstract void SuperscriptLoaded();

        protected void Age_Loaded(object sender, RoutedEventArgs e)
        {
            this.SuperscriptLoaded();
        }

        protected (float Original, float Completed, float Remaining) GetWork()
        {
            var activeTasks = this.ActiveTasks;
            var original = activeTasks.Sum(x => x.Fields.OriginalEstimate);
            var completed = activeTasks.Sum(x => x.Fields.CompletedWork);
            var remaining = activeTasks.Sum(x => x.Fields.RemainingWork);
            return (original, completed, remaining);
        }
    }
}
