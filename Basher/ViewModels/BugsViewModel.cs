namespace Basher.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;

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
            this.Items = new ObservableCollection<WorkItem>(await this.VstsService.GetBugs(ids));
            await base.RefreshItems(loading);
        }

        public override void SetTitle(string criticalitySuffix)
        {
            var count = this.Items.Count;
            var s1 = this.Items.Count(b => b.Fields.Priority == 1);
            var s2 = this.Items.Count(b => b.Fields.Priority == 2);
            var s3 = this.Items.Count(b => b.Fields.Priority == 3);
            var s4 = this.Items.Count(b => b.Fields.Priority == 4);

            var title = $"{App.Settings.Account.ToUpperInvariant()} / {App.Settings.Project.ToUpperInvariant()} / GIFTS = {count} / P1 = {s1} / P2 = {s2} / P3 = {s3} / P4 = {s4}";
            var appView = ApplicationView.GetForCurrentView();
            appView.Title = title;
        }

        public override async Task Initialize(Func<Task> postInit)
        {
            await base.Initialize(postInit);
            this.MessengerInstance.Register<NotificationMessageAction<bool>>(this, async reply =>
            {
                await this.InitializeInternal(true);
                reply.Execute(true);
            });
        }
    }
}
