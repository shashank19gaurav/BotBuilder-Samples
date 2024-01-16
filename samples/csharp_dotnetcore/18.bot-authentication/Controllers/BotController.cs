// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using System;
using System.Net.Http;
using System.Collections.Generic;

namespace Microsoft.BotBuilderSamples
{
    // This ASP Controller is created to handle a request. Dependency Injection will provide the Adapter and IBot
    // implementation at runtime. Multiple different IBot implementations running at different endpoints can be
    // achieved by specifying a more specific type for the bot constructor argument.
    [Route("api/messages")]
    [ApiController]
    public class BotController : ControllerBase
    {
        private readonly IBotFrameworkHttpAdapter _adapter;
        private readonly IBot _bot;

        public BotController(IBotFrameworkHttpAdapter adapter, IBot bot)
        {
            _adapter = adapter;
            _bot = bot;
        }

        [HttpPost]
        public async Task PostAsync()
        {
            // Delegate the processing of the HTTP POST to the adapter.
            // The adapter will invoke the bot.
            await _adapter.ProcessAsync(Request, Response, _bot);
        }

        [HttpGet]
        public async Task GetAsync()
        {
            // Delegate the processing of the HTTP POST to the adapter.
            // The adapter will invoke the bot.

            var code = Request.Query["code"].ToString();

            // await _adapter.ProcessAsync(Request, Response, _bot);

            // Use HttpClient or another HTTP client library to make the POST request
            using (var httpClient = new HttpClient())
            {
                var requestContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("client_id", "Iv1.01f7a933d14bb558"),
                    new KeyValuePair<string, string>("client_secret", "83b0b8d1c390a98007a741b45ddf194c11772e93"),
                    new KeyValuePair<string, string>("code", code),
                    // Optional: new KeyValuePair<string, string>("redirect_uri", "<Your Redirect URI>"),
                    // Optional: new KeyValuePair<string, string>("repository_id", "<Repository ID>")
                });

                // Send the request and get the response
                var response = await httpClient.PostAsync("https://github.com/login/oauth/access_token", requestContent);
                var responseString = await response.Content.ReadAsStringAsync();

                // The responseString will contain the access token
                // Extract and use the access token as needed
            }

            // Additional logic to handle the bot's response
        }

    }
}
