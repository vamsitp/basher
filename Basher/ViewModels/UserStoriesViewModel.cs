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
            if (ucs?.Count > 0)
            {
                this.Items = new ObservableCollection<WorkItem>(ucs);
                await base.RefreshItems(loading);
            }
        }

        public override void SetTitle(string criticalitySuffix)
        {
            var (stories, allTasks, closed, inProgress, notStarted, original, completed, remaining) = this.GetUserStoryCounts();
            var title = $"{App.Settings.Account.ToUpperInvariant()} | {App.Settings.Project.ToUpperInvariant()} | STORIES COMMITTED = {stories} | TASKS = {closed}C, {inProgress}IP, {notStarted}NS / {allTasks} | WORK = {completed}C, {remaining}R / {original}";
            var appView = ApplicationView.GetForCurrentView();
            appView.Title = title;
        }

        protected (int stories, int allTasks, int closed, int inProgress, int notStarted, float original, float completed, float remaining) GetUserStoryCounts(string assignedToFullName = null)
        {
            var workitems = string.IsNullOrWhiteSpace(assignedToFullName) ? this.Items.ToList() : this.Items.Where(x => x.Fields.AssignedToFullName.Equals(assignedToFullName, StringComparison.OrdinalIgnoreCase)).ToList();
            var stories = workitems.Count;
            var (allTasks, closed, inProgress, notStarted, original, completed, remaining) = ItemViewModel.GetWork(workitems.Cast<UserStory>());
            return (stories, allTasks, closed, inProgress, notStarted, original, completed, remaining);
        }

        public override async Task Initialize(Func<bool, Task> postInit)
        {
            await base.Initialize(postInit);
            await this.InitializeInternal(false);
        }

        protected override MarqueeItem GetMarqueeAssignment(IGrouping<(string AssignedTo, string AssignedToFullName), WorkItem> x)
        {
            var (stories, allTasks, closed, inProgress, notStarted, original, completed, remaining) = this.GetUserStoryCounts(x.Key.AssignedToFullName);
            var item = new MarqueeItem(x.Key.AssignedTo.ToMarqueeKey(upperCase: false), $"Stories = {stories} | Tasks = {closed}c, {inProgress}ip, {notStarted}ns / {allTasks} | Work = {completed}c, {remaining}r / {original}");
            return item;
        }
    }
}
