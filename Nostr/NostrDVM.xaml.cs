using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using OxyPlot.Reporting;
using Secp256k1Net;
using System;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WebSocketSharp;

using static ssi.MainHandler;
using static System.Net.Mime.MediaTypeNames;
using Button = System.Windows.Controls.Button;
using Image = System.Windows.Controls.Image;


namespace ssi
{
    /// <summary>
    /// Interaktionslogik für NostrDVM.xaml
    /// </summary>
    public partial class NostrDVM : Window
    {
        MainHandler handler;
        System.Windows.Documents.Paragraph paragraph = new System.Windows.Documents.Paragraph();
        string pk = "27da5b78f4b1d1c33817f76cf4c40b733e99cd192585ea1b711142682c3594b9";

        string RELAY = "wss://relay.primal.net";

        public NostrDVM(MainHandler mh)
        {
            InitializeComponent();
            handler = mh;
            _ = listenNostr(pk);
            ReplyBox.Document = new FlowDocument(paragraph);
            //KeyPairHex keys = handler.createKeys();

        }


        public JToken getTag(JArray tags, string parseBy)
        {
           
            foreach (var tag in tags
                .Where(obj => obj[0].Value<string>() == parseBy))
            {
                return tag;
            }
            return null;
        }

        private BitmapImage DownloadImage(string url)
        {
            BitmapImage bitmap = new BitmapImage();
            using (WebClient client = new WebClient())
            {
                byte[] imageBytes = client.DownloadData(url);
                using (MemoryStream ms = new MemoryStream(imageBytes))
                {
                    bitmap.BeginInit();
                    bitmap.StreamSource = ms;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                }
            }
            return bitmap;
        }



        public async Task listenNostr(string private_key_hex)
        {
            byte[] privateKey = SchnorrSigner.HexToByteArray(private_key_hex);
            string publickeyhex = SchnorrSigner.getPublickeyHex(private_key_hex);


            var ws = new WebSocket(RELAY);
            ws.OnMessage += (sender, e) =>
            {
                this.Dispatcher.Invoke(() =>
                {
                    dynamic json = JsonConvert.DeserializeObject<dynamic>(e.Data);
                    try
                    {
                        dynamic note = JsonConvert.DeserializeObject<dynamic>(json[2].ToString());

                        JToken AmountTag = getTag(note["tags"], "amount");
                        paragraph.Inlines.Add(new Bold(new Run(note["pubkey"].ToString() + ": "))
                        {
                            Foreground = System.Windows.Media.Brushes.Green
                        });

                         string content = note["content"];

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
                            
                           Foreground =  note["kind"] != "7000" ? System.Windows.Media.Brushes.Blue :  System.Windows.Media.Brushes.Orange

                        }); ;
                       

                        if (AmountTag != null)
                        {
                            //paragraph.Inlines.Add(AmountTag[2].ToString());
                            Button zapButton = new Button();
                            zapButton.Background = Brushes.Gold;
                            zapButton.BorderBrush = Brushes.Black;
                            zapButton.Height = 20;
                            zapButton.Width = 50;
                            zapButton.Margin = new Thickness(10, 0, 0, 0);
                            zapButton.Content = "\u26a1 Zap";
                            zapButton.IsEnabled = true;

                            zapButton.Click += async (sender2, EventArgs) => {


                                paragraph.Inlines.Add(new Bold(new Run("Paying Lightning invoice..."))
                                {
                                    Foreground = System.Windows.Media.Brushes.Gold
                                });
                                Lightning lightning = new Lightning();
                                await lightning.PayInvoice(MainHandler.myWallet, AmountTag[2].ToString());
                                 paragraph.Inlines.Add(new LineBreak());
                            
                            
                            };
                            


                            paragraph.Inlines.Add(zapButton);   


                        }


                        paragraph.Inlines.Add(new LineBreak());

                    }
                    catch { Console.WriteLine("No content"); }
                 
                }); 
               
                Console.WriteLine("Received: " + e.Data);
            };

            ws.Connect();

            string subscriptionid = "asdjnasdlkjashdajskdhasjdasd";
            JArray array = new JArray();
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
                { "kinds", kinds} ,
                { "since", ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds() },
                { "#p", ptags }
            };
        // { "#p","27da5b78f4b1d1c33817f76cf4c40b733e99cd192585ea1b711142682c3594b9"}

    

            array.Add("REQ");
            array.Add(subscriptionid);
            array.Add(filter);

            Console.WriteLine(array.ToString());
            ws.Send(array.ToString());

            Console.ReadKey(true);
            ws.Close();



        }

        public async Task sendNostr(string private_key_hex, string content)
        {

        
            var secp256k1 = new Secp256k1();
            byte[] privateKey = SchnorrSigner.HexToByteArray(private_key_hex);
            string publickeyhex = SchnorrSigner.getPublickeyHex(private_key_hex);


            var ws = new WebSocket(RELAY);
            ws.OnMessage += (sender, ex) =>
            {
                try
                {
                    Console.WriteLine("Nostr Received: " + ex.Data);
   
                }
                catch { Console.WriteLine("error"); }




            };



            ws.Connect();

            long created_at = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
            //created_at = 1720464386;
            int kind = 5100;
            JArray tags = new JArray
            {




            };


            JArray iTag = new JArray
                {
                    "i", content ,"text"
                };


            tags.Add(iTag);




            JArray evt = new JArray
            {
                  0,
                  publickeyhex,
                  created_at,
                  kind,
                  tags,
                  content
            };




            string stringifiedevent = JsonConvert.SerializeObject(evt);

            Console.WriteLine(stringifiedevent);
            var msgBytes = Encoding.UTF8.GetBytes(stringifiedevent);
            var msgHash = System.Security.Cryptography.SHA256.Create().ComputeHash(msgBytes);
            string id = BitConverter.ToString(msgHash).ToLower().Replace("-", "");



            // Sign the message
            string signature = SchnorrSigner.SignMessage(msgHash, private_key_hex);

            // bool isValid = SchnorrSigner2.VerifyMessage(id, signature, publickeyhex);

            // Output the signature
            Console.WriteLine("Schnorr Signature C Library: " + signature);

            JObject signed_event = new JObject
            {
                { "id", id  },
                { "pubkey", publickeyhex },
                { "created_at", created_at },
                { "kind", kind },
                { "tags", tags },
                { "content", content },
                { "sig", signature }
            };



            JArray array = new JArray
            {
                "EVENT",
                signed_event
            };

            Console.WriteLine(array.ToString());
            ws.Send(array.ToString());

            Console.ReadKey(true);
            ws.Close();







        }

        private void ButtonOnClick(object sender, RoutedEventArgs e)
        {
        
        }

        private void DVM_Send_Click(object sender, RoutedEventArgs e)
        {
            
            string text = text_box.Text;
            _ = sendNostr(pk, text);
            
        }

        private void result_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Scrollviewer.ScrollToEnd();
        }

     
    }
}
