// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.


using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using AdaptiveCards;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace Microsoft.BotBuilderSamples
{

    public class GitHubOAuthDialog : Dialog
    {
        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dialogContext, object options = null, CancellationToken cancellationToken = default)
        {
            var card = new AdaptiveCard("1.0")
            {
                Body = new List<AdaptiveElement>()
                    {
                        new AdaptiveTextBlock("Please authenticate with GitHub")
                        {
                            Size = AdaptiveTextSize.Default,
                            Weight = AdaptiveTextWeight.Bolder
                        }
                    },
                Actions = new List<AdaptiveAction>()
                    {
                        new AdaptiveOpenUrlAction()
                        {
                            Title = "GitHub Login",
                            Url = new Uri("https://github.com/login/oauth/authorize?client_id=Iv1.01f7a933d14bb558&state=abcdefg")
                        }
                    }
            };

            var attachment = new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = card
            };

            var message = MessageFactory.Attachment(attachment);

            await dialogContext.Context.SendActivityAsync(message, cancellationToken);

            return await dialogContext.EndDialogAsync(null, cancellationToken);
        }
    }
}
