using System;
using System.Collections.Generic;
using System.Linq;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Parameters;

namespace TweetExtractor
{
    class Program
    {
        static void Main(string[] args)
        {
            var twitsExtractor = new TwitsExtractor();
            var allTweets = twitsExtractor.GetAllValidTweets("cbsantiago");
            System.IO.File.WriteAllLines("cbsantiago.twits", allTweets.Select(x => x.FullText).ToArray());
        }
    }

    public class TwitsExtractor
    {
        private string CONSUMER_KEY;
        private string CONSUMER_SECRET;
        private string ACCESS_TOKEN;
        private string ACCESS_TOKEN_SECRET;

        private bool isInit = false;

        public TwitsExtractor()
        {
            CONSUMER_KEY = Environment.GetEnvironmentVariable("CONSUMER_KEY");
            CONSUMER_SECRET = Environment.GetEnvironmentVariable("CONSUMER_SECRET");
            ACCESS_TOKEN = Environment.GetEnvironmentVariable("ACCESS_TOKEN");
            ACCESS_TOKEN_SECRET = Environment.GetEnvironmentVariable("ACCESS_TOKEN_SECRET");

            System.Console.WriteLine($"CK: {CONSUMER_KEY}");
            System.Console.WriteLine($"CS: {CONSUMER_SECRET}");
            System.Console.WriteLine($"AT: {ACCESS_TOKEN}");
            System.Console.WriteLine($"AS: {ACCESS_TOKEN_SECRET}");

            if (!string.IsNullOrWhiteSpace(CONSUMER_KEY) &&
            !string.IsNullOrWhiteSpace(CONSUMER_SECRET) &&
            !string.IsNullOrWhiteSpace(ACCESS_TOKEN) &&
            !string.IsNullOrWhiteSpace(ACCESS_TOKEN_SECRET))
            {
                isInit = true;
                Auth.SetUserCredentials(CONSUMER_KEY, CONSUMER_SECRET, ACCESS_TOKEN, ACCESS_TOKEN_SECRET);
            }
        }

        public IEnumerable<ITweet> GetAllValidTweets(string twitterScreenName, Predicate<string> isValidTweet = null)
        {
            if (!isInit)
                throw new Exception("Not logged in to Twitter. Please set the required Authentification Environment Variables: CONSUMER_KEY, CONSUMER_SECRET, ACCESS_TOKEN, ACCESS_TOKEN_SECRET");

            RateLimit.RateLimitTrackerMode = RateLimitTrackerMode.TrackAndAwait;

            RateLimit.QueryAwaitingForRateLimit += (sender, args) =>
            {
                Console.WriteLine($"Query : {args.Query} is awaiting for rate limits!");
            };

            var lastTweets = Timeline.GetUserTimeline(twitterScreenName, 200).ToArray();

            var allTweets = new List<ITweet>(lastTweets);
            var beforeLast = allTweets;

            while (lastTweets.Length > 0 && allTweets.Count <= 3200)
            {
                var idOfOldestTweet = lastTweets.Select(x => x.Id).Min();
                Console.WriteLine($"Oldest Tweet Id = {idOfOldestTweet}");

                var numberOfTweetsToRetrieve = allTweets.Count > 3000 ? 3200 - allTweets.Count : 200;
                var timelineRequestParameters = new UserTimelineParameters
                {
                    // MaxId ensures that we only get tweets that have been posted 
                    // BEFORE the oldest tweet we received
                    MaxId = idOfOldestTweet - 1,
                    MaximumNumberOfTweetsToRetrieve = numberOfTweetsToRetrieve
                };

                lastTweets = Timeline.GetUserTimeline(twitterScreenName, timelineRequestParameters).ToArray();
                if (isValidTweet == null)
                {
                    allTweets.AddRange(lastTweets);
                }
                else
                {
                    foreach (var item in lastTweets)
                    {
                        if (isValidTweet(item.FullText))
                            allTweets.Add(item);
                    }
                }
            }
            return allTweets;
        }
    }
}
