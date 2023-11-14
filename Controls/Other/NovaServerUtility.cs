using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static ssi.DatabaseCMLExtractFeaturesWindow;
using static ssi.DatabaseNovaServerWindow;

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

        public static Processor parseTrainerFile(KeyValuePair<string,JToken> trainerDictionary)
        {
            try
            {
                Processor processor = new Processor();
                JObject chainobject = JObject.Parse(trainerDictionary.Value.ToString());
                processor.Name = Path.GetFileName(trainerDictionary.Key);
                processor.Path = trainerDictionary.Key;
                processor.LeftContext = "0";
                processor.RightContext = "0";
                processor.FrameStep = "0";
                processor.Category = "0";
                processor.Description = "";
                processor.Backend = "NOVA-SERVER";
                processor.isTrained = ((bool)chainobject["info_trained"]);
                if (!processor.isTrained)
                { return null; }

                var leftContext = chainobject["meta_left_ctx"];
                if (leftContext != null)
                {
                    processor.LeftContext = chainobject["meta_left_ctx"].ToString();
                }

                var rightContext = chainobject["meta_right_ctx"];
                if (rightContext != null)
                {
                    processor.RightContext = chainobject["meta_right_ctx"].ToString();
                }

                var frameStep = chainobject["meta_frame_step"];
                if (frameStep != null)
                {
                    processor.FrameStep = chainobject["meta_frame_step"].ToString();
                }

                var category = chainobject["meta_category"];
                if (category != null)
                {
                    processor.Category = chainobject["meta_category"].ToString();
                }
                var description = chainobject["meta_description"];
                if (description != null)
                {
                    processor.Description = chainobject["meta_description"].ToString();
                }
                var backend = chainobject["meta_backend"];
                if(backend != null)
                {
                    processor.Backend = backend.ToString();
                }

                var modelWeightsPath = chainobject["model_weights_path"];
                if(modelWeightsPath != null)
                {
                    processor.ModelWeightsPath = modelWeightsPath.ToString();
                }

                processor.isIterable = true;
                var isiterable = chainobject["meta_is_iterable"];
                if (isiterable != null)
                {
                    processor.isIterable = ((bool)chainobject["meta_is_iterable"]);
                }

                Transformer t = new Transformer();
                t.Name = chainobject["model_create"].ToString();

                var Script = chainobject["model_script_path"];
                if (Script != null)
                {
                    t.Script = chainobject["model_script_path"].ToString();
                }
                var Syspath = chainobject["syspath"];
                var OptStr = chainobject["model_option_string"];
                if (OptStr != null)
                {
                    t.OptStr = chainobject["model_option_string"].ToString();
                }
                var Multi_role_input = chainobject["model_multi_role_input"];
                if (Multi_role_input != null)
                {
                    t.Multi_role_input = bool.Parse(chainobject["model_multi_role_input"].ToString());
                }
                else t.Multi_role_input = true;

                t.Type = "filter";
                processor.AddTransformer(t);

                processor.Inputs = new List<ServerInputOutput>();
                processor.Outputs = new List<ServerInputOutput>();

                var meta_io = chainobject["meta_io"];
                if (meta_io != null && meta_io.ToString() != "[]")
                {

                    JArray io = JArray.Parse(meta_io.ToString());
                    foreach (var element in io)
                    {
                        ServerInputOutput inputoutput = new ServerInputOutput();
                        inputoutput.ID = element["id"].ToString();
                        inputoutput.IO = element["type"].ToString();
                        var defaultname = element["default_value"];
                        if (defaultname != null)
                        {
                            inputoutput.DefaultName = defaultname.ToString();
                        }
                        string[] split = element["data"].ToString().Split(':');
                        inputoutput.Type = split[0];
                        if (split.Length > 1)
                            inputoutput.SubType = split[1];
                        if (split.Length > 2)
                            inputoutput.SubSubType = split[2];

                        if (inputoutput.IO == "input")
                        {
                            processor.Inputs.Add(inputoutput);
                        }
                        else processor.Outputs.Add(inputoutput);
                    }

                }

                return processor;
            }
            catch (Exception e)
            {
                MessageTools.Error(e.ToString());
                return null;
            }

        }

    }
}
