using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BoxKite.Twitter;
using BoxKite.Twitter.Authentication;
using BoxKite.Twitter.Models;
using Newtonsoft.Json;

namespace TwitterAuthenticator
{
    class Program
    {
        static void Main(string[] args)
        {
            Task t = MainAsync(args);
            t.Wait();
        }

        private async static Task MainAsync(string[] args)
        {
            var twitterCredentials = new TwitterCredentials()
            {
                ConsumerKey = AppSettings.ApplicationConsumerKey,
                ConsumerSecret = AppSettings.ApplicationConsumerSecret,
            };

            var twitter = new UserSession(twitterCredentials, new DesktopPlatformAdaptor());

            var oathToken = await twitter.StartUserAuthentication();

            Console.Write("Please enter the PIN as displayed in the new browser window: ");
            var pin = Console.ReadLine();

            var credentials = await twitter.ConfirmPin(pin, oathToken);

            if (credentials.Valid)
            {
                string json = JsonConvert.SerializeObject(credentials);

                File.WriteAllText(@"credentials.json", json);
                Console.WriteLine("Credentials were successfully saved.");
            }
            else
            {
                Console.WriteLine("Credentials were invalid.");
            }

            Console.ReadLine();
        }
    }
}
