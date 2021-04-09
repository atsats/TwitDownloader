using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwitDownloader
{
    class WorkItem
    {
        const int MinNumTokens = 6;
        public string WordsInputFile { get; set; }
        public string ParsedJsonFile { get; set; }
        public string RawJsonFiles { get; set; }
        public int MaxTweets { get; set; }
        public int MaxSeconds { get; set; }
        public bool Verbose { get; set; }

        public RetVal LoadFromString(string line)
        {
            RetVal retVal = new RetVal();
            try
            {
                if (line != null && line.Trim() != "")
                {
                    string[] tokens = line.Split(new char[] { ',' });
                    if (tokens.Length >= MinNumTokens)
                    {
                        WordsInputFile = tokens[0];
                        ParsedJsonFile = tokens[1];
                        RawJsonFiles = tokens[2];
                        int val = 0;
                        if (!int.TryParse(tokens[3],out val))
                        {
                            retVal.MoreInfo += "Bad MaxTweets value";
                        }
                        else
                        {
                            MaxTweets = val;
                        }
                        if (!int.TryParse(tokens[4], out val))
                        {
                            retVal.MoreInfo += "Bad MaxSeconds value";
                        }
                        else
                        {
                            MaxSeconds = val;
                        }
                        Verbose = false;
                        string verboseStr = tokens[5].Trim().ToUpper();
                        if (verboseStr == "Y" || verboseStr == "YES" || verboseStr == "TRUE")
                            Verbose = true;
                        if (retVal.MoreInfo == "")
                            retVal.Succeeded = true;
                    }
                }
                else
                {
                    retVal.MoreInfo += "Empty string";
                }
            }
            catch (Exception ex)
            {
                retVal.MoreInfo += "Exception in LoadFromString(): " + ex.ToString();
            }
            return retVal;
        }
    }
}
