// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Linq;

namespace Microsoft.BotBuilderSamples
{
    // This IBot implementation can run any type of Dialog. The use of type parameterization is to allows multiple different bots
    // to be run at different endpoints within the same project. This can be achieved by defining distinct Controller types
    // each with dependency on distinct IBot types, this way ASP Dependency Injection can glue everything together without ambiguity.
    // The ConversationState is used by the Dialog system. The UserState isn't, however, it might have been used in a Dialog implementation,
    // and the requirement is that all BotState objects are saved at the end of a turn.
    public class DialogBot<T> : ActivityHandler where T : Dialog
    {
        protected readonly BotState ConversationState;
        protected readonly Dialog Dialog;
        protected readonly ILogger Logger;
        protected readonly BotState UserState;

        public DialogBot(ConversationState conversationState, UserState userState, T dialog, ILogger<DialogBot<T>> logger)
        {
            ConversationState = conversationState;
            UserState = userState;
            Dialog = dialog;
            Logger = logger;
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            await base.OnTurnAsync(turnContext, cancellationToken);

            // Save any state changes that might have occurred during the turn.
            await ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await UserState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        protected override async Task<InvokeResponse> OnInvokeActivityAsync(ITurnContext<IInvokeActivity> turnContext, CancellationToken cancellationToken)
        {
            if (turnContext.Activity.Name == "signin/verifyState")
            {
                // Handle the OAuth card verification
                await Dialog.RunAsync(turnContext, ConversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken);

                // Acknowledge the invoke activity
                return new InvokeResponse { Status = 200 };
            }

            return await base.OnInvokeActivityAsync(turnContext, cancellationToken);
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {

            if (turnContext.Activity.Text.Contains("login", StringComparison.OrdinalIgnoreCase))
            {
                // Assuming you have the UserTokenClient set up in your adapter's turn context.
                var userTokenClient = turnContext.TurnState.Get<UserTokenClient>();
                var tokenResponse = await userTokenClient.GetUserTokenAsync(turnContext.Activity.From.Id, "github-login", turnContext.Activity.ChannelId, null, cancellationToken);

                if (tokenResponse != null && !string.IsNullOrEmpty(tokenResponse.Token))
                {
                    // User has a valid token, send it back in the chat.
                    await turnContext.SendActivityAsync(MessageFactory.Text($"You are already logged in. Your token is: {tokenResponse.Token}"), cancellationToken);
                    return;
                }
            }
            else if (turnContext.Activity.Text.Contains("whoami", StringComparison.OrdinalIgnoreCase))
            {
                // Assuming you have the UserTokenClient set up in your adapter's turn context.
                var userToken = await FetchGithubToken(turnContext, cancellationToken);
                if (string.IsNullOrEmpty(userToken))
                {
                    // Trigger login flow
                    await Dialog.RunAsync(turnContext, ConversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken);
                }

                userToken = await FetchGithubToken(turnContext, cancellationToken);
                // User has a valid token, print it to the chat.
                await turnContext.SendActivityAsync(MessageFactory.Text($"You are logged in. Your token is: {userToken}"), cancellationToken);

                var userDetails = await GetUserDetails(userToken);
                await turnContext.SendActivityAsync(MessageFactory.Text($"{userDetails}"), cancellationToken);
                return;
            }
            Logger.LogInformation("Running dialog with Message Activity.");

            // Run the Dialog with the new message Activity.
            await Dialog.RunAsync(turnContext, ConversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken);
        }

        private async Task<string> FetchGithubToken(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            // Assuming you have the UserTokenClient set up in your adapter's turn context.
            var userTokenClient = turnContext.TurnState.Get<UserTokenClient>();
            var tokenResponse = await userTokenClient.GetUserTokenAsync(turnContext.Activity.From.Id, "github-login", turnContext.Activity.ChannelId, null, cancellationToken);

            return tokenResponse.Token;
        }

        private async Task<string> GetUserDetails(string userToken)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
                httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("YourAppName", "1.0"));

                var response = await httpClient.GetAsync("https://api.github.com/user");
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var user = JsonConvert.DeserializeObject<dynamic>(jsonResponse);
                    var userDetails = $"Name: {user.name}\nLogin: {user.login}\nEmail: {user.email}\nBio: {user.bio}";
                    return userDetails;
                }
                else
                {
                    return "Unable to retrieve user details.";
                }

            }
        }

    }
}
