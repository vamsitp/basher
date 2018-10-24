namespace Basher.Services
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using Serilog;

    using Windows.Media.Core;
    using Windows.Media.Playback;
    using Windows.Media.SpeechSynthesis;
    using Windows.UI.Xaml.Controls;

    public class SpeechService
    {
        private readonly MediaPlayer mediaPlayer = null;
        private readonly MediaElement mediaElement = null;

        public SpeechService()
        {
            this.mediaPlayer = new MediaPlayer() { Volume = 0.2 };
            this.mediaElement = new MediaElement() { Volume = 1 }; // DefaultPlaybackRate = 1.25
        }

        public async Task Play(string sound, string speechText, bool loading = false)
        {
            if (loading)
            {
                return;
            }

            Log.Information(sound + ": " + speechText);
            this.PlaySound(sound);
            await this.PlaySpeech(speechText);
        }

        public void PlaySound(string sound)
        {
            if (!string.IsNullOrWhiteSpace(sound))
            {
                this.mediaPlayer.Source = MediaSource.CreateFromUri(new Uri($"ms-appx:///Assets/{sound}.mp3"));
                this.mediaPlayer.Play();
            }
        }

        public async Task PlaySpeech(string speechText)
        {
            if (!string.IsNullOrWhiteSpace(speechText))
            {
                using (var speech = new SpeechSynthesizer())
                {
                    var voice = SpeechSynthesizer.AllVoices.FirstOrDefault(gender => gender.Gender == VoiceGender.Female && gender.Language.Equals(App.Settings.SpeechLocale, StringComparison.OrdinalIgnoreCase));
                    speech.Voice = voice ?? SpeechSynthesizer.AllVoices.FirstOrDefault(gender => gender.Gender == VoiceGender.Female);
                    var stream = await speech.SynthesizeTextToStreamAsync(speechText);
                    await WindowManagerService.Current.MainDispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        this.mediaElement.SetSource(stream, stream.ContentType);
                        this.mediaElement.Play();
                    });
                }
            }
        }
    }
}
