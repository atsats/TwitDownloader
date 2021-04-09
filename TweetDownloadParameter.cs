using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tweetinvi;

namespace TwitDownloader
{
    class TweetDownloadParameter
    {
        public TwitterClient Client { get; set; }
        public WorkItem WorkItem { get; set; }

        public TweetDownloadParameter(TwitterClient client, WorkItem workItem)
        {
            Client = client;
            WorkItem = workItem;
        }

    }
}
