namespace Yelly
{
    using System;
    using System.Configuration;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Speech.Synthesis;
    using System.Threading.Tasks;
    using Microsoft.Bot.Connector.DirectLine;
    using Newtonsoft.Json;

    class Program
    {
        private const string YellyStarted = "Yelly started!";
        private readonly static string BotId = ConfigurationManager.AppSettings["BotId"];
        private readonly static string DirectLineSecret = ConfigurationManager.AppSettings["DirectLineSecret"];
        private readonly static string FromUser = ConfigurationManager.AppSettings["BotUser"];
        private readonly static SpeechSynthesizer Synth = new SpeechSynthesizer();

        static void Main(string[] args)
        {
            Console.BackgroundColor = ConsoleColor.DarkRed;
            Console.WriteLine("\n> Hi, this is Yelly!\n> Type any phrase and hit 'ENTER' to Yell\n> Press 'CTRL + C' or type 'EXIT' and hit 'ENTER' to quit\n");
            Console.BackgroundColor = ConsoleColor.Black;
            SetSpeech(Synth);
            StartBotConversation().GetAwaiter().GetResult();
        }

        private static async Task StartBotConversation()
        {
            // Create a new Direct Line client.
            var client = new DirectLineClient(DirectLineSecret);

            // Start the conversation.
            var conversation = await client.Conversations.StartConversationAsync();

            // Start the bot message reader in a separate thread.
            new System.Threading.Thread(async () => await ReadBotMessagesAsync(client, conversation.ConversationId)).Start();

            await SendMessage(client, conversation, YellyStarted);

            // Loop until the user chooses to exit this loop.
            while (true)
            {
                // Accept the input from the user.
                string input = Console.ReadLine().Trim();

                // Check to see if the user wants to exit.
                if (input.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    // Exit the app if the user requests it.
                    break;
                }
                else
                {
                    Synth.SpeakAsync(input);
                }
            }
        }

        private static Task SendMessage(DirectLineClient client, Conversation conversation, string input)
        {
            // Create a message activity with the text the user entered.
            var userMessage = new Activity
            {
                From = new ChannelAccount(FromUser, FromUser),
                Text = input,
                Type = ActivityTypes.Message
            };

            // Send the message activity to the bot.
            return client.Conversations.PostActivityAsync(conversation.ConversationId, userMessage);
        }


        /// <summary>
        /// Polls the bot continuously and retrieves messages sent by the bot to the client.
        /// </summary>
        /// <param name="client">The Direct Line client.</param>
        /// <param name="conversationId">The conversation ID.</param>
        /// <returns></returns>
        private static async Task ReadBotMessagesAsync(DirectLineClient client, string conversationId)
        {
            string watermark = null;

            // Poll the bot for replies once per second.
            while (true)
            {
                // Retrieve the activity set from the bot.
                try
                {
                    var activitySet = await client.Conversations.GetActivitiesAsync(conversationId, watermark);
                    watermark = activitySet?.Watermark;

                    // Extract the activities sent from our bot.
                    var activities = from x in activitySet.Activities
                                     where x.From.Id == BotId
                                     select x;

                    // Analyze each activity in the activity set.
                    foreach (var activity in activities)
                    {
                        var message = activity.Text;
                        if (!message.Equals(YellyStarted))
                        {
                            Console.BackgroundColor = ConsoleColor.DarkCyan;
                            Console.WriteLine(message);
                            Console.BackgroundColor = ConsoleColor.Black;
                            Synth.SpeakAsync(message);
                        }
                    }

                    // Wait for one second before polling the bot again.
                    await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private static void SetSpeech(SpeechSynthesizer synth)
        {
            //// var voices = synth.GetInstalledVoices();
            synth.SetOutputToDefaultAudioDevice();
            Enum.TryParse<VoiceGender>(ConfigurationManager.AppSettings["VoiceGender"], true, out var voiceGender);
            Enum.TryParse<VoiceAge>(ConfigurationManager.AppSettings["VoiceAge"], true, out var voiceAge);
            int.TryParse(ConfigurationManager.AppSettings["VoiceAlternate"], out var voiceAlternate);
            synth.SelectVoiceByHints(voiceGender, voiceAge, voiceAlternate, CultureInfo.GetCultureInfo(ConfigurationManager.AppSettings["CultureInfo"]));
        }
    }
}
