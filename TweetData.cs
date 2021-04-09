using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Models.DTO;
using Tweetinvi.Models.Entities;

namespace TwitDownloader
{
    class TweetData
    {
        public string Category { get; set; }
        public int NumMatchWords { get; set; }
        public string MatchWords { get; set; }
        public string TweetUserScreenName { get; set; }
        public int TweetUserDaysOn { get; set; }
        public int TweetUserNumFollowers { get; set; }
        public int TweetUserNumFollowing { get; set; }
        public decimal?  TweetUserFollowRatio { get; set; }
        public int TweetUserNumTweets { get; set; }
        public int TweetUserNumFavorites { get; set; }
        public bool TweetUserVerified { get; set; }
        public bool TweetUserProtected { get; set; }
        public bool TweetUserDefaultProfilePic { get; set; }
        public int? TweetNumQuoted { get; set; }
        public int? TweetNumReplied { get; set; }
        public int TweetNumFavorite { get; set; }
        public int TweetNumRetweet { get; set; }

        public bool IsRetweet { get; set; }
        public int? NumMedia { get; set; }
        public int? NumUrls { get; set; }
        public int? NumHashtags { get; set; }
        public int? NumContributors { get; set; }
        public int? NumUserMentions { get; set; }
        public int? NumSymbols { get; set; }
        public bool PossiblySensitive { get; set; }
        public string Source { get; set; }
        public string SourceUrl { get; set; }
        public string Place { get; set; }
        public double? Longitude { get; set; }
        public double? Latitude { get; set; }
        public string TweetId { get; set; }
        public DateTimeOffset TweetCreatedAt { get; set; }
        public string ReTweetUserScreenName { get; set; }
        public int? ReTweetUserDaysOn { get; set; }
        public int? ReTweetUserNumFollowers { get; set; }
        public int? ReTweetUserNumFollowing { get; set; }
        public decimal? ReTweetUserFollowRatio { get; set; }
        public int? ReTweetUserNumTweets { get; set; }
        public int? ReTweetUserNumFavorites { get; set; }
        public int? ReTweetNumQuoted { get; set; }
        public int? ReTweetNumReplied { get; set; }
        public int? ReTweetNumFavorite { get; set; }
        public int? ReTweetNumRetweet { get; set; }

        public bool? ReTweetUserVerified { get; set; }
        public bool? ReTweetUserProtected { get; set; }
        public bool? ReTweetUserDefaultProfilePic { get; set; }
        public string Lang { get; set; }
        public string TweetText { get; set; }
        public string ReTweetText { get; set; }

        public TweetData(string category,string [] matchWords, ITweet t, string rawJson)
        {
            Category = (category.ToUpper().Trim().Replace(".TXT", "")).ToLower(); 
            MatchWords = "";
            NumMatchWords = 0;
            foreach (string word in matchWords)
            {
                NumMatchWords++;
                MatchWords += " " + word;
            }
            MatchWords = MatchWords.Trim();

            JObject tweetObj = JObject.Parse(rawJson);
            Lang = (string)tweetObj.SelectToken("lang");


            TweetUserScreenName = t.CreatedBy.ScreenName;
            TweetUserDaysOn = (int)Math.Round((new TimeSpan(DateTime.Now.Ticks - t.CreatedBy.CreatedAt.Ticks)).TotalDays, 0);
            TweetUserNumFollowers = t.CreatedBy.FollowersCount;
            TweetUserNumFollowing = t.CreatedBy.FriendsCount;
            TweetUserFollowRatio = GetFollowRatio(t.CreatedBy);
            TweetUserNumTweets = t.CreatedBy.StatusesCount;
            TweetUserNumFavorites = t.CreatedBy.FavoritesCount;
            TweetUserVerified = t.CreatedBy.Verified;
            TweetUserProtected = t.CreatedBy.Protected;
            TweetUserDefaultProfilePic = t.CreatedBy.DefaultProfileImage;


            TweetId = t.IdStr;
            TweetCreatedAt = t.CreatedAt;

            IsRetweet = t.IsRetweet;
            NumMedia = t.Media?.Count;
            NumUrls = t.Urls?.Count;
            NumHashtags = t.Hashtags?.Count;
            NumContributors = t.Contributors?.Count();
            NumUserMentions = t.UserMentions?.Count;
            NumSymbols = t.Entities?.Symbols?.Count;
            TweetNumQuoted = t.QuoteCount;
            TweetNumReplied = t.ReplyCount;
            TweetNumFavorite =  t.FavoriteCount;
            TweetNumRetweet =  t.RetweetCount;
            PossiblySensitive =  t.PossiblySensitive;
            Source =  GetSource(t.Source);
            if (Source.Equals("OTHER",StringComparison.InvariantCultureIgnoreCase) || Source.Equals("BOT",StringComparison.InvariantCultureIgnoreCase))
            {
                SourceUrl = t.Source;
            }
            Place =  t.Place?.FullName;
            Longitude =  t.Coordinates?.Longitude;
            Latitude =  t.Coordinates?.Latitude;


            ReTweetUserScreenName = t.RetweetedTweet?.CreatedBy?.ScreenName;
            if (t.RetweetedTweet != null)
                ReTweetUserDaysOn = (int)Math.Round((new TimeSpan(DateTime.Now.Ticks - t.RetweetedTweet.CreatedBy.CreatedAt.Ticks)).TotalDays, 0);
            ReTweetUserNumFollowers = t.RetweetedTweet?.CreatedBy?.FollowersCount;
            ReTweetUserNumFollowing = t.RetweetedTweet?.CreatedBy?.FriendsCount;
            ReTweetUserFollowRatio = GetFollowRatio(t.RetweetedTweet?.CreatedBy);
            ReTweetUserVerified = t.RetweetedTweet?.CreatedBy.Verified;
            ReTweetUserProtected = t.RetweetedTweet?.CreatedBy.Protected;
            ReTweetUserNumTweets = t.RetweetedTweet?.CreatedBy.StatusesCount;
            ReTweetUserNumFavorites = t.RetweetedTweet?.CreatedBy.FavoritesCount;
            ReTweetUserDefaultProfilePic = t.RetweetedTweet?.CreatedBy.DefaultProfileImage;
            ReTweetNumQuoted = t.RetweetedTweet?.QuoteCount;
            ReTweetNumReplied = t.RetweetedTweet?.ReplyCount;
            ReTweetNumFavorite = t.RetweetedTweet?.FavoriteCount;
            ReTweetNumRetweet = t.RetweetedTweet?.RetweetCount;


            TweetText = t.FullText;
            ReTweetText = t.RetweetedTweet?.Text;

        }

        private decimal? GetFollowRatio(IUser u)
        {
            decimal? ratio = null;
            if (u != null && u.FollowersCount > 0)
                ratio = Math.Round((decimal)((decimal)u.FriendsCount) / ((decimal)u.FollowersCount),5);
            return ratio;
        }

        private string GetSource(string sourceUrl)
        {
            string source = sourceUrl;
            sourceUrl = sourceUrl.ToUpper();
            if (sourceUrl.Contains("IPHONE"))
                source = "iPhone";
            else if (sourceUrl.Contains("ANDROID"))
                source = "Android";
            else if (sourceUrl.Contains("IPAD"))
                source = "iPad";
            else if (sourceUrl.Contains("WEB APP"))
                source = "WebApp";
            else if (sourceUrl.Contains("BOT"))
                source = "Bot";
            else if (sourceUrl.Contains("TWEETDECK"))
                source = "TweetDeck";
            else if (sourceUrl.Contains("TWEETLOGIX"))
                source = "TweetLogix";
            else if (sourceUrl.Contains("WINDOWS"))
                source = "Windows";
            else if (sourceUrl.Contains(" MAC"))
                source = "Mac";
            else if (sourceUrl.Contains("IFTTT"))
                source = "IFTTT";
            else if (sourceUrl.Contains("RADIOYOU"))
                source = "RadioYou";
            else
            {
                source = "OTHER";
            }
            return source;
        }
    }

}
