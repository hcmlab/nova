using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Security;

namespace ssi.Tools
{

    public enum NIP90TASK
    {
        SPEECHTOTEXT = 65002,
        TRANSLATION = 65003,
        SUMMARIZATION = 65004,
        TEXTTOIMAGE = 65005
    }

    public enum NIP90INPUT_TYPE
    {
        URL,
        TEXT,
        EVENT,
        JOB
    }


    class NovaServerNostr
    {
        public NovaServerNostr(string message) {

            string novadvm_pubkey = "npub1cc79kn3phxc7c6mn45zynf4gtz0khkz59j4anew7dtj8fv50aqrqlth2hf";

            MultipartFormDataContent content = new MultipartFormDataContent
            {

                { new StringContent(message), "message" },
                { new StringContent(novadvm_pubkey), "receiver" }
            };

            NostrRequest(content);

        }

        public async Task<Dictionary<string, string>> NostrRequest(MultipartFormDataContent content)
        {
                try
                {
                HttpClient client = new HttpClient();
                    string[] tokens = Properties.Settings.Default.NovaServerAddress.Split(':');
                    string url = "http://" + tokens[0] + ":" + tokens[1] + "/nostr";
                    var response = await client.PostAsync(url, content);

                    var responseString = await response.Content.ReadAsStringAsync();
                    var explanationDic = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseString);

                    if (explanationDic["success"] == "failed")
                    {
                        return null;
                    }

                    return explanationDic;
                }
                catch (Exception)
                {
                    return null;
                }
            }

        

    }
}
