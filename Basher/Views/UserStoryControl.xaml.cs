namespace Basher.Views
{
    using System;
    using System.Linq;
    using System.Text;
    using Basher.Models;
    using Basher.ViewModels;

    using Windows.UI;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Media;

    public sealed partial class UserStoryControl : ItemControl
    {
        public UserStoryControl(MainViewModel viewModel, double left, double top, WorkItem item, Color color, double maxWidth, double maxHeight, bool flip = false)
            : base(viewModel, left, top, item, color, maxWidth, maxHeight, flip)
        {
            this.InitializeComponent();
        }

        protected override int ControlWidth => 56;

        protected override void SetTooltips()
        {
            var toolTip = new ToolTip();
            var item = this.WorkItem as UserStory;
            var (allTasks, closed, inProgress, notStarted, original, completed, remaining) = ItemViewModel.GetWork(item);
            var content = new StringBuilder();
            content.AppendFormat("{0}: {1}\n", nameof(item.Id), item.Id);
            content.AppendFormat("{0}: {1}\n", nameof(item.Fields.Title), item.Fields.Title);
            content.AppendFormat("{0}: {1}\n", nameof(item.Fields.StoryPoints), item.Fields.StoryPoints);
            content.AppendFormat("{0}: {1}\n", nameof(item.Fields.OriginalEstimate), original);
            content.AppendFormat("{0}: {1}\n", nameof(item.Fields.CompletedWork), completed);
            content.AppendFormat("{0}: {1}\n", nameof(item.Fields.RemainingWork), remaining);
            content.AppendFormat("{0}: {1}\n", nameof(item.Fields.State), item.Fields.State);
            content.AppendFormat("{0}: {1}", nameof(item.Fields.Reason), item.Fields.Reason);
            toolTip.Content = content.ToString();
            ToolTipService.SetToolTip(this.MainControl, toolTip);
        }

        protected override void SetBitmap(int criticality)
        {
            var (allTasks, closed, inProgress, notStarted, original, completed, remaining) = ItemViewModel.GetWork(this.WorkItem as UserStory);
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

            base.SetBitmap(criticality);
        }

        protected override void SuperscriptLoaded()
        {
            var (allTasks, closed, inProgress, notStarted, original, completed, remaining) = ItemViewModel.GetWork(this.WorkItem as UserStory);
            this.Age.Text = $"{closed}c, {inProgress}ip, {notStarted}ns / {allTasks}" + Environment.NewLine + $"{completed}c, {remaining}r / {original}";
            if (closed == allTasks)
            {
                this.Age.Foreground = new SolidColorBrush(Colors.PaleGreen);
            }
            else if (closed >= allTasks / 2)
            {
                this.Age.Foreground = new SolidColorBrush(Colors.Orange);
            }
            else
            {
                this.Age.Foreground = new SolidColorBrush(Colors.OrangeRed);
            }
        }
    }
}
