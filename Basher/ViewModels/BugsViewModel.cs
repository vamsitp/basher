namespace Basher.ViewModels
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;

    using Basher.Models;
    using Basher.Services;

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
            this.Items = new ObservableCollection<WorkItem>(await this.VstsService.GetBugs(ids));
            await base.RefreshItems(loading);
        }

        public override void SetTitle(string criticalitySuffix)
        {
            var (count, s1, s2, s3, s4) = this.GetCounts();
            var title = $"{App.Settings.Account.ToUpperInvariant()} / {App.Settings.Project.ToUpperInvariant()} / GIFTS = {count} / {criticalitySuffix}1 = {s1} / {criticalitySuffix}2 = {s2} / {criticalitySuffix}3 = {s3} / {criticalitySuffix}4 = {s4}";
            var appView = ApplicationView.GetForCurrentView();
            appView.Title = title;
        }
    }
}
