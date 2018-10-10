namespace Basher.Services
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Basher.ViewModels;
    using Basher.Views;
    using CommonServiceLocator;

    using GalaSoft.MvvmLight.Threading;

    using Serilog;

    public class RecognitionHandler
    {
        private const string Spaces = @"\s*";
        private const string OpenItem = "OpenItem";
        private const string OpenPullRequests = "OpenPullRequests";
        private const string OpenWiki = "OpenWiki";
        private const string ItemsAssignedTo = "ItemsAssignedTo";
        private const string StatusOfItem = "StatusOfItem";
        private const string DetailsOfItem = "DetailsOfItem";
        private const string DisplayHelp = "DisplayHelp";
        private const string StopListening = "StopListening";
        private const string GotoSettings = "GotoSettings";

        public static class Intents
        {
            internal const string Open = @"(open|go\s*to|show)";
            internal const string AssignedTo = @"(bug.|assigned)\s*(on|to)";
            internal const string Status = @"(status\s*of)";
            internal const string Details = @"(details\s*of)";
            internal const string Stop = @"(stop|quit|exit)";
        }

        public static class Entities
        {
            internal const string Any = @"(.*)";
            internal const string Wiki = @"(wiki|vicky)";
            internal const string PullRequests = @"(PR.|pull\s*request.)";
            internal const string Id = @"(\d+)";
            internal const string Help = @"(help|usage|faq)";
            internal const string Speech = @"(listening|hearing|speech|recognizing)";
            internal const string Settings = @"(settings|preferences)";
        }

        private static string Suffix => App.Settings.SpeechRecognizerName;
        private static readonly Dictionary<string, Regex> RegexPatterns = new Dictionary<string, Regex>
        {
            { OpenItem, GetRegex(Intents.Open, Entities.Id) },
            { OpenPullRequests, GetRegex(Intents.Open, Entities.PullRequests) },
            { OpenWiki, GetRegex(Intents.Open, Entities.Wiki) },
            { ItemsAssignedTo, GetRegex(Intents.AssignedTo, Entities.Any) },
            { StatusOfItem, GetRegex(Intents.Status, Entities.Id) },
            { DetailsOfItem, GetRegex(Intents.Details, Entities.Id) },
            { DisplayHelp, GetRegex(Intents.Open, Entities.Help) },
            { StopListening, GetRegex(Intents.Stop, Entities.Speech) },
            { GotoSettings, GetRegex(Intents.Open, Entities.Settings) }
        };

        private readonly SpeechService speechService;
        private readonly NavigationServiceEx navigationService;

        public RecognitionHandler(SpeechService speechService, NavigationServiceEx navigationService)
        {
            this.speechService = speechService;
            this.navigationService = navigationService;
        }

        private static Regex GetRegex(string intent, string entity)
        {
            return new Regex($@"{Suffix}{Spaces}{Entities.Any}{Spaces}{intent}{Spaces}{entity}", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        public Task Process(string speechText)
        {
            var text = this.GetMatch(speechText);
            return this.GetReponse(text.Key, text.Value);
        }

        private KeyValuePair<string, (string Intent, string Entity)> GetMatch(string input)
        {
            KeyValuePair<string, (string Intent, string Entity)> result;
            if (!string.IsNullOrWhiteSpace(input))
            {
                foreach (var pattern in RegexPatterns)
                {
                    var match = pattern.Value.Match(input);
                    if (match.Success && match.Groups?.Count >=4 && !string.IsNullOrWhiteSpace(match.Groups[3]?.Value))
                    {
                        var intent = match.Groups[2].Value;
                        var entity = match.Groups[3].Value;
                        result = new KeyValuePair<string, (string Intent, string Entity)>(pattern.Key, (intent, entity));
                        break;
                    }
                }
            }

            Log.Debug($"input: {input} | result: {result}");
            return result;
        }

        private async Task GetReponse(string key, (string Intent, string Entity) input)
        {
            var vm = ServiceLocator.Current.GetInstance<MainViewModel>();
            if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(input.Intent) && !string.IsNullOrWhiteSpace(input.Entity))
            {
                switch (key)
                {
                    case DisplayHelp:
                        DispatcherHelper.CheckBeginInvokeOnUI(async () => await vm.DisplayHelp());
                        break;
                    case StopListening:
                        DispatcherHelper.CheckBeginInvokeOnUI(async () => await vm.StartStopListening(false));
                        break;
                    case StatusOfItem:
                    case DetailsOfItem:
                        DispatcherHelper.CheckBeginInvokeOnUI(() => vm.SetMarqueeItems(input.Entity));
                        break;
                    case OpenItem:
                        await this.LaunchUri($"_workitems/edit/{input.Entity}");
                        break;
                    case OpenWiki:
                        await this.LaunchUri($"_wiki");
                        break;
                    case OpenPullRequests:
                        await this.LaunchUri($"_git");
                        break;
                    case GotoSettings:
                        this.navigationService.Navigate(this.navigationService.GetNameOfRegisteredPage(typeof(SettingsPage)));
                        break;
                    default:
                        var vstsResult = input.Intent + " " + input.Entity;
                        DispatcherHelper.CheckBeginInvokeOnUI(async () => await this.speechService.PlaySpeech(vstsResult));
                        break;
                }
            }
        }

        private async Task LaunchUri(string replacement)
        {
            var uri = string.Format(App.Settings.VstsBaseUrl.OriginalString, App.Settings.Project).Replace("_apis", replacement);
            await Windows.System.Launcher.LaunchUriAsync(new Uri(uri));
        }
    }
}
