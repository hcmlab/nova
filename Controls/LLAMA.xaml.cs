using ExCSS;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;

namespace ssi.Controls
{

    /// <summary>
    /// Interaktionslogik für LLAMA.xaml
    /// </summary>
    public partial class LLAMA : Window
    {
        System.Windows.Documents.Paragraph paragraph = new System.Windows.Documents.Paragraph();
        private static HttpClient client = new HttpClient();
   

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

            getModels();
            return;


            InputBox.Focus();
        }
        public static Dictionary<string, dynamic> dict_users = new Dictionary<string, dynamic>();

        private async void getModels()
        {
            List<string> res = new List<string>();
            string url = "http://" + Properties.Settings.Default.NovaAssistantAddress + "/models";
            try
            {
                HttpClient client2 = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(2);
                var response = client2.GetAsync(url).Result;
                if (response == null)
                {
                    System.Windows.MessageBox.Show("Could not connect to Assistant");
                    this.Close();
                }

                var c = await response.Content.ReadAsByteArrayAsync();
                string result = System.Text.Encoding.UTF8.GetString(c);
                JArray models = JArray.Parse(result);
                string all = "";
                foreach (var model in models ) {
                    if (!model["provider"].ToString().StartsWith("text-completion"))
                        {
                        res.Add(model["provider"].ToString() + "/" + model["id"].ToString());
                    }
                }


  
                ModelBox.ItemsSource = res;
                ModelBox.SelectedItem = Properties.Settings.Default.NovaAssistantModel;
                if  (ModelBox.SelectedItem == null || ModelBox.SelectedItem.ToString() == "")
                {
                    ModelBox.SelectedIndex = 0;
                }

               
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);         
            }
               
        }


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
                if (list.Scheme.Type == AnnoScheme.TYPE.FREE || list.Scheme.Type == AnnoScheme.TYPE.DISCRETE)
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
                labels += "(" + item.Role + "," + "\""  +item.Item.Label.Trim() + "\");";
                //labels += item.ToString() + ";";
            }

            return labels;
        }

       
        public void update(string user, string text)
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
                bool enforcedtermism = Properties.Settings.Default.NovaAssistantEnforceDeterminism;
                float temperature = float.Parse(Properties.Settings.Default.NovaAssistantTemperature);
                int maxtokens = int.Parse(Properties.Settings.Default.NovaAssistantMaxtokens);
                int topk = int.Parse(Properties.Settings.Default.NovaAssistantTopK);
                float topP = float.Parse(Properties.Settings.Default.NovaAssistantTopP);
                bool contextaware = true;

   
                this.Dispatcher.Invoke(() =>
                {
                    contextaware = Contextaware.IsChecked == true;
                });

       

                if (contextaware && MainHandler.annoLists.Count > 0)
                {
                    datastr = await PrepareData();
                    datadescr = Properties.Settings.Default.NovaAssistantDataDescription;


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

                    this.Dispatcher.Invoke(() =>
                    {
                        dict_users[user]["history"] = new List<List<string>>();
                        answer = "";
                        paragraph.Inlines.Clear();
                    });

                }
                else
                {
                    string model = "";
                    string provider = "";

     
                        this.Dispatcher.Invoke(() =>
                        {
                            if (ModelBox.SelectedItem.ToString() != "")
                            {
                                var split = ModelBox.SelectedItem.ToString().Split('/');
                                provider = split[0];
                                for (int i = 1; i < split.Length; i++) {
                                    model = model + split[i] + "/";
                                }
                                model = model.TrimEnd('/');

                               // model = ModelBox.SelectedItem.ToString().Split('/')[1];
                                
                               
                               
                            }
                           
                        });


                var payload = new
                {
                    system_prompt = systemprompt, // + " Respond using JSON",
                    data = datastr,
                    model = model,
                    provider = provider,
                    message = message,
                    temperature = temperature,
                    max_new_tokens = maxtokens,
                    top_p = topP,
                    top_k = topk,
                    history = dict_users[user]["history"],
                    enforce_determinism = enforcedtermism,
                    //esp_format = "json",
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
            update(from, message);

           
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




    private void InputBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {

            if (e.Key == Key.Return)
            {
              
               // if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                {
                    //MessageBox.Show("Shift + Enter pressed");
                    e.Handled = true;
                    CallLlama();
                }
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

        private void Window_Deactivated(object sender, EventArgs e)
        {
            Window window = (Window)sender;
            window.Topmost = true;
        }

        private void ModelBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {

              Properties.Settings.Default.NovaAssistantModel = ModelBox.SelectedItem.ToString();
              Properties.Settings.Default.Save();
            });

        }
    }
}
