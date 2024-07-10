using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using OxyPlot.Reporting;
using Secp256k1Net;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Windows;
using System.Windows.Controls;

using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ThirdParty.BouncyCastle.Utilities.IO.Pem;
using WebSocketSharp;

using static ssi.MainHandler;
using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Button = System.Windows.Controls.Button;
using Image = System.Windows.Controls.Image;


namespace ssi
{
    /// <summary>
    /// Interaktionslogik für NostrDVM.xaml
    /// </summary>
    public partial class NostrDVM : System.Windows.Window
    {

        System.Windows.Documents.Paragraph paragraph = new System.Windows.Documents.Paragraph();
        string pk = "27da5b78f4b1d1c33817f76cf4c40b733e99cd192585ea1b711142682c3594b9"; 
        
        List<string> relays = new List<string>
            {
                "wss://relay.primal.net",
                "wss://nos.lol/",
                "wss://nostr.mom/"
            };

        public NostrDVM()
        {
            InitializeComponent();
            _ = listenNostr(pk);
            ReplyBox.Document = new FlowDocument(paragraph);
           
        }



        public NIP89 getDVMInfo(NostrSigner signer, string dvmkey )
        {

            NIP89 info = new NIP89();
            NostrClient client = new NostrClient(signer);

            foreach (string relay in relays)
            {
                client.addRelay(relay);
            }

            client.connect();

            client.OnResponse += (sender, note) =>
            {
                string content = note["content"];
                Console.WriteLine(content);
                info = DVM.parseNIP89(content);
                client.disconnect();
               
            };


            JArray kinds = new JArray
                    {
                        31990,
                    };

            JArray authors = new JArray
                    {
                        dvmkey
                    };


            JObject filter = new JObject
                    {
                        { "kinds", kinds },
                        { "authors", authors }
                    };


            client.get_events_of(filter);

            int timeoutms = 2000;
            int step = 200;
            while(timeoutms > 0)
            {
                if (info.name  == null){
                    timeoutms = timeoutms - step;
                    System.Threading.Thread.Sleep(step);
                }
                else
                {
                    return info;
                }
            }

            info.name = dvmkey;
            return info;
        }
     


        public async Task listenNostr(string private_key_hex)
        {
            byte[] privateKey = MainHandler.Keys.Parse(private_key_hex);
            string publickeyhex = MainHandler.Keys.getPublickeyHex(private_key_hex);

            NostrSigner signer = new NostrSigner(pk);
            NostrClient client = new NostrClient(signer);

            foreach (string relay in relays)
            {
                client.addRelay(relay);
            }

            client.connect();


            client.OnResponse += (sender, note) =>
            {

                try
                {
                   // dynamic note = client.extractNote(e.Data);
                    {

                        this.Dispatcher.Invoke(() =>
                        {


                            JToken AmountTag = NostrClient.getTag(note["tags"], "amount");
                            NIP89 info = getDVMInfo(signer, note["pubkey"].ToString());

                            paragraph.Inlines.Add(new Bold(new Run(info.name + ": "))
                            {
                                Foreground = System.Windows.Media.Brushes.Green
                            });

                            string content = note["content"];
                            if(content == "")
                            //Some DVMS might have the status message in the 3rd tag in status instead of content
                            {
                                JToken StatusTag = NostrClient.getTag(note["tags"], "status");
                                if(StatusTag.ToList().Count() > 2)
                                {
                                    content = StatusTag[2].ToString();
                                }
                            }

                          

                            if (content.Contains("https://") && note["kind"] != "7000")
                            {


                                BitmapImage bitmap = DownloadImage(content);

                                Image image = new Image
                                {
                                    Source = bitmap,
                                    Width = 300, // bitmap.Width, // Set desired width
                                    Height = 300// bitmap.Height // Set desired height
                                };

                                InlineUIContainer container = new InlineUIContainer(image);

                                System.Windows.Documents.Paragraph paragraph = new System.Windows.Documents.Paragraph(container);

                                ReplyBox.Document.Blocks.Add(paragraph);

                            }


                            paragraph.Inlines.Add(new Run(content)
                            {

                                Foreground = note["kind"] != "7000" ? System.Windows.Media.Brushes.Blue : System.Windows.Media.Brushes.Orange

                            }); ;


                            if (AmountTag != null)
                            {
                                //paragraph.Inlines.Add(AmountTag[2].ToString());

                                if (AmountTag.ToList().Count < 3)
                                {
                                    paragraph.Inlines.Add(new Run(info.lud16)
                                    {

                                        Foreground = System.Windows.Media.Brushes.Orange

                                    }); ;

                                }
                                
                                
                                
                                else if (MainHandler.myWallet != null)
                                {


                                    Button zapButton = new Button();
                                    zapButton.Background = Brushes.Gold;
                                    zapButton.BorderBrush = Brushes.Black;
                                    zapButton.Height = 20;
                                    zapButton.Width = 50;
                                    zapButton.Margin = new Thickness(10, 0, 0, 0);
                                    zapButton.Content = "\u26a1 Zap";
                                    zapButton.IsEnabled = true;

                                    zapButton.Click += async (sender2, EventArgs) =>
                                    {


                                        paragraph.Inlines.Add(new Bold(new Run("Paying Lightning invoice..."))
                                        {
                                            Foreground = System.Windows.Media.Brushes.Gold
                                        });


                                        Lightning lightning = new Lightning();
                                        await lightning.PayInvoice(MainHandler.myWallet, AmountTag[2].ToString());
                                        paragraph.Inlines.Add(new Bold(new Run("... Success"))
                                        {
                                            Foreground = System.Windows.Media.Brushes.Green
                                        });
                                        paragraph.Inlines.Add(new LineBreak());


                                    };

                                    paragraph.Inlines.Add(zapButton);

                                }
                                else
                                {
                                    paragraph.Inlines.Add(new Bold(new Run(AmountTag[2].ToString()))
                                    {
                                        Foreground = System.Windows.Media.Brushes.Gold
                                    });

                                }


                            }

                        

                            paragraph.Inlines.Add(new LineBreak());



                        });

                        Console.WriteLine("Received: " + note);
                    }
                }

                catch (Exception ex2)
                {
                    Console.WriteLine(ex2.Message);
                }


            };

                try
                {
                    JArray kinds = new JArray
                    {
                        7000, 6100
                    };

                    JArray ptags = new JArray
                    {
                        publickeyhex
                    };


                    JObject filter = new JObject
                    {
                        { "kinds", kinds },
                        { "since", NostrTime.Now() },
                        { "#p", ptags }
                    };


                    client.get_events(filter);

                   
                    client.disconnect();


                }
                catch (Exception ex) {
                    Console.WriteLine(ex.ToString());
                }

            }
        


        public void sendNostr(string private_key_hex, string content)
        {
            var secp256k1 = new Secp256k1();
            byte[] privateKey = MainHandler.Keys.Parse(private_key_hex);
            string publickeyhex = MainHandler.Keys.getPublickeyHex(private_key_hex);


            NostrSigner signer = new NostrSigner(pk);
            NostrClient client = new NostrClient(signer);

            foreach (string relay in relays)
            {
                client.addRelay(relay);
            }

            client.connect();


            client.OnResponse += (sender, note) =>
            {
                try
                {
                    Console.WriteLine("Nostr Received: " + note);
   
                }
                catch { Console.WriteLine("error"); }

            };

            int kind = 5100;
            List<Tag> tags = new List<Tag>();
            string[] tag  =  {"i", content, "text"};
            Tag iTag = new Tag(tag);
            tags.Add(iTag);


            NostrEvent unsigned_event = new NostrEvent("", kind, tags, publickeyhex, NostrTime.Now());
            NostrEvent signedEvent = client.signer.Sign(unsigned_event);


            client.send(signedEvent);

            Console.ReadKey(true);
            client.disconnect();

        }

  
        private void DVM_Send_Click(object sender, RoutedEventArgs e)
        {
            
            string text = text_box.Text;
            sendNostr(pk, text);
            
        }

        private void result_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Scrollviewer.ScrollToEnd();
        }

     
    }
}
