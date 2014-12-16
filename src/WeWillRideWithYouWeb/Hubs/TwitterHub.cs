using System;
using BoxKite.Twitter.Models;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Raven.Abstractions.Data;
using Raven.Client;

namespace WeWillRideWithYouWeb.Hubs
{
    [HubName("twitterHub")]
    public class TwitterHub : Hub
    {
        private readonly IDocumentStore _documentStore;

        public TwitterHub(IDocumentStore documentStore)
        {
            _documentStore = documentStore;

            _documentStore.Changes().ForDocumentsStartingWith("tweets/").Subscribe(async change =>
            {
                if (change.Type == DocumentChangeTypes.Put)
                {
                    Tweet tweet;

                    using (var session = _documentStore.OpenAsyncSession())
                    {
                        tweet = await session.LoadAsync<Tweet>(change.Id);
                    }

                    Send(tweet);
                }
            });
        }

        public void Send(Tweet tweet)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<TwitterHub>();
            context.Clients.All.addTweet(tweet);
        }
    }
}