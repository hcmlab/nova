using ExCSS;
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
using System.Threading;
using System.Threading.Tasks;
using System.Web.Routing;
using System.Web.Security;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using static CSJ2K.j2k.codestream.HeaderInfo;
using static ExCSS.AttributeSelectorFactory;
using static ssi.DatabaseCMLExtractFeaturesWindow;
using static ssi.DatabaseNovaServerWindow;
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

       
        public void updateUItest(string user, string text)
        {
            this.Dispatcher.Invoke(() =>
            {
                paragraph.Inlines.Add(new Bold(new Run(user + ": "))
                {
                    Foreground = System.Windows.Media.Brushes.Green
                });
                paragraph.Inlines.Add(text);
                paragraph.Inlines.Add(new LineBreak());
                InputBox.Clear();


            });
        }

        public async void callLLAMA2(object pr)
        {
            string message = pr.ToString();

            string from = "Nova Assistant";

            try
            {
                HttpClient client = new HttpClient();


                string datastr = "";
                string datadescr = "";
                string systemprompt = Properties.Settings.Default.NovaAssistantSystemPrompt;
                float temperature = float.Parse(Properties.Settings.Default.NovaAssistantTemperature);
                int maxtokens = int.Parse(Properties.Settings.Default.NovaAssistantMaxtokens);
                int topk = int.Parse(Properties.Settings.Default.NovaAssistantTopK);
                float topP = float.Parse(Properties.Settings.Default.NovaAssistantTopP);
                bool contextaware = true;

                _ = Dispatcher.BeginInvoke(new Action(() =>
                {

                    this.Dispatcher.Invoke(() =>
                    {
                        contextaware = Contextaware.IsChecked == true;
                    });

                }), DispatcherPriority.SystemIdle);

                if (contextaware && MainHandler.annoLists.Count > 1)
                {
                    datastr = await PrepareData();
                    datadescr = "The data you are supposed to analyse is provided to you in list form, where each entry contains the identity of the speaker at position 0 and the transcript of a speaker at position 1";


                    if (!dict_users.ContainsKey(from))
                    {
                        dict_users[from] = new Dictionary<string, dynamic>
                        {
                            ["history"] = new List<List<string>>()
                        };
                    }
                }

                string user = Properties.Settings.Default.MongoDBUser;

                if (!dict_users.ContainsKey(user))
                {
                    dict_users[user] = new Dictionary<string, dynamic>
                    {
                        ["history"] = new List<List<string>>()
                    };
                }

                string answer =  "";

                if (message == "/clear")
                {

                    _ = Dispatcher.BeginInvoke(new Action(() =>
                    {

                        this.Dispatcher.Invoke(() =>
                        {
                            dict_users[user]["history"] = new List<List<string>>();
                            answer = "";
                            paragraph.Inlines.Clear();
                        });

                    }), DispatcherPriority.SystemIdle);

                 

   
                }
                else
                {

                    var payload = new
                    {
                        system_prompt = systemprompt,
                        data_desc = datadescr,
                        data = datastr,
                        message = message,
                        temperature = temperature,
                        max_new_tokens = maxtokens,
                        top_p = topP,
                        top_k = topk,
                        history = dict_users[user]["history"]
                    };

                    //answer = await PostStream(url, payload);
                    string reply = "";
                    string url = "http://" + Properties.Settings.Default.NovaAssistantAddress + "/assist";
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    request.Method = "POST";
                    request.ContentType = "application/json";

                    using (StreamWriter streamWriter = new StreamWriter(request.GetRequestStream()))
                    {
                        string json = JsonConvert.SerializeObject(payload);
                        streamWriter.Write(json);
                    }

                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    using (Stream stream = response.GetResponseStream())
                    using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        int index = 0;
                        answer = "";
                        while (!reader.EndOfStream)
                        {
                            char line = ' ';
                            int nextchar = reader.Read();

                            if (nextchar != -1)
                            {
                                line = (char)nextchar;
                                _ = Dispatcher.BeginInvoke(new Action(() =>
                                {

                                    this.Dispatcher.Invoke(() =>
                                    {
                                       


                                        if (index == 0)
                                        {
                                            InputBox.Clear();
                                            paragraph.Inlines.Add(new Bold(new Run(from + ": "))
                                            {
                                                Foreground = System.Windows.Media.Brushes.Red
                                            });
                                            answer += line;
                                            paragraph.Inlines.Add(line.ToString());
                                            //paragraph.Inlines.Add(new LineBreak());

                                        }
                                        else
                                        {
                                            answer += line;
                                            if (paragraph.Inlines.Count > 1)
                                            {
                                                paragraph.Inlines.Remove(paragraph.Inlines.ElementAt(paragraph.Inlines.Count - 1));
                                                paragraph.Inlines.Add(answer);
                                            }
                                           

                                        }

                                        index++;
                                    });



                                }), DispatcherPriority.SystemIdle);
                            }

                            Thread.Sleep(20);
                        }

                        _ = Dispatcher.BeginInvoke(new Action(() =>
                        {

                            this.Dispatcher.Invoke(() =>
                            {

                                paragraph.Inlines.Add(new LineBreak());
                                dict_users[user]["history"].Add(new List<string> { message, answer });
                            });

                         }), DispatcherPriority.SystemIdle);
                }




            }
            }
            catch (Exception e) {
                System.Windows.MessageBox.Show(e.Message);
            }

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
            updateUItest(from, message);

           
            string response;
            new Thread(callLLAMA2).Start(message);





                    this.DataContext = this;
        }

        async Task<string> PostStream(string url, object data)
        {
            string reply = "";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/json";

            using (StreamWriter streamWriter = new StreamWriter(request.GetRequestStream()))
            {
                string json = JsonConvert.SerializeObject(data);
                streamWriter.Write(json);
            }

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (!string.IsNullOrEmpty(line))
                    {
                        reply += line;
                        //Console.Write(line + " ");
                        //System.Windows.MessageBox.Show(line + " ");
                        this.Dispatcher.Invoke(() =>
                        {
                            if (line != "")
                            {
                                string from = "Nova Assistant";
                                string text = reply;
                                this.Dispatcher.Invoke(() =>
                                {

                                    paragraph.Inlines.Add(new Bold(new Run(from + ": "))
                                    {
                                        Foreground = System.Windows.Media.Brushes.Red
                                    });
                                    paragraph.Inlines.Add(text);
                                    paragraph.Inlines.Add(new LineBreak());

                                });

                             
                            }
                            Console.WriteLine(line + "\n");

                        });

                    }
                }
            }

            return reply;
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


            async Task<string> PostStream(string url, object data)
            {
                string reply = "";
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.ContentType = "application/json";

                using (StreamWriter streamWriter = new StreamWriter(request.GetRequestStream()))
                {
                    string json = JsonConvert.SerializeObject(data);
                    streamWriter.Write(json);
                }

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        if (!string.IsNullOrEmpty(line))
                        {
                            reply += line;
                            //Console.Write(line + " ");
                            //System.Windows.MessageBox.Show(line + " ");
                            this.Dispatcher.Invoke(() =>
                            {
                                if (line != "")
                                {
                                    string from = "Nova Assistant";
                                    string text = reply;
                                    paragraph.Inlines.Add(new Bold(new Run(from + ": "))
                                    {
                                        Foreground = System.Windows.Media.Brushes.Red
                                    });
                                    paragraph.Inlines.Add(text);
                                    paragraph.Inlines.Add(new LineBreak());
                                }

                                //paragraph.Inlines.Add(reply);
                                //paragraph.Inlines.Add(new LineBreak());
                                //System.Windows.MessageBox.Show(reply);
                                Console.WriteLine(line +"\n");

                            });
                            
                        }
                    }
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
        else if (e.Key == System.Windows.Input.Key.Escape || e.Key == Key.A && Keyboard.IsKeyDown(Key.LeftCtrl))
        {
            e.Handled = true;
            //this.DialogResult = false;
            this.Hide();
        }

    }

    private void ReplyBox_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
    {

    }

    private void ReplyBox_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        Scrollviewer.ScrollToEnd();

    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        //if (!AppGeneral.IsClosing)
        {
            this.Hide();
            e.Cancel = true;
        }
    }
    
    }
}
