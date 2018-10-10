namespace Basher.Services
{
    using System;
    using System.Threading.Tasks;

    using GalaSoft.MvvmLight.Views;

    using Windows.UI.Xaml.Controls;

    public class DialogServiceEx : DialogService, IDialogServiceEx
    {
        public async Task<ContentDialogResult> ShowContentDialog(string title, object content, string buttonText = "OK")
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                CloseButtonText = buttonText
            };

            var result = await dialog.ShowAsync();
            return result;
        }
    }

    public interface IDialogServiceEx : IDialogService
    {
        Task<ContentDialogResult> ShowContentDialog(string title, object content, string buttonText = "OK");
    }
}
