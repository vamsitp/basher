namespace Basher.Services
{
    using System;
    using System.Threading.Tasks;

    using Basher.Helpers;
    using Basher.ViewModels;

    using CommonServiceLocator;

    using GalaSoft.MvvmLight.Views;

    using Serilog;

    using Windows.Globalization;
    using Windows.Media.SpeechRecognition;
    using Windows.UI.Core;

    public class RecognitionService
    {
        // The speech recognizer used throughout this sample.
        private SpeechRecognizer speechRecognizer = null;
        private readonly RecognitionHandler speechHandler = null;

        /// <summary>
        /// the HResult 0x8004503a typically represents the case where a recognizer for a particular language cannot
        /// be found. This may occur if the language is installed, but the speech pack for that language is not.
        /// See Settings -> Time & Language -> Region & Language -> *Language* -> Options -> Speech Language Options.
        /// </summary>
        private static readonly uint HResultRecognizerNotFound = 0x8004503a;
        private bool permissionGained;
        private readonly IDialogService dialogService;
        private readonly SpeechService speechService;

        public RecognitionService(IDialogService dialogService, SpeechService speechService, RecognitionHandler speechHandler)
        {
            this.speechHandler = speechHandler;
            this.dialogService = dialogService;
            this.speechService = speechService;
        }

        public async Task Initialize()
        {
            if (!this.permissionGained)
            {
                await WindowManagerService.Current.MainDispatcher.RunAsync(CoreDispatcherPriority.Normal, async () => this.permissionGained = await AudioCapturePermissions.RequestMicrophonePermission());
            }

            try
            {
                if (this.speechRecognizer != null)
                {
                    this.speechRecognizer.StateChanged -= this.SpeechRecognizer_StateChanged;
                    this.speechRecognizer.ContinuousRecognitionSession.Completed -= this.ContinuousRecognitionSession_Completed;
                    this.speechRecognizer.ContinuousRecognitionSession.ResultGenerated -= this.ContinuousRecognitionSession_ResultGenerated;
                    this.speechRecognizer.HypothesisGenerated -= this.SpeechRecognizer_HypothesisGenerated;

                    this.speechRecognizer.Dispose();
                    this.speechRecognizer = null;
                }

                var recognizerLanguage = new Language(App.Settings.SpeechLocale); // SpeechRecognizer.SystemSpeechLanguage
                this.speechRecognizer = new SpeechRecognizer(recognizerLanguage);

                // Provide feedback to the user about the state of the recognizer. This can be used to provide visual feedback in the form
                // of an audio indicator to help the user understand whether they're being heard.
                this.speechRecognizer.StateChanged += this.SpeechRecognizer_StateChanged;

                // Apply the dictation topic constraint to optimize for dictated free-form speech.
                var dictationConstraint = new SpeechRecognitionTopicConstraint(SpeechRecognitionScenario.Dictation, "dictation");
                this.speechRecognizer.Constraints.Add(dictationConstraint);
                var result = await this.speechRecognizer.CompileConstraintsAsync();
                if (result.Status != SpeechRecognitionResultStatus.Success)
                {
                    await this.dialogService.ShowError(result.Status.ToString(), "Grammar Compilation Failed", "OK", null);
                }

                // Handle continuous recognition events. Completed fires when various error states occur. ResultGenerated fires when
                // some recognized phrases occur, or the garbage rule is hit. HypothesisGenerated fires during recognition, and
                // allows us to provide incremental feedback based on what the user's currently saying.
                this.speechRecognizer.ContinuousRecognitionSession.Completed += this.ContinuousRecognitionSession_Completed;
                this.speechRecognizer.ContinuousRecognitionSession.ResultGenerated += this.ContinuousRecognitionSession_ResultGenerated;
                this.speechRecognizer.HypothesisGenerated += this.SpeechRecognizer_HypothesisGenerated;
                await this.StartRecognizing(true);
            }
            catch (Exception ex)
            {
                if ((uint)ex.HResult == HResultRecognizerNotFound)
                {
                    throw new Exception("Speech Language pack for selected language not installed.", ex);
                }
                else
                {
                    throw;
                }
            }
        }

        public async Task SpeechRecognitionChanged(bool listen)
        {
            Log.Debug($"Page_SpeechRecognitionChanged: {listen}");
            if (listen)
            {
                await this.StartRecognizing(false);
            }
            else
            {
                await this.StopRecognizing();
            }
        }

        private async Task StartRecognizing(bool loading)
        {
            // The recognizer can only start listening in a continuous fashion if the recognizer is currently idle.
            // This prevents an exception from occurring.
            if (this.speechRecognizer.State == SpeechRecognizerState.Idle)
            {
                try
                {
                    if (!loading)
                    {
                        await this.speechService.PlaySpeech("Starting speech recognition");
                    }

                    await this.speechRecognizer.ContinuousRecognitionSession.StartAsync();
                }
                catch (Exception ex)
                {
                    await this.dialogService.ShowMessageBox(ex.Message.Replace("The text associated with this error code could not be found.", string.Empty), "SPEECH PERMISSIONS");
                    await ShowSpeechPermissions();
                }
            }
        }

        public static async Task ShowSpeechPermissions()
        {
            var settings = "PrivacyTermsLink".GetLocalized().Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var setting in settings)
            {
                var success = await Windows.System.Launcher.LaunchUriAsync(new Uri(setting));
                if (success)
                {
                    break;
                }
            }
        }

        private async Task StopRecognizing()
        {
            if (this.speechRecognizer != null)
            {
                try
                {
                    //if (this.speechRecognizer.State != SpeechRecognizerState.Idle)
                    //{
                    //    await this.speechRecognizer.ContinuousRecognitionSession.CancelAsync();
                    //}

                    await this.speechService.PlaySpeech("Stopping speech recognition");
                    await this.speechRecognizer.ContinuousRecognitionSession.StopAsync();
                }
                catch (Exception ex)
                {
                    throw new Exception("Error stopping Speech-recognizer", ex);
                }
            }
        }

        BugsViewModel ViewModel => ServiceLocator.Current.GetInstance<BugsViewModel>();
        private async void SpeechRecognizer_HypothesisGenerated(SpeechRecognizer sender, SpeechRecognitionHypothesisGeneratedEventArgs args)
        {
            await WindowManagerService.Current.MainDispatcher.RunAsync(CoreDispatcherPriority.Normal, () => this.ViewModel.Listening = true);
        }

        private void SpeechRecognizer_StateChanged(SpeechRecognizer sender, SpeechRecognizerStateChangedEventArgs args)
        {
            Log.Verbose("StateChanged: " + args.State.ToString());
        }

        private async void ContinuousRecognitionSession_ResultGenerated(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionResultGeneratedEventArgs args)
        {
            await WindowManagerService.Current.MainDispatcher.RunAsync(CoreDispatcherPriority.Normal, () => this.ViewModel.Listening = false);
            if (args.Result.Confidence != SpeechRecognitionConfidence.Rejected)
            {
                await this.speechHandler.Process(args.Result.Text);
            }
            else
            {
                var discardedText = args.Result.Text;
                // this.speechText = discardedText;
            }

            Log.Debug($"ResultGenerated: {args.Result.Confidence.ToString()}: {args.Result.Text}");
        }

        private async void ContinuousRecognitionSession_Completed(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionCompletedEventArgs args)
        {
            if (args.Status != SpeechRecognitionResultStatus.Success)
            {
                Log.Verbose("SessionCompleted: " + args.Status.ToString());
                await WindowManagerService.Current.MainDispatcher.RunAsync(CoreDispatcherPriority.Normal, async () => await this.StartRecognizing(true));
            }
        }
    }
}
