using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BoxKite.Twitter;
using BoxKite.Twitter.Authentication;
using BoxKite.Twitter.Models;

namespace Prototype
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Task t = MainAsync(args);
            t.Wait();
        }

        private static async Task MainAsync(string[] args)
        {
            var twitterCredentials = new TwitterCredentials()
            {
                ConsumerKey = AppSettings.ApplicationConsumerKey,
                ConsumerSecret = AppSettings.ApplicationConsumerSecret,
                Token = AppSettings.UserAccessToken,
                TokenSecret = AppSettings.UserAccessTokenSecret,
            };

            var twitter = new UserSession(twitterCredentials, new DesktopPlatformAdaptor());

            var oathToken = await twitter.StartUserAuthentication();

            Console.Write("pin: ");
            var pin = Console.ReadLine();

            var credentials = await twitter.ConfirmPin(pin, oathToken);

            if (credentials.Valid)
            {
                var session = new UserSession(credentials, new DesktopPlatformAdaptor());
                var checkUser = await session.GetVerifyCredentials();

                if (checkUser.OK)
                {
                    Console.WriteLine(credentials.ScreenName + " is authorised to use BoxKite.Twitter.");

                    var stream = session.StartSearchStream(track: "iwillridewithyou");
                    stream.FoundTweets.Subscribe(
                        tweet =>
                        {
                            if (tweet.Location != null)
                            {
                                tweet.Location.Coordinates.ForEach(x => Console.WriteLine("Coordinates: {0}", x));
                            }

                            Console.WriteLine(String.Format("ScreenName: {0}, Tweet: {1}", tweet.User.ScreenName, tweet.Text));
                        }
                        );
                    stream.Start();
                }
            }

            Thread.Sleep(Timeout.Infinite);
        }
    }
}