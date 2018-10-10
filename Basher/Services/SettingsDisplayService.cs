using System;
using System.Threading.Tasks;
using Basher.Helpers;
using Basher.Models;
using Basher.Views;
using CommonServiceLocator;
using GalaSoft.MvvmLight.Messaging;
using Windows.Storage;

namespace Basher.Services
{
    // For instructions on testing this service see https://github.com/Microsoft/WindowsTemplateStudio/tree/master/docs/features/whats-new-prompt.md
    public static class SettingsDisplayService
    {
        internal static async Task ShowIfAppropriateAsync()
        {
            var settings = await ApplicationData.Current.LocalSettings.ReadAsync<Settings>("Basher");
            if (settings == null)
            {
                settings = new Settings { ApiVersion = "4.1", Background = "Background.gif", CriticalityField = "Severity", RefreshIntervalInSecs = 90, SpeechLocale = "en-US" };
            }

            App.Settings = settings;
            if (string.IsNullOrWhiteSpace(App.Settings?.Account) || string.IsNullOrWhiteSpace(App.Settings?.Project) || string.IsNullOrWhiteSpace(App.Settings?.AccessToken))
            {
                await ShowSettings();
            }

            Messenger.Default.Send(new NotificationMessageAction<bool>("Settings set", reply => { }));
        }

        public static async Task ShowSettings()
        {
            await ServiceLocator.Current.GetInstance<IDialogServiceEx>().ShowContentDialog("PREFERENCES", new SettingsPage(), "SAVE");
            await ApplicationData.Current.LocalSettings.SaveAsync("Basher", App.Settings);
        }
    }
}
