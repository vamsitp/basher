namespace Basher.Views
{
    using System;
    using System.Text;

    using Basher.Models;
    using Basher.ViewModels;

    using Windows.UI;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Media;

    public sealed partial class BugControl : ItemControl
    {
        public BugControl(MainViewModel viewModel, double left, double top, WorkItem item, Color color, double maxWidth, double maxHeight, bool flip = false)
            : base(viewModel, left, top, item, color, maxWidth, maxHeight, flip)
        {
            this.InitializeComponent();
        }

        protected override int ControlWidth => (5 * (24 / this.Criticality) + (32 / this.Criticality));

        protected override void SetTooltips()
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

        protected override void SuperscriptLoaded()
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
    }
}
