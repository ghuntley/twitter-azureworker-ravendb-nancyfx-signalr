using System;
using System.IO;
using System.Net;
using System.Reactive;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using BoxKite.Twitter;
using BoxKite.Twitter.Models;
using Microsoft.WindowsAzure.ServiceRuntime;
using Newtonsoft.Json;
using Polly;
using Raven.Client;
using Raven.Client.Document;
using Serilog;
using Serilog.Events;

namespace ArchiveTweetStream
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        private ILogger logger;
        private IDocumentStore store;
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        public override void Run()
        {
            Log.Information("ArchiveTweetStream is running.");

            try
            {
                RunAsync(cancellationTokenSource.Token).Wait();
            }
            finally
            {
                runCompleteEvent.Set();
            }
        }

        public override bool OnStart()
        {
            logger = new LoggerConfiguration()
                .Destructure.UsingAttributes()
                .MinimumLevel.Debug()
                .WriteTo.Trace()
                .WriteTo.Logentries(AppSettings.LogEntriesToken)
                .CreateLogger();
            Log.Logger = logger;

            store = new DocumentStore()
            {
                ConnectionStringName = "RavenDB"
            }.Initialize();

            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            bool result = base.OnStart();

            Log.Information("ArchiveTweetStream has been started.");

            return result;
        }

        public override void OnStop()
        {
            Log.Information("ArchiveTweetStream is stopping.");

            cancellationTokenSource.Cancel();
            runCompleteEvent.WaitOne();

            base.OnStop();

            Log.Information("ArchiveTweetStream has stopped.");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            const string filename = "credentials.json";

            var json = File.ReadAllText(filename);
            var credentials = JsonConvert.DeserializeObject<TwitterCredentials>(json);

            var session = new UserSession(credentials, new DesktopPlatformAdaptor());
            User user = await session.GetVerifyCredentials();

            if (user.OK)
            {
                Log.Information("Successfully logged into twitter as {screenName}", user.ScreenName);

                var stream = session.StartSearchStream(AppSettings.Hashtag);
                stream.FoundTweets.Subscribe(async tweet => await SaveTweet(tweet));

                stream.Start();

                Log.Information("Successfully started a streaming subscription to {hashtag}", AppSettings.Hashtag);

            }
            else
            {
                Log.Error("Shutting down due to authentication failure: {@user}", user);
                throw new AuthenticationException();
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                Log.Information("ArchiveTweetStream is still working.");

                await Task.Delay(TimeSpan.FromMinutes(1));
            }
        }

        private async Task SaveTweet(Tweet tweet)
        {
            await Policy
                .Handle<Exception>()
                .RetryForeverAsync()
                .ExecuteAsync(async () =>
                {
                    using (
                        logger.BeginTimedOperation("Saving tweet to the database.", tweet.Id.ToString(),
                            LogEventLevel.Debug))
                    {
                        try
                        {
                            using (var session = store.OpenAsyncSession())
                            {
                                // Operations against session
                                await session.StoreAsync(tweet);

                                // Flush those changes
                                await session.SaveChangesAsync();
                            }

                            Log.Verbose("{@tweet}", tweet);
                        }
                        catch (Exception ex)
                        {
                            Log.Warning("Failed to save {@tweet} to the database, will retry.", tweet);
                            throw;
                        }
                    }

                    Log.Information("Tweet {Id} has been saved to the database.", tweet.Id);
                });
        }
    }
}