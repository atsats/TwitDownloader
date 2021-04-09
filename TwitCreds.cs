using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwitDownloader
{
    class TwitCreds
    {
        const int MinNumTokens = 5;
        public string Application { get; set; } = "";
        public string ApiKey { get; set; }  = "";
        public string ApiSecretKey { get; set; } = "";
        public string AccessToken { get; set; } = "";
        public string AccessTokenSecret { get; set; } = "";

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
                        Application = tokens[0];
                        ApiKey = tokens[1];
                        ApiSecretKey = tokens[2];
                        AccessToken = tokens[3];
                        AccessTokenSecret = tokens[4];
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
