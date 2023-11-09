using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ssi.Controls.Other.NovaServerUtility
{
    public static class NovaServerUtility
    {


        public static int GetDeterministicHashCode(string str)
        {
            unchecked
            {
                int hash1 = (5381 << 16) + 5381;
                int hash2 = hash1;

                for (int i = 0; i < str.Length; i += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ str[i];
                    if (i == str.Length - 1)
                        break;
                    hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
                }

                return hash1 + (hash2 * 1566083941);
            }
        }

        public static async Task<Dictionary<string, string>> getInfoFromServer(string jobID, string url, HttpClient client)
        {

            MultipartFormDataContent contentStatus = new MultipartFormDataContent
                {
                    { new StringContent(jobID), "jobID"  }
            };

            var response = client.PostAsync(url, contentStatus).Result;
            var responseContent = response.Content;
            string responseString = responseContent.ReadAsStringAsync().Result;
            var explanationDic = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseString);

            return explanationDic;
        }

    }
}
