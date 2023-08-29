using Microsoft.Toolkit.HighPerformance;
using Newtonsoft.Json;
using Octokit;
using OxyPlot.Reporting;
using SharpCompress.Common;
using SharpDX;
using SharpDX.Multimedia;
using ssi.Tools;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Web.Routing;
using System.Web.Security;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static CSJ2K.j2k.codestream.HeaderInfo;
using static ExCSS.AttributeSelectorFactory;
using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.LinkLabel;

namespace ssi.Controls
{
    /// <summary>
    /// Interaktionslogik für LLAMA.xaml
    /// </summary>
    public partial class LLAMA : Window
    {
        System.Windows.Documents.Paragraph paragraph = new System.Windows.Documents.Paragraph();
        DatabaseUser user;
      

        public class DataItem
        {
            public AnnoListItem Item { get; set; }
            public string Role { get; set; }

            public override string ToString()
            {
                return ("(" + Role +"," + Item.Label+")");
            }
        }


        public LLAMA()
        {
            InitializeComponent();
            ReplyBox.Document = new FlowDocument(paragraph);
     
            InputBox.Focus();
        }
        public static Dictionary<string, dynamic> dict_users = new Dictionary<string, dynamic>();

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
           
            CallLlama();
           
        }


        public async Task<string> PrepareData()
        {
            string labels = "";

            List<DataItem> annoListItems = new List<DataItem>();
            foreach (AnnoList list in MainHandler.annoLists)
            {
                if (list.Scheme.Type == AnnoScheme.TYPE.FREE)
                {
                   foreach(AnnoListItem ali in list)
                    {
                        DataItem item = new DataItem();
                        item.Item = ali;
                        item.Role = list.Meta.Role;
                        annoListItems.Add(item);
                    }
                }
            }
            var annoListItemsSorted = annoListItems.OrderBy(x => x.Item.Start).ToList(); // ToList optional


            foreach (DataItem item in annoListItemsSorted)
            {
                labels += item.ToString() + ";";
            }

            return labels;
        }

       

        public async void CallLlama()
        {
            string message = InputBox.Text;
            string from = "Nova User";
            if (DatabaseHandler.IsConnected == true)
            {
                var user = DatabaseHandler.GetUserInfo(Properties.Settings.Default.MongoDBUser);
                from = user.Fullname;
            }

            var text = message;
            paragraph.Inlines.Add(new Bold(new Run(from + ": "))
            {
                Foreground = System.Windows.Media.Brushes.Green
            });
            paragraph.Inlines.Add(text);
            paragraph.Inlines.Add(new LineBreak());
            InputBox.Clear();

            string url =  "http://" + Properties.Settings.Default.NovaAssistantAddress + "/assist";
          
            string response;
            try
            {
                 response = await SendMessage(message, url);
            }
            catch {
                response = "Couldn't connect to Assistent Server."; 
            }

            //ReplyBox.Text += response + "\n\n\n";
           //ReplyBox.AppendText(response + "\n");
   

            if (response != "")
            {
                from = "Nova Assistant";
                text = response;
                paragraph.Inlines.Add(new Bold(new Run(from + ": "))
                {
                    Foreground = System.Windows.Media.Brushes.Red
                });
                paragraph.Inlines.Add(text);
                paragraph.Inlines.Add(new LineBreak());
            }

          

            this.DataContext = this;
        }
      
     

    public async Task<string> SendMessage(string msg, string URL)
        {
            HttpClient client = new HttpClient();


            string datastr = "";
            string datadescr = "";
            string systemprompt = "Your Name is Nova Assistant. Do not create ficional examples. Under no circumstances should you add data to it.";

            if (msg.Contains("data"))
            {
                datastr = await PrepareData();
                datadescr = "The data you are supposed to analyse is provided to you in list form, where each entry contains the identity of the speaker at position 0 and the transcript of a speaker at position 1";
                systemprompt = "Your name is Nova Assistant. You are a therapeutic assistant, helping me analyse the interaction between a patient and the therapist. If you don't know the answer, please do not share false information. Do not create ficional examples. Under no circumstances should you add data to it. Do not start any analysis unless I specifically ask for it.";
            }
            else if (msg.StartsWith("-text-to-image"))
            {
                NovaServerNostr server = new NovaServerNostr(msg);
                
                return "";
                    
            }



            string user = Properties.Settings.Default.MongoDBUser;

            if (!dict_users.ContainsKey(user))
            {
                dict_users[user] = new Dictionary<string, dynamic>
                {
                    ["history"] = new List<List<string>>()
                };
            }

            async Task<string> PostStreamAsync(string url, object data)
            {
     
                    string ans = "";

                    var jsonContent = Newtonsoft.Json.JsonConvert.SerializeObject(data);
                    var stringContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    using (var response = await client.PostAsync(url, stringContent))
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (var reader = new System.IO.StreamReader(stream))
                    {
         
                        while (!reader.EndOfStream)
                        {
                            var line = await reader.ReadLineAsync();
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                Console.WriteLine(line);
                                this.Dispatcher.Invoke(() =>
                                {
                                    //ReplyBox.Text += line;
                                    //ReplyBox.Selection.Start = ReplyBox.TextLength;
                                    //ReplyBox.SelectionLength = 0;

                                    //ReplyBox.SelectionColor = color;
                                    //ReplyBox.AppendText(line);
                                    //ReplyBox.SelectionColor = ReplyBox.ForeColor;
                                });
                             }
                        }
                    }
           

                    return ans;
                
            }

            async Task<string> PostStream(string url, object data)
            {
                string reply = "";
                var json = JsonConvert.SerializeObject(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using (HttpResponseMessage response = await client.PostAsync(url, content))
                using (HttpContent responseContent = response.Content)
                {
                    string responseBody = await responseContent.ReadAsStringAsync();
                    reply = responseBody.TrimStart();
                }
                return reply;
            }

            string answer;

            if (msg == "-clear")
            {
                dict_users[user]["history"] = new List<List<string>>();
                answer = "";
                paragraph.Inlines.Clear();

                //FlowDocument ObjFdoc = new FlowDocument();
                //System.Windows.Documents.Paragraph paragraph = new System.Windows.Documents.Paragraph();
                //paragraph.Inlines.Add(new Run(""));
                //ObjFdoc.Blocks.Add(ObjPara1);
                //ReplyBox.Document = ObjFdoc;

            }
            else
            {
              
                var payload = new
                {
                    system_prompt = systemprompt,
                    data_desc = datadescr,
                    data = datastr,
                    message = msg,
                    history = dict_users[user]["history"]
                };

                answer = await PostStream(URL, payload);
                dict_users[user]["history"].Add(new List<string> { msg, answer });
            }
            return answer;
            
        }

        private void InputBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Return)
            {
                e.Handled = true;
                CallLlama();
            }
            else if (e.Key == System.Windows.Input.Key.Escape)
            {
                e.Handled = true;
                this.DialogResult = false;
                this.Close();
            }

        }

        private void ReplyBox_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        {

        }

        private void ReplyBox_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Scrollviewer.ScrollToEnd();

        }
    }
}
