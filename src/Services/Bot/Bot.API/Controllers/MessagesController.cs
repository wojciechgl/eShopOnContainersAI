using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Autofac;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Bot.API.Dialogs;

namespace Bot.API.Controllers
{
    [Route("api/v1/[controller]")]
    [BotAuthentication]
    public class MessagesController : Controller
    {

        private readonly ILogger<MessagesController> _logger;
        private readonly BotSettings _botSettings;

        public MessagesController(ILoggerFactory loggerFactory, IOptionsSnapshot<BotSettings> settings)
        {
            _logger = loggerFactory.CreateLogger<MessagesController>();
            _botSettings = settings.Value;
        }

        // POST api/values
        [HttpPost]
        public virtual async Task<IActionResult> Post([FromBody]Activity activity)
        {
            if (activity != null) {
                // one of these will have an interface and process it
                switch (activity.GetActivityType())
                {
                    case ActivityTypes.Message:
                        await Conversation.SendAsync(activity, () => new EchoDialog());
                        break;

                    case ActivityTypes.ConversationUpdate:
                        await ConversationUpdate(activity);
                        break;
                    case ActivityTypes.ContactRelationUpdate:
                    case ActivityTypes.Typing:
                    case ActivityTypes.DeleteUserData:
                    case ActivityTypes.Ping:
                    default:
                        _logger.LogWarning("Unknown activity type ignored: {0}", activity.GetActivityType());
                        break;
                }
            }
            return new StatusCodeResult((int) HttpStatusCode.Accepted);
        }

        private async Task ConversationUpdate(Activity activity){
            IConversationUpdateActivity update = activity;
            using (var scope = DialogModule.BeginLifetimeScope(Conversation.Container, activity))
            {
                var client = scope.Resolve<IConnectorClient>();
                if (update.MembersAdded.Any())
                {
                    var reply = activity.CreateReply();
                    foreach (var newMember in update.MembersAdded)
                    {
                        if (newMember.Id != activity.Recipient.Id)
                        {
                            reply.Text = $"Welcome {newMember.Name}!";            
                        }
                        else
                        {
                            reply.Text = $"{activity.From.Name} has joined";      
                        }
                        _logger.LogInformation("User joinned: {0}",activity.From.Name);
                        await client.Conversations.ReplyToActivityAsync(reply);
                    }
                }
            }
        }
    }
}