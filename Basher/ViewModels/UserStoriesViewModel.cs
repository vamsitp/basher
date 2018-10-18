namespace Basher.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;

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
            this.Items = new ObservableCollection<WorkItem>(await this.VstsService.GetUserStories(ids));
            await base.RefreshItems(loading);
        }

        public override void SetTitle(string criticalitySuffix)
        {
            var (total, committed, resolved, closed) = this.GetUserStoryCounts();
            var title = $"{App.Settings.Account.ToUpperInvariant()} / {App.Settings.Project.ToUpperInvariant()} / STORIES COMMITTED = {committed} / RESOLVED = {resolved} / CLOSED = {resolved}";
            var appView = ApplicationView.GetForCurrentView();
            appView.Title = title;
        }

        protected (int total, int committed, int resolved, int closed) GetUserStoryCounts()
        {
            var total = this.Items.Count;
            var committed = this.Items.Count(b => b.Fields.State == "Committed");
            var resolved = this.Items.Count(b => b.Fields.State == "Resolved");
            var closed = this.Items.Count(b => b.Fields.State == "Closed");
            return (total, committed, resolved, closed);
        }

        public override async Task Initialize(Func<bool, Task> postInit)
        {
            await base.Initialize(postInit);
            await this.InitializeInternal(false);
        }
    }
}
