namespace Basher.ViewModels
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;
    using Basher.Models;
    using Basher.Services;
    using Windows.UI.ViewManagement;
    using Windows.UI.Xaml.Media;

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

        public override async Task RefreshBugs(bool loading = false)
        {
            var ids = loading ? new List<int>() : this.Bugs?.Select(x => x.Id)?.ToList();
            this.Bugs = new ObservableCollection<WorkItem>(await this.VstsService.GetUserStories(ids));
            if (this.Colors.Count == 0)
            {
                this.Colors = this.Bugs.Select(b => b.Fields.AssignedToFullName).Distinct().ToDictionary(x => x, x => new SolidColorBrush(this.GetRandomColor(x)));
            }

            this.SetMarqueeBugAssignements();
        }

        public override void SetTitle(string criticalitySuffix)
        {
            var count = this.Bugs.Count;
            var s1 = this.Bugs.Count(b => b.Fields.Priority == 1);
            var s2 = this.Bugs.Count(b => b.Fields.Priority == 2);
            var s3 = this.Bugs.Count(b => b.Fields.Priority == 3);
            var s4 = this.Bugs.Count(b => b.Fields.Priority == 4);

            var title = $"{App.Settings.Account.ToUpperInvariant()} / {App.Settings.Project.ToUpperInvariant()} / US = {count} / P1 = {s1} / P2 = {s2} / P3 = {s3} / P4 = {s4}";
            var appView = ApplicationView.GetForCurrentView();
            appView.Title = title;
        }
    }
}
