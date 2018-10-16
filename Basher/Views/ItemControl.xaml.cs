namespace Basher.Views
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using Basher.Helpers;
    using Basher.Models;
    using Basher.ViewModels;

    using Windows.UI;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Input;
    using Windows.UI.Xaml.Media;
    using Windows.UI.Xaml.Media.Animation;
    using Windows.UI.Xaml.Media.Imaging;

    public sealed partial class ItemControl : UserControl
    {
        private const string WorkItemsApi = "_apis/wit/workItems";
        private static readonly string WorkItemEdit = $"{App.Settings.Project}/_workitems/edit";
        private static int[] times = Enumerable.Range(1, 10).ToArray();

        public WorkItem WorkItem { get; }

        public IList<WorkItem> ActiveTasks => (this.WorkItem as UserStory).Tasks.Where(x => !x.Fields.State.Equals("Removed")).ToList();

        private readonly MainViewModel viewModel;
        private readonly string text;
        private readonly bool flip;
        private readonly int criticality;
        private readonly Color color;

        private string animationState = Extensions.Playing;
        private bool isBug;
        private double left;
        private double top;
        private double maxWidth;
        private double maxHeight;

        public ItemControl(MainViewModel viewModel, double left, double top, WorkItem item, Color color, double maxWidth, double maxHeight, bool flip = false)
        {
            this.isBug = viewModel is BugsViewModel;
            this.Tag = item;
            this.WorkItem = item;
            this.viewModel = viewModel;
            this.left = left;
            this.top = top;
            this.text = item.GetText();
            this.flip = flip;
            this.criticality = item.Fields.Criticality;
            this.color = color;
            this.maxWidth = maxWidth;
            this.maxHeight = maxHeight;

            this.InitializeComponent();
        }

        private async void ItemControl_DoubleTapped(object sender, Windows.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri(this.WorkItem.Url.Replace(WorkItemsApi, WorkItemEdit)));
        }

        private async void ItemControl_Tapped(object sender, TappedRoutedEventArgs e)
        {
            await this.viewModel.SetMarqueeItems(this.WorkItem, false);
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var toolTip = new ToolTip();
            var item = this.WorkItem;
            var content = new StringBuilder();
            content.AppendFormat("{0}: {1}\n", nameof(item.Id), item.Id);
            content.AppendFormat("{0}: {1}\n", nameof(item.Fields.Title), item.Fields.Title);
            content.AppendFormat("{0}: {1}\n", nameof(item.Fields.CreatedBy), item.Fields.CreatedBy);
            content.AppendFormat("{0}: {1}\n", nameof(item.Fields.ChangedBy), item.Fields.ChangedBy);
            content.AppendFormat("{0}: {1}\n", nameof(item.Fields.Severity), item.Fields.Severity);
            content.AppendFormat("{0}: {1}\n", nameof(item.Fields.Priority), item.Fields.Priority);
            content.AppendFormat("{0}: {1}\n", nameof(item.Fields.State), item.Fields.State);
            content.AppendFormat("{0}: {1}", nameof(item.Fields.Reason), item.Fields.Reason);
            toolTip.Content = content.ToString();
            ToolTipService.SetToolTip(this.MainControl, toolTip);
        }

        private void Item_Loaded(object sender, RoutedEventArgs e)
        {
            var img = sender as Image;
            var bitmapImage = new BitmapImage { AutoPlay = true };
            img.Source = bitmapImage;
            this.SetBitmap(img, this.criticality);
            var width = 60;
            if (this.isBug)
            {
                width = (5 * (24 / this.criticality) + (32 / this.criticality));
            }

            img.Width = bitmapImage.DecodePixelWidth = width;
        }

        private void AssignedTo_Loaded(object sender, RoutedEventArgs e)
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

        private void MainControl_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (this.animationState.Equals(Extensions.Playing))
            {
                this.animationState = Extensions.Paused;
                foreach (var sb in this.MainControl.Tag as List<Storyboard>)
                {
                    sb.Pause();
                }
            }
            else
            {
                this.animationState = Extensions.Playing;
                foreach (var sb in this.MainControl.Tag as List<Storyboard>)
                {
                    sb.Resume();
                }
            }
        }

        public void SetText(string text)
        {
            this.AssignedTo.Text = text;
            this.SetForeground(this.criticality);
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

        private void SetBitmap(Image img, int criticality)
        {
            if (!this.isBug)
            {
                var (original, completed, remaining) = this.GetWork();
                if (remaining == 0)
                {
                    criticality = 4;
                }
                else if (completed == 0)
                {
                    criticality = 1;
                }
                else if (remaining > completed)
                {
                    criticality = 2;
                }
                else
                {
                    criticality = 3;
                }
            }

            var suffix = this.GetSuffix(criticality);
            var bitmap = (img.Source as BitmapImage);
            var prefix = this.isBug ? "Bug" : "UserStory";
            bitmap.UriSource = new Uri($"ms-appx:///Assets/{prefix}{suffix}.gif");
        }

        private string GetSuffix(int criticality)
        {
            var suffix = criticality.ToString();
            return suffix; // + (criticality == 4 ? (this.flip ? "_" : string.Empty) : string.Empty);
        }

        private void Age_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.isBug)
            {
                var item = this.WorkItem;
                var age = DateTimeOffset.Now.Subtract(item.Fields.CreatedDate).Days;
                this.Age.Text = age.ToString() + "d";
                if (age > 7 && age <= 14)
                {
                    this.Age.Foreground = new SolidColorBrush(Colors.DarkSalmon);
                }
                else if (age > 14 && age <= 30)
                {
                    this.Age.Foreground = new SolidColorBrush(Colors.Orange);
                }
                else if (age > 30)
                {
                    this.Age.Foreground = new SolidColorBrush(Colors.OrangeRed);
                }
            }
            else
            {
                var activeTasks = this.ActiveTasks;
                var allTasks = activeTasks.Count;
                var closedTasks = activeTasks.Count(x => x.Fields.State.Equals("Closed"));
                var (original, completed, remaining) = this.GetWork();
                this.Age.Text = $"{closedTasks}C / {allTasks}T" + Environment.NewLine + $"{completed}C, {remaining}R / {original}H";
                if (closedTasks == allTasks)
                {
                    this.Age.Foreground = new SolidColorBrush(Colors.PaleGreen);
                }
                else if (closedTasks >= allTasks / 2)
                {
                    this.Age.Foreground = new SolidColorBrush(Colors.Orange);
                }
                else
                {
                    this.Age.Foreground = new SolidColorBrush(Colors.OrangeRed);
                }
            }
        }

        private (float Original, float Completed, float Remaining) GetWork()
        {
            var activeTasks = this.ActiveTasks;
            var original = activeTasks.Sum(x => x.Fields.OriginalEstimate);
            var completed = activeTasks.Sum(x => x.Fields.CompletedWork);
            var remaining = activeTasks.Sum(x => x.Fields.RemainingWork);
            return (original, completed, remaining);
        }
    }
}
