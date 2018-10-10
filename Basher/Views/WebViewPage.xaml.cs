using System;

using Basher.ViewModels;

using Windows.UI.Xaml.Controls;

namespace Basher.Views
{
    public sealed partial class WebViewPage : Page
    {
        private WebViewViewModel ViewModel
        {
            get { return DataContext as WebViewViewModel; }
        }

        public WebViewPage()
        {
            InitializeComponent();
            ViewModel.Initialize(webView);
        }
    }
}
