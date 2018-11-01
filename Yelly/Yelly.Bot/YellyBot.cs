namespace Yelly.Bot
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;

    using Microsoft.Bot;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Connector;
    using Microsoft.Bot.Connector.Authentication;
    using Microsoft.Bot.Schema;
    using Microsoft.Extensions.Configuration;

    public class YellyBot : IBot
    {
        private const string DirectLineServiceUrl = "https://directline.botframework.com/";
        private static readonly ConcurrentDictionary<string, ITurnContext> ChannelActivities = new ConcurrentDictionary<string, ITurnContext>();
        private readonly ConnectorClient directlineConnector;

        public YellyBot(IConfiguration configuration)
        {
            MicrosoftAppCredentials.TrustServiceUrl(DirectLineServiceUrl, DateTime.Now.AddDays(1));
            var account = new MicrosoftAppCredentials(configuration.GetValue<string>("MicrosoftAppId"), configuration.GetValue<string>("MicrosoftAppPassword"));
            this.directlineConnector = new ConnectorClient(new Uri(DirectLineServiceUrl), account);
        }

        public async Task OnTurn(ITurnContext turnContext)
        {
            // Respond to the various activity types.
            switch (turnContext.Activity.Type)
            {
                case ActivityTypes.Message:
                    // Respond to the incoming text message.
                    await this.RespondToMessage(turnContext);
                    break;

                case ActivityTypes.ConversationUpdate:
                    break;

                case ActivityTypes.ContactRelationUpdate:
                    break;

                case ActivityTypes.Typing:
                    break;

                case ActivityTypes.DeleteUserData:
                    break;
            }
        }

        /// <summary>
        /// Responds to the incoming message by either sending a hero card, an image,
        /// or echoing the user's message.
        /// </summary>
        /// <param name="context">The context of this conversation.</param>
        private async Task RespondToMessage(ITurnContext context)
        {
            if (context.Activity.ChannelId.Equals("directline"))
            {
                ChannelActivities.AddOrUpdate("directLine", context, (key, oldValue) => context);
            }
            else
            {
                //// var connector = directLine.Services.GetServices<IConnectorClient>().FirstOrDefault().Value;
                ChannelActivities.TryGetValue("directLine", out var directLine);
                await this.directlineConnector.Conversations.ReplyToActivityAsync(directLine.Activity.CreateReply(context.Activity.Text));
            }
        }

        //// private IMessageActivity GetMessage(ITurnContext context)
        //// {
        ////     var message = Activity.CreateMessageActivity();
        ////     message.Type = ActivityTypes.Message;
        ////     message.Text = context.Activity.Text;
        ////     message.From = context.Activity.From;
        ////     message.Recipient = new ChannelAccount("yelly", "yelly");
        ////     return message;
        //// }
    }
}