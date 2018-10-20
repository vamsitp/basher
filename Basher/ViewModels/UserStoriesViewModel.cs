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

    using Windows.UI.ViewManagement;

    public class UserStoriesViewModel : MainViewModel
    {
        public UserStoriesViewModel(
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
            var ucs = await this.VstsService.GetUserStories(ids);
            if (ucs != null)
            {
                this.Items = new ObservableCollection<WorkItem>(await this.VstsService.GetUserStories(ids));
                await base.RefreshItems(loading);
            }
        }

        public override void SetTitle(string criticalitySuffix)
        {
            var (total, committed, resolved, closed) = this.GetUserStoryCounts();
            var title = $"{App.Settings.Account.ToUpperInvariant()} / {App.Settings.Project.ToUpperInvariant()} / STORIES COMMITTED = {committed} / RESOLVED = {resolved} / CLOSED = {resolved}";
            var appView = ApplicationView.GetForCurrentView();
            appView.Title = title;
        }

        protected (int total, int committed, int resolved, int closed) GetUserStoryCounts(string assignedToFullName = null)
        {
            var workitems = string.IsNullOrWhiteSpace(assignedToFullName) ? this.Items.ToList() : this.Items.Where(x => x.Fields.AssignedToFullName.Equals(assignedToFullName, StringComparison.OrdinalIgnoreCase)).ToList();
            var total = workitems.Count;
            var committed = workitems.Count(b => b.Fields.State == "Committed");
            var resolved = workitems.Count(b => b.Fields.State == "Resolved");
            var closed = workitems.Count(b => b.Fields.State == "Closed");
            return (total, committed, resolved, closed);
        }

        public override async Task Initialize(Func<bool, Task> postInit)
        {
            await base.Initialize(postInit);
            await this.InitializeInternal(false);
        }

        protected override MarqueeItem GetMarqueeAssignment(IGrouping<(string AssignedTo, string AssignedToFullName), WorkItem> x)
        {
            var (total, committed, resolved, closed) = this.GetUserStoryCounts(x.Key.AssignedToFullName);
            var item = new MarqueeItem(x.Key.AssignedTo.ToMarqueeKey(upperCase: false), $"Committed = {committed} / Closed = {closed} / Resolved = {resolved}");
            return item;
        }
    }
}
