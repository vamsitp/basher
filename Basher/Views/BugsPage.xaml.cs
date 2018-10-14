namespace Basher.Views
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using Basher.Helpers;
    using Basher.Models;

    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Media;

    public sealed partial class BugsPage : MainPage
    {
        private Random random = null;

        public BugsPage()
        {
            this.InitializeComponent();
            this.random = new Random();
        }

        protected override async Task PopulateWorkItems(bool loading = false)
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
    }
}
