namespace Yell
{
    using System;
    using System.Configuration;
    using System.Globalization;
    using System.Speech.Synthesis;

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("\n> Type any phrase and hit 'ENTER' to Yell\n> Press 'CTRL + C' to quit\n");
            using (var synth = new SpeechSynthesizer())
            {
                SetSpeech(synth);
                string input;
                while ((input = Console.ReadLine()) != null)
                {
                    synth.Speak(input);
                }
            }
        }

        private static void SetSpeech(SpeechSynthesizer synth)
        {
            synth.SetOutputToDefaultAudioDevice();
            Enum.TryParse<VoiceGender>(ConfigurationManager.AppSettings["VoiceGender"], true, out var voiceGender);
            Enum.TryParse<VoiceAge>(ConfigurationManager.AppSettings["VoiceAge"], true, out var voiceAge);
            int.TryParse(ConfigurationManager.AppSettings["VoiceAlternate"], out var voiceAlternate);
            synth.SelectVoiceByHints(voiceGender, voiceAge, voiceAlternate, CultureInfo.GetCultureInfo(ConfigurationManager.AppSettings["CultureInfo"]));
        }
    }
}
