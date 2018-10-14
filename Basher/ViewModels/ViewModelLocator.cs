using System;

using Basher.Services;
using Basher.Views;

using CommonServiceLocator;

using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Views;

namespace Basher.ViewModels
{
    [Windows.UI.Xaml.Data.Bindable]
    public class ViewModelLocator
    {
        private static bool initialized;

        public ViewModelLocator()
        {
            if (!initialized)
            {
                ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);
                SimpleIoc.Default.Register(() => new NavigationServiceEx());

                SimpleIoc.Default.Register<IDialogService, DialogService>();
                SimpleIoc.Default.Register<IDialogServiceEx, DialogServiceEx>();
                SimpleIoc.Default.Register<IVstsService, VstsService>();
                SimpleIoc.Default.Register<SpeechService>();
                SimpleIoc.Default.Register<RecognitionHandler>();
                SimpleIoc.Default.Register<RecognitionService>();

                this.Register<BugsViewModel, BugsPage>();
                this.Register<UserStoriesViewModel, UserStoriesPage>();
                this.Register<WebViewViewModel, WebViewPage>();
                this.Register<ChartViewModel, ChartPage>();
                this.Register<SettingsViewModel, SettingsPage>();
                this.Register<SchemeActivationSampleViewModel, SchemeActivationSamplePage>();
                initialized = true;
            }
        }

        public SchemeActivationSampleViewModel SchemeActivationSampleViewModel => ServiceLocator.Current.GetInstance<SchemeActivationSampleViewModel>();

        public SettingsViewModel SettingsViewModel => ServiceLocator.Current.GetInstance<SettingsViewModel>();

        public ChartViewModel ChartViewModel => ServiceLocator.Current.GetInstance<ChartViewModel>();

        public WebViewViewModel WebViewViewModel => ServiceLocator.Current.GetInstance<WebViewViewModel>();

        public BugsViewModel BugsViewModel => ServiceLocator.Current.GetInstance<BugsViewModel>();

        public UserStoriesViewModel UserStoriesViewModel => ServiceLocator.Current.GetInstance<UserStoriesViewModel>();

        public NavigationServiceEx NavigationService => ServiceLocator.Current.GetInstance<NavigationServiceEx>();

        public void Register<VM, V>()
            where VM : class
        {
            SimpleIoc.Default.Register<VM>();
            this.NavigationService.Configure(typeof(VM).FullName, typeof(V));
        }
    }
}
