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
    using GalaSoft.MvvmLight.Messaging;
    using Windows.UI.ViewManagement;

    public class BugsViewModel : MainViewModel
    {
        public BugsViewModel(
                IVstsService vstsService,
                NavigationServiceEx navigationService,
                IDialogServiceEx dialogService,
                RecognitionService recognitionService,
                SpeechService speechService)
            : base(vstsService,
                    navigationService,
                    dialogService,
                    recognitionService,
                    speechService)
        {
        }

        public override async Task RefreshItems(bool loading = false)
        {
            var ids = loading ? new List<int>() : this.Items?.Select(x => x.Id)?.ToList();
            var bugs = await this.VstsService.GetBugs(ids);
            if (bugs != null)
            {
                this.Items = new ObservableCollection<WorkItem>(bugs);
                await base.RefreshItems(loading);
            }
        }

        public override void SetTitle(string criticalitySuffix)
        {
            var (count, s1, s2, s3, s4) = this.GetCounts();
            var title = $"{App.Settings.Account.ToUpperInvariant()} / {App.Settings.Project.ToUpperInvariant()} / GIFTS ACTIVE = {count} / {criticalitySuffix}1 = {s1} / {criticalitySuffix}2 = {s2} / {criticalitySuffix}3 = {s3} / {criticalitySuffix}4 = {s4}";
            var appView = ApplicationView.GetForCurrentView();
            appView.Title = title;
        }

        protected (int Total, int P1, int P2, int P3, int P4) GetCounts(string assignedToFullName = null)
        {
            var workitems = string.IsNullOrWhiteSpace(assignedToFullName) ? this.Items.ToList() : this.Items.Where(x => x.Fields.AssignedToFullName.Equals(assignedToFullName, StringComparison.OrdinalIgnoreCase)).ToList();
            var total = workitems.Count;
            // var active = workitems.Count(b => b.Fields.State == "Active" || b.Fields.State == "New" || b.Fields.State == "Committed");
            // var resolved = workitems.Count(b => b.Fields.State == "Resolved");
            // var closed = workitems.Count(b => b.Fields.State == "Closed");

            var s1 = workitems.Count(b => b.Fields.Criticality == 1);
            var s2 = workitems.Count(b => b.Fields.Criticality == 2);
            var s3 = workitems.Count(b => b.Fields.Criticality == 3);
            var s4 = workitems.Count(b => b.Fields.Criticality == 4);
            return (total, s1, s2, s3, s4);
        }

        public override async Task Initialize(Func<bool, Task> postInit)
        {
            await base.Initialize(postInit);
            this.MessengerInstance.Register<NotificationMessageAction<bool>>(this, async reply =>
            {
                await this.InitializeInternal(true);
                reply.Execute(true);
            });
        }

        protected override MarqueeItem GetMarqueeAssignment(IGrouping<(string AssignedTo, string AssignedToFullName), WorkItem> x)
        {
            var (count, s1, s2, s3, s4) = this.GetCounts(x.Key.AssignedToFullName);
            var item = new MarqueeItem(x.Key.AssignedTo.ToMarqueeKey(upperCase: false), $"Gifts = {count.ToString().PadLeft(2)} / {App.Settings.Criticality}1 = {s1} / {App.Settings.Criticality}2 = {s2} / {App.Settings.Criticality}3 = {s3} / {App.Settings.Criticality}4 = {s4}");
            return item;
        }
    }
}
