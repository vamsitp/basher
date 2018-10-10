using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;

using Basher.Helpers;
using Basher.Services;

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

using Windows.ApplicationModel;
using Windows.Storage;
using Windows.UI.Xaml;

namespace Basher.ViewModels
{
    // TODO WTS: Add other settings as necessary. For help see https://github.com/Microsoft/WindowsTemplateStudio/blob/master/docs/pages/settings.md
    public class SettingsViewModel : ViewModelBase
    {
        public Visibility FeedbackLinkVisibility => Microsoft.Services.Store.Engagement.StoreServicesFeedbackLauncher.IsSupported() ? Visibility.Visible : Visibility.Collapsed;

        private string _account;
        private string _project;
        private string _customWiqlFilter;
        private string _accessToken;
        private string _criticalityField;
        private string _apiVersion;
        private double _refreshIntervalInSecs;
        private string _speechLocale;
        private string _background;
        private string _speechRecognizerName;

        // private ViewLifetimeControl _viewLifetimeControl;

        // public void Initialize(ViewLifetimeControl viewLifetimeControl)
        // {
        //     _viewLifetimeControl = viewLifetimeControl;
        //     _viewLifetimeControl.Released += OnViewLifetimeControlReleased;
        // }

        // private async void OnViewLifetimeControlReleased(object sender, EventArgs e)
        // {
        //     _viewLifetimeControl.Released -= OnViewLifetimeControlReleased;
        //     await WindowManagerService.Current.MainDispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
        //     {
        //         WindowManagerService.Current.SecondaryViews.Remove(_viewLifetimeControl);
        //     });
        // }

        private ICommand _launchFeedbackHubCommand;

        public ICommand LaunchFeedbackHubCommand
        {
            get
            {
                if (this._launchFeedbackHubCommand == null)
                {
                    this._launchFeedbackHubCommand = new RelayCommand(
                        async () =>
                        {
                            // This launcher is part of the Store Services SDK https://docs.microsoft.com/en-us/windows/uwp/monetize/microsoft-store-services-sdk
                            var launcher = Microsoft.Services.Store.Engagement.StoreServicesFeedbackLauncher.GetDefault();
                            await launcher.LaunchAsync();
                        });
                }

                return this._launchFeedbackHubCommand;
            }
        }

        private ICommand _launchPrivacyCommand;
        public ICommand LaunchPrivacyCommand
        {
            get
            {
                if (this._launchPrivacyCommand == null)
                {
                    this._launchPrivacyCommand = new RelayCommand(
                        async () =>
                        {
                            await RecognitionService.ShowSpeechPermissions();
                        });
                }

                return this._launchPrivacyCommand;
            }
        }

        private ElementTheme _elementTheme = ThemeSelectorService.Theme;

        public List<double> RefreshIntervals => new List<double> { 60, 90, 120, 180, 360, 720 };

        public List<string> Criticalities => new List<string> { "Severity", "Priority" };

        public ElementTheme ElementTheme
        {
            get => this._elementTheme;

            set => this.Set(ref this._elementTheme, value);
        }

        private string _versionDescription;

        public string VersionDescription
        {
            get => this._versionDescription;
            set => this.Set(ref this._versionDescription, value);
        }

        public string Account
        {
            get => this._account;

            set
            {
                if (this.Set(ref this._account, value) && App.Settings != null)
                {
                    App.Settings.Account = value;
                }
            }
        }

        public string Project
        {
            get => this._project;

            set
            {
                if (this.Set(ref this._project, value) && App.Settings != null)
                {
                    App.Settings.Project = value;
                }
            }
        }

        public string AccessToken
        {
            get => this._accessToken;

            set
            {
                if (this.Set(ref this._accessToken, value) && App.Settings != null)
                {
                    App.Settings.AccessToken = value;
                }
            }
        }

        public string CustomWiqlFilter
        {
            get => this._customWiqlFilter;

            set
            {
                if (this.Set(ref this._customWiqlFilter, value) && App.Settings != null)
                {
                    App.Settings.CustomWiqlFilter = value;
                }
            }
        }

        public string CriticalityField
        {
            get => this._criticalityField;

            set
            {
                if (this.Set(ref this._criticalityField, value) && App.Settings != null)
                {
                    App.Settings.CriticalityField = value;
                }
            }
        }

        public string ApiVersion
        {
            get => this._apiVersion;

            set
            {
                if (this.Set(ref this._apiVersion, value) && App.Settings != null)
                {
                    App.Settings.ApiVersion = value;
                }
            }
        }

        public double RefreshIntervalInSecs
        {
            get => this._refreshIntervalInSecs;

            set
            {
                if (this.Set(ref this._refreshIntervalInSecs, value) && App.Settings != null)
                {
                    App.Settings.RefreshIntervalInSecs = value;
                }
            }
        }

        public string SpeechLocale
        {
            get => this._speechLocale;

            set
            {
                if (this.Set(ref this._speechLocale, value) && App.Settings != null)
                {
                    App.Settings.SpeechLocale = value;
                }
            }
        }

        public string Background
        {
            get => this._background;

            set
            {
                if (this.Set(ref this._background, value) && App.Settings != null)
                {
                    App.Settings.Background = value;
                }
            }
        }

        public string SpeechRecognizerName
        {
            get => this._speechRecognizerName;

            set
            {
                if (this.Set(ref this._speechRecognizerName, value) && App.Settings != null)
                {
                    App.Settings.SpeechRecognizerName = value;
                }
            }
        }

        private ICommand _switchThemeCommand;

        public ICommand SwitchThemeCommand
        {
            get
            {
                if (this._switchThemeCommand == null)
                {
                    this._switchThemeCommand = new RelayCommand<ElementTheme>(
                        async (param) =>
                        {
                            this.ElementTheme = param;
                            await ThemeSelectorService.SetThemeAsync(param);
                        });
                }

                return this._switchThemeCommand;
            }
        }

        public void Initialize()
        {
            this.VersionDescription = this.GetVersionDescription();
            var settings = App.Settings;
            this.Account = settings.Account;
            this.Project = settings.Project;
            this.AccessToken = settings.AccessToken;
            this.CustomWiqlFilter = settings.CustomWiqlFilter;
            this.ApiVersion = settings.ApiVersion;
            this.CriticalityField = settings.CriticalityField;
            this.RefreshIntervalInSecs = settings.RefreshIntervalInSecs;
            this.SpeechLocale = settings.SpeechLocale;
            this.Background = settings.Background;
            this.SpeechRecognizerName = settings.SpeechRecognizerName;
        }

        private string GetVersionDescription()
        {
            var appName = "AppDisplayName".GetLocalized();
            var package = Package.Current;
            var packageId = package.Id;
            var version = packageId.Version;

            return $"{appName} - {version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }
    }
}
