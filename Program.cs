using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Tweetinvi;
using Tweetinvi.Models;

namespace TwitDownloader
{
    // This program uses the .net Twitter library here: https://github.com/linvi/tweetinvi
    class Program
    {
        const int MaxRetriesOnStreamExceptions = 50;
        static async Task Main(string[] args)
        {

            // default file name for data to collect
            string workItemsFileName = "workitems.txt";
            string credsFileName = "twitcreds.txt";

            if (args.Length == 1)
                workItemsFileName = args[0];

            if (args.Length == 2)
            {
                credsFileName = args[0];
                workItemsFileName = args[1];
            }

            List<TwitCreds> credsList = LoadTwitterCreds(credsFileName);

            if (credsList != null && credsList.Count > 0)
            {

                List<TwitterClient> twitClientList = new List<TwitterClient>();
                foreach (TwitCreds creds in credsList)
                {
                    var client = new TwitterClient(creds.ApiKey, creds.ApiSecretKey, creds.AccessToken, creds.AccessTokenSecret);
                    var user = await client.Users.GetAuthenticatedUserAsync();
                    if (user != null)
                    {
                        ShowMessage($"You authenticated as {user}!");
                        twitClientList.Add(client);
                    }
                }
                int clientPtr = 0;
                int numClients = twitClientList.Count;
                ShowMessage($"{numClients} available Twitter credentials will be cycled through.");


                List<WorkItem> workItemList = LoadWorkItems(workItemsFileName);

                if (twitClientList.Count > 0)
                {
                    string runFileName = "runstats-" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm") + ".csv";
                    string allWordsFileName = "allWords-" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm") + ".json";

                    var tasks = new List<Task<RunStats>>();

                    using (StreamWriter swRun = new StreamWriter(runFileName))
                    {
                        swRun.WriteLine("ListName,NumTweets,NumSkippedNonMatch,NumSkippedNonEnglish,NumRetries,RunSeconds,TimedOut,Succeeded");
                        foreach (WorkItem w in workItemList)
                        {
                            TweetDownloadParameter tdp = new TweetDownloadParameter(twitClientList[clientPtr], w);
                            tasks.Add(Task<RunStats>.Factory.StartNew(InvokeGetTweets, tdp));
                            //RunStats rs = await GetTweets(tdp);
                            clientPtr = (clientPtr + 1) % numClients;
                        }
                        Task.WaitAll(tasks.ToArray());
                        ShowMessage("All threads exited.");
                        using (StreamWriter swAllFile = new StreamWriter(allWordsFileName))
                        {
                            swAllFile.WriteLine("[");
                            int numTweets = 0;
                            foreach (var t in tasks)
                            {
                                RunStats rs = t.Result;
                                swRun.WriteLine($"{rs.Item.WordsInputFile},{rs.NumTweets},{rs.NumSkippedNonMatch},{rs.NumSkippedNonEnglish},{rs.NumRetries},{rs.RunSeconds},{rs.TimedOut},{rs.Succeeded}");
                                foreach (TweetData td in rs.TweetDataList)
                                {
                                    string jsonString = JsonConvert.SerializeObject(td, Formatting.Indented);
                                    swAllFile.WriteLine(((numTweets++ > 0) ? "," : "") + jsonString);
                                }
                                if (rs.Succeeded)
                                {
                                    ShowMessage($"Captured {rs.NumTweets} for {rs.Item.WordsInputFile} in {rs.RunSeconds} seconds");
                                }
                                else
                                    ShowMessage($"{rs.Item.WordsInputFile} failed!");
                            }
                            swAllFile.WriteLine("]");
                            swAllFile.Close();
                        }
                        swRun.Close();
                    }
                }
                else
                {
                    ShowMessage($"Unable to connect to Twitter with the credentials you supplied in {credsFileName}");
                }
            }
        }



        private static List<TwitCreds> LoadTwitterCreds(string credsFileName)
        {
            List<TwitCreds> credsList = new List<TwitCreds>();
            try
            {
                using (StreamReader sr = new StreamReader(credsFileName))
                {
                    string line = sr.ReadLine();
                    while (line != null)
                    {
                        line = line.Trim();
                        if (line != "" && line[0] != '#')
                        {
                            TwitCreds creds = new TwitCreds();
                            RetVal rv = creds.LoadFromString(line);
                            if (!rv.Succeeded)
                                ShowMessage($"Error loading Twitter credentials {rv.MoreInfo}");
                            else
                                credsList.Add(creds);
                        }
                        line = sr.ReadLine();
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessage("Exception in LoadTwitterCreds(): " + ex.ToString());
            }
            return credsList;
        }

        private static List<WorkItem> LoadWorkItems(string workItemsFileName)
        {
            List<WorkItem> workItemsList = new List<WorkItem>();

            try
            {
                using (StreamReader sr = new StreamReader(workItemsFileName))
                {
                    int lineNum = 0;
                    string line = sr.ReadLine();
                    while (line != null)
                    {
                        lineNum++;
                        line = line.Trim();
                        if (line != "" && line[0] != '#')
                        {
                            WorkItem wi = new WorkItem();
                            RetVal rv = wi.LoadFromString(line);
                            if (rv.Succeeded)
                            {
                                workItemsList.Add(wi);
                            }
                            else
                            {
                                ShowMessage($"Error on {lineNum} {rv.MoreInfo}");
                            }
                        }
                        line = sr.ReadLine();
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessage("Exception in LoadWorkItems(): " + ex.ToString());
            }
            return workItemsList;
        }

        static RunStats InvokeGetTweets(object arg)
        {
            TweetDownloadParameter par = (TweetDownloadParameter)arg;
            return GetTweets(par).Result;
        }

        static async Task<RunStats> GetTweets(TweetDownloadParameter par) 
        //static RunStats GetTweets(object arg)
        {
            int numTweets = 0;
            int numRecords = 0;
            int numSkippedNonMatch = 0;
            int numSkippedNonEnglish = 0;
            RunStats rs = new RunStats();

            TwitterClient client = par.Client;
            WorkItem workItem = par.WorkItem;
            List<TweetData> tweetDataList = new List<TweetData>();
            rs.Item = workItem;
            int numRetries = 0;

            try
            {
                string[] words = LoadWords(workItem.WordsInputFile);
                if (words.Length > 0)
                {
                    bool filesOpen = false;
                    using (StreamWriter swRawJsonFile = new StreamWriter(workItem.RawJsonFiles))
                    using (StreamWriter swParsedJsonFile = new StreamWriter(workItem.ParsedJsonFile))
                    {
                        swRawJsonFile.WriteLine("[");
                        swParsedJsonFile.WriteLine("[");

                        filesOpen = true;
ReTry:
                        var stream = client.Streams.CreateFilteredStream();

                        for (int i = 0; i < words.Length; i++)
                        {
                            words[i] = words[i].Trim();
                            if (words[i] != "")
                                stream.AddTrack(words[i]);
                        }
                        DateTime startTime = DateTime.Now;
                        // this is required for well-formed JSON
                        stream.MatchingTweetReceived += (sender, eventReceived) =>
                        {
                            swRawJsonFile.WriteLine(((numRecords > 0) ? "," : "") + eventReceived.Json);
                            numRecords++;
                            if (eventReceived.MatchingTracks.Length > 0)
                            {
                                DateTime currentTime = DateTime.Now;
                                TimeSpan tsElapsed = new TimeSpan(currentTime.Ticks - startTime.Ticks);
                                ITweet t = eventReceived.Tweet;
                                TweetData td = new TweetData(workItem.WordsInputFile, eventReceived.MatchingTracks, t, eventReceived.Json);
                                if (td.Lang == null || td.Lang.Equals("en", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    string rawJsonString = JsonConvert.SerializeObject(td, Formatting.Indented);
                                    swParsedJsonFile.WriteLine(((numTweets > 0) ? "," : "") + rawJsonString);
                                    numTweets++;
                                    tweetDataList.Add(td);
                                    if (workItem.Verbose)
                                    {
                                        Console.Write($"{workItem.WordsInputFile} {numTweets} received,{numSkippedNonMatch} non-match,{numSkippedNonEnglish} non-English         \r");
                                    }
                                }
                                else
                                {
                                    numSkippedNonEnglish++;
                                }

                                bool stopNow = (numTweets >= workItem.MaxTweets);

                                if (workItem.MaxSeconds > 0)
                                    stopNow |= (tsElapsed.TotalSeconds > workItem.MaxSeconds);

                                if (stopNow)
                                {
                                    Console.Write($"{workItem.WordsInputFile} {numTweets} received,{numSkippedNonMatch} non-match,{numSkippedNonEnglish} non-English\n");
                                    stream.Stop();
                                }
                            }
                            else
                            {
                                numSkippedNonMatch++;
                            }
                        };

                        try
                        {
                            await stream.StartMatchingAnyConditionAsync();
                            rs.Succeeded = true;
                        }
                        catch (Exception ex)
                        {
                            ShowMessage($"Exception while in await StartMatchingAnyConditionAsync(): {ex.ToString()}");
                            if (numRetries < MaxRetriesOnStreamExceptions)
                            {
                                numRetries++;
                                ShowMessage($"{workItem.WordsInputFile} Retry #{numRetries} out of {MaxRetriesOnStreamExceptions}");
                                Thread.Sleep(5000);
                                goto ReTry;
                            }
                            else
                            {
                                ShowMessage($"Out of retries on {workItem.WordsInputFile}" );
                            }
                        }
                        if (filesOpen)
                        {
                            swRawJsonFile.WriteLine("]");
                            swParsedJsonFile.WriteLine("]");
                            swRawJsonFile.Close();
                            swParsedJsonFile.Close();
                        }
                        rs.TweetDataList = tweetDataList;
                        rs.NumTweets = numTweets;
                        rs.NumSkippedNonMatch = numSkippedNonMatch;
                        rs.NumSkippedNonEnglish = numSkippedNonEnglish;
                        rs.NumRetries = numRetries;
                        TimeSpan ts = new TimeSpan(DateTime.Now.Ticks - startTime.Ticks);
                        rs.RunSeconds = (int)Math.Round(ts.TotalSeconds, 0);
                        if (workItem.MaxSeconds > 0)
                        {
                            rs.TimedOut = (ts.TotalSeconds > workItem.MaxSeconds) && (rs.NumTweets < workItem.MaxTweets);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"Exception in GetTweets() {ex.ToString()}");
            }
            return rs;
        }

        private static string[] LoadWords(string wordsInputFile)
        {
            string[] words = null;
            try
            {
                using (StreamReader sr = new StreamReader(wordsInputFile))
                {
                    string line = sr.ReadLine();
                    words = line.Split(new char[] { ',' });
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"Exception in LoadWords() {ex.ToString()}");
            }
            return words;
        }

        private static void ShowMessage(string msg)
        {
            Console.WriteLine(msg);
        }
    }
}
