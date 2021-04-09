using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwitDownloader
{
    class RunStats
    {
        public WorkItem Item { get; set; }
        public bool Succeeded { get; set; } = false;
        public int NumTweets { get; set; } = 0;
        public int NumSkippedNonMatch { get; set; } = 0;
        public int NumSkippedNonEnglish { get; set; } = 0;
        public int NumRetries { get; set; } = 0;
        public int RunSeconds { get; set; } = 0;
        public bool TimedOut { get; set; } = false;

        public List<TweetData> TweetDataList = new List<TweetData>();

    }
}
