namespace Basher
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Basher.Models;
    using Basher.Services;
    using CommonServiceLocator;
    using GalaSoft.MvvmLight.Views;
    using Microsoft.AppCenter;
    using Microsoft.AppCenter.Analytics;

    using Serilog;

    using Windows.ApplicationModel;
    using Windows.ApplicationModel.Activation;
    using Windows.Storage;
    using Windows.UI.Xaml;


    public sealed partial class App : Application
    {
        private Lazy<ActivationService> _activationService;

        private IDialogService Dialog => ServiceLocator.Current.GetInstance<IDialogService>();

        private ActivationService ActivationService => this._activationService.Value;

        public static Settings Settings { get; set; }

        public App()
        {
            this.InitializeComponent();
            var logPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "Logs", "Basher_{Date}.log");
            Log.Logger = new LoggerConfiguration().MinimumLevel.Verbose().WriteTo.RollingFile(logPath, outputTemplate: "{Timestamp:dd-MMM-yyyy HH:mm:ss} | [{Level}] {Message}{NewLine}{Exception}").Enrich.FromLogContext().CreateLogger();
            this.UnhandledException += this.App_UnhandledException;
            TaskScheduler.UnobservedTaskException += this.OnTaskSchedulerOnUnobservedTaskException;
            AppCenter.Start("6d81fd25-b4d7-4def-ad94-5e04fa6297b1", typeof(Analytics));
            this.EnteredBackground += this.App_EnteredBackground;

            // Deferred execution until used. Check https://msdn.microsoft.com/library/dd642331(v=vs.110).aspx for further info on Lazy<T> class.
            this._activationService = new Lazy<ActivationService>(this.CreateActivationService);
        }

        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            if (!args.PrelaunchActivated)
            {
                await this.ActivationService.ActivateAsync(args);
            }
        }

        protected override async void OnActivated(IActivatedEventArgs args)
        {
            await this.ActivationService.ActivateAsync(args);
        }

        private ActivationService CreateActivationService()
        {
            return new ActivationService(this, typeof(ViewModels.BugsViewModel));
        }

        private async void App_EnteredBackground(object sender, EnteredBackgroundEventArgs e)
        {
            var deferral = e.GetDeferral();
            await Helpers.Singleton<SuspendAndResumeService>.Instance.SaveStateAsync();
            deferral.Complete();
        }

        protected override async void OnBackgroundActivated(BackgroundActivatedEventArgs args)
        {
            await this.ActivationService.ActivateAsync(args);
        }

        private async void App_UnhandledException(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs args)
        {
            if (!args.Handled)
            {
                args.Handled = true;
            }

            await this.HandleError(args.Exception, args.Message);
        }

        private async void OnTaskSchedulerOnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs args)
        {
            if (!args.Observed)
            {
                args.SetObserved();
            }

            var ex = args.Exception.GetBaseException();
            await this.HandleError(ex, args.Exception.GetBaseException().Message);
        }

        public async Task HandleError(Exception ex, string message = "")
        {
            Log.Error(ex, message);
            await this.Dialog.ShowError(ex, message, "OK", null);
        }
    }
}
