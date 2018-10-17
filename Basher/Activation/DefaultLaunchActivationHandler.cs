using System;
using System.Threading.Tasks;

using Basher.Services;

using Windows.ApplicationModel.Activation;

namespace Basher.Activation
{
    internal class DefaultLaunchActivationHandler : ActivationHandler<LaunchActivatedEventArgs>
    {
        private readonly string _navElement;

        private NavigationServiceEx NavigationService => CommonServiceLocator.ServiceLocator.Current.GetInstance<NavigationServiceEx>();

        public DefaultLaunchActivationHandler(Type navElement)
        {
            this._navElement = navElement.FullName;
        }

        protected override async Task HandleInternalAsync(LaunchActivatedEventArgs args)
        {
            // When the navigation stack isn't restored, navigate to the first page and configure the
            // new page by passing required information in the navigation parameter
            this.NavigationService.Navigate(this._navElement, args.Arguments);

            //// TODO WTS: Remove or change this sample which shows a toast notification when the app is launched.
            //// You can use this sample to create toast notifications where needed in your app.
            //Singleton<ToastNotificationsService>.Instance.ShowToastNotificationSample();
            await Task.CompletedTask;
        }

        protected override bool CanHandleInternal(LaunchActivatedEventArgs args)
        {
            // None of the ActivationHandlers has handled the app activation
            return this.NavigationService.Frame.Content == null;
        }
    }
}
