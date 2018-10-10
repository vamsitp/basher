using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Basher.Activation;
using Basher.Helpers;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using Windows.ApplicationModel.Activation;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace Basher.Services
{
    // For more information on application activation see https://github.com/Microsoft/WindowsTemplateStudio/blob/master/docs/activation.md
    internal class ActivationService
    {
        private readonly App _app;
        private readonly Lazy<UIElement> _shell;
        private readonly Type _defaultNavItem;

        private static ViewModels.ViewModelLocator Locator => Application.Current.Resources["Locator"] as ViewModels.ViewModelLocator;

        private static NavigationServiceEx NavigationService => Locator.NavigationService;

        public static readonly KeyboardAccelerator AltLeftKeyboardAccelerator = BuildKeyboardAccelerator(VirtualKey.Left, VirtualKeyModifiers.Menu);

        public static readonly KeyboardAccelerator BackKeyboardAccelerator = BuildKeyboardAccelerator(VirtualKey.GoBack);

        public ActivationService(App app, Type defaultNavItem, Lazy<UIElement> shell = null)
        {
            this._app = app;
            this._shell = shell;
            this._defaultNavItem = defaultNavItem;
        }

        public async Task ActivateAsync(object activationArgs)
        {
            if (this.IsInteractive(activationArgs))
            {
                // Initialize things like registering background task before the app is loaded
                await this.InitializeAsync();

                // Do not repeat app initialization when the Window already has content,
                // just ensure that the window is active
                if (Window.Current.Content == null)
                {
                    // Create a Frame to act as the navigation context and navigate to the first page
                    Window.Current.Content = this._shell?.Value ?? new Frame();
                    NavigationService.NavigationFailed += (sender, e) =>
                    {
                        throw e.Exception;
                    };
                    NavigationService.Navigated += this.Frame_Navigated;
                    if (SystemNavigationManager.GetForCurrentView() != null)
                    {
                        SystemNavigationManager.GetForCurrentView().BackRequested += this.ActivationService_BackRequested;
                    }
                }
            }

            var activationHandler = this.GetActivationHandlers().FirstOrDefault(h => h.CanHandle(activationArgs));

            if (activationHandler != null)
            {
                await activationHandler.HandleAsync(activationArgs);
            }

            if (this.IsInteractive(activationArgs))
            {
                var defaultHandler = new DefaultLaunchActivationHandler(this._defaultNavItem);
                if (defaultHandler.CanHandle(activationArgs))
                {
                    await defaultHandler.HandleAsync(activationArgs);
                }

                // Ensure the current window is active
                Window.Current.Activate();

                // Tasks after activation
                await this.StartupAsync();
            }

            DispatcherHelper.Initialize();
            Messenger.Default.Register<NotificationMessageAction<string>>(this, this.HandleNotificationMessage);
        }

        private void HandleNotificationMessage(NotificationMessageAction<string> message)
        {
            message.Execute("Success (from ActivationService)!");
        }

        private async Task InitializeAsync()
        {
            //await Singleton<LiveTileService>.Instance.EnableQueueAsync();
            //await Singleton<BackgroundTaskService>.Instance.RegisterBackgroundTasksAsync();
            WindowManagerService.Current.Initialize();
            await ThemeSelectorService.InitializeAsync();
        }

        private async Task StartupAsync()
        {
            await ThemeSelectorService.SetRequestedThemeAsync();
            //Singleton<LiveTileService>.Instance.SampleUpdate();
            await FirstRunDisplayService.ShowIfAppropriateAsync();
            await WhatsNewDisplayService.ShowIfAppropriateAsync();
            await SettingsDisplayService.ShowIfAppropriateAsync();
        }

        private IEnumerable<ActivationHandler> GetActivationHandlers()
        {
            //yield return Singleton<LiveTileService>.Instance;
            //yield return Singleton<ToastNotificationsService>.Instance;
            //yield return Singleton<BackgroundTaskService>.Instance;
            yield return Singleton<SuspendAndResumeService>.Instance;
            yield return Singleton<SchemeActivationHandler>.Instance;
        }

        private bool IsInteractive(object args)
        {
            return args is IActivatedEventArgs;
        }

        private void Frame_Navigated(object sender, NavigationEventArgs e)
        {
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = NavigationService.CanGoBack ?
                AppViewBackButtonVisibility.Visible : AppViewBackButtonVisibility.Collapsed;
        }

        private static KeyboardAccelerator BuildKeyboardAccelerator(VirtualKey key, VirtualKeyModifiers? modifiers = null)
        {
            var keyboardAccelerator = new KeyboardAccelerator() { Key = key };
            if (modifiers.HasValue)
            {
                keyboardAccelerator.Modifiers = modifiers.Value;
            }

            ToolTipService.SetToolTip(keyboardAccelerator, string.Empty);
            keyboardAccelerator.Invoked += OnKeyboardAcceleratorInvoked;
            return keyboardAccelerator;
        }

        private static void OnKeyboardAcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            var result = NavigationService.GoBack();
            args.Handled = result;
        }

        private void ActivationService_BackRequested(object sender, BackRequestedEventArgs e)
        {
            var result = NavigationService.GoBack();
            e.Handled = result;
        }
    }
}
