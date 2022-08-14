using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Media.Imaging;

namespace ssi
{

    public class Lightning
    {

        public class LightningWallet
        {
            public int balance { get; set; }

            public string admin_key { get; set; }
            public string invoice_key { get; set; }
            public string wallet_name { get; set; }
            public string user_name { get; set; }
            public string email { get; set; }
            public string password { get; set; }
            public string admin_id { get; set; }
            public string user_id { get; set; }
            public string wallet_id { get; set; }
            public string lnaddresspin { get; set; }
            public string lnaddressname { get; set; }


        }


        public class LightningInvoice
        {
            public uint amount { get; set; }
            public string memo { get; set; }
            public string payment_hash { get; set; }
            public string payment_request { get; set; }
            public string checking_id { get; set; }
        }


        public async Task<Dictionary<string, string>> CreateWallet(DatabaseUser user, string server)
        {
            try
            {
                var content = new MultipartFormDataContent
                {
                    { new StringContent(user + "@" + server), "user_name" },
                    { new StringContent(user + "@" + server), "wallet_name" },
                    { new StringContent(user.Email), "email" },
                    { new StringContent("-"), "password" },
                };

                string url = Defaults.Lightning.LNBitsEndpoint + "/createLightningWallet";
                var client = new HttpClient();
                var response = await client.PostAsync(url, content);

                var responseString = await response.Content.ReadAsStringAsync();
                var responseDic = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseString);

                if (responseDic["success"] == "failed")
                {
                    return null;
                }

               

                return responseDic;

            }
            catch (Exception e)
            {
                return null;
            }
        }
       
     


        public async Task<int> GetWalletBalance(string admin_key)
        {
            var content = new MultipartFormDataContent
            {
                { new StringContent(admin_key), "wallet_admin_key" },
            };
            try
            {
                string url = Defaults.Lightning.LNBitsEndpoint + "/getWalletBalance";
                var client = new HttpClient();
                var response = await client.PostAsync(url, content);

                var responseString = await response.Content.ReadAsStringAsync();
                var responseDic = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseString);

                if (responseDic["success"] == "failed")
                {
                    return -1;
                }

                return Int32.Parse(responseDic["balance"]);
            }
            catch(Exception e)
            {
                return -1;
            }
          

       
            

        }



        //User Transactions
        public async Task<LightningInvoice> CreateInvoice(string invoice_key, uint sats, string memo)
        {
            var content = new MultipartFormDataContent
            {
                { new StringContent(invoice_key), "invoice_key" },
                { new StringContent(sats.ToString()), "sats" },
                { new StringContent(memo), "memo" },
            };
            string url = Defaults.Lightning.LNBitsEndpoint + "/createLightningInvoice";
            var client = new HttpClient();
            var response = await client.PostAsync(url, content);

            var responseString = await response.Content.ReadAsStringAsync();
            var responseDic = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseString);

            if (responseDic["success"] == "failed")
            {
                return null;
            }

            LightningInvoice invoice = new LightningInvoice();
            invoice.payment_request = responseDic["payment_request"];
            invoice.checking_id = responseDic["checking_id"];
            invoice.payment_hash = responseDic["payment_hash"];
            invoice.memo = memo;
            invoice.amount = sats;

            

            return invoice;
        }

       
        public async Task<string> TestLNaddress(string name, string key, string pin=" ", string prevname=" ")
        {
            var content = new MultipartFormDataContent
            {
               
            };
           string url = "";
           if(pin == " ")
            {
                url = "https://novaannotation.com/" + "create?name=" + name + "&key=" + key;
            }
           else
            {
                url = "https://novaannotation.com/" + "create?name=" + name + "&key=" + key + "&currentname=" + prevname + "&pin=" + pin;
            }
            var client = new HttpClient();
            var response = await client.PostAsync(url, content);

            var responseString = await response.Content.ReadAsStringAsync();
            if(responseString.Contains("Success"))
                {
                string[] array =  responseString.Split('&');
                string[] array2 = array[1].Split('=');
                string retpin = array2[1];
                return retpin;
                }
            else return "Error: " + responseString;

            //var responseDic = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseString);
            //dynamic responseDic = JObject.Parse(responseString);


        }


        public async Task<string> PayInvoice (Lightning.LightningWallet wallet, string payment_request)

        {
            var content = new MultipartFormDataContent
            {
                { new StringContent(wallet.admin_key), "wallet_admin_key" },
                { new StringContent(payment_request), "payment_request" },
            };
            string url = Defaults.Lightning.LNBitsEndpoint + "/payLightningInvoice";
            var client = new HttpClient();
            var response = await client.PostAsync(url, content);

            var responseString = await response.Content.ReadAsStringAsync();
            //var responseDic = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseString);
            dynamic responseDic = JObject.Parse(responseString);

            if (responseDic["success"] != "success")
            {
                return responseDic["message"];
            }
            
            else
            {
                LightningInvoice invoice = new LightningInvoice();
                invoice.payment_hash = responseDic["payment_hash"];
                invoice.checking_id = responseDic["checking_id"];
                return await checkInvoiceIsPaid(wallet, invoice); 

            }
        }


        public int DecodeBolt11(string payment_string)
        {
            try
            {
                string remaininginvoice = payment_string.Remove(0, 4);
                int index = getIndexOfFirstLetter(remaininginvoice);
                char identifier = remaininginvoice.ElementAt<char>(index);
                string numberstring = remaininginvoice.Substring(0, index);
                double number = double.Parse(numberstring);
                if (identifier == 'm') number = number * 100000000 * 0.001;   //m(milli): multiply by 0.001
                else if (identifier == 'u') number = number * 100000000 * 0.000001;  //u(micro): multiply by 0.000001
                else if (identifier == 'n') number = number * 100000000 * 0.000000001; //n(nano): multiply by 0.000000001
                else if (identifier == 'p') number = number * 100000000 * 0.000000000001; //p(pico): multiply by 0.000000000001

                int sats = (int)number;
                return sats; 
            }
            catch (Exception ex)
            {
                return -1;
            }
        }

        public async Task<double> PriceinCurrency(int sats, string currency = "USD")
        {
            string url = "https://api.coinstats.app/public/v1/coins?skip=0&limit=5&currency=" + currency;
            var client = new HttpClient();
            var response = await client.GetAsync(url);

            var responseString = await response.Content.ReadAsStringAsync();
            dynamic data = JObject.Parse(responseString);
            //var responseDic = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseString);
            var bitcoinprice = data["coins"][0]["price"];
            var pricepersat = bitcoinprice / 100000000.0;
            return pricepersat * sats;
        }
        //helper for DecodeBolt11
        int getIndexOfFirstLetter(string input)
        {
            var index = 0;
            foreach (var c in input)
                if (char.IsLetter(c))
                    return index;
                else
                    index++;

            return input.Length;
        }

        public async Task<Bitmap> GetQRImage(string payment_request)
        {
  
            WebClient client = new WebClient();
            Stream stream = client.OpenRead("https://chart.googleapis.com/chart?cht=qr&chl=" + payment_request + "&chs=200x200&chld=M|0");
            Bitmap bitmap; bitmap = new Bitmap(stream);
            stream.Flush();
            stream.Close();
            client.Dispose();
            if (bitmap != null)
            {
                return bitmap;
            }
            else return null;
        }

        public async Task<Bitmap> GetQRImageLNURL(string lnurlwid)
        {

            WebClient client = new WebClient();
            Stream stream = client.OpenRead("https://lnbits.novaannotation.com/withdraw/img/" + lnurlwid);
            Bitmap bitmap; bitmap = new Bitmap(stream);
            stream.Flush();
            stream.Close();
            client.Dispose();
            if (bitmap != null)
            {
                return bitmap;
            }
            else return null;
        }


        public async Task<string> checkInvoiceIsPaid(Lightning.LightningWallet wallet, Lightning.LightningInvoice invoice)
        {

            var content = new MultipartFormDataContent
            {
                { new StringContent(wallet.admin_key), "wallet_admin_key" },
                { new StringContent(invoice.payment_hash), "payment_hash" },
            };
            string url = Defaults.Lightning.LNBitsEndpoint + "/checkInvoiceIsPaid";
            var client = new HttpClient();
            var response = await client.PostAsync(url, content);

            var responseString = await response.Content.ReadAsStringAsync();
            var responseDic = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseString);

            if (responseDic["success"] == "success")
            {
                return "Success";
                
            }

            else return "Transaction failed";
        }

        //public async Task<BitmapImage> getLNURLwQR(string lnurlwid)
        //{
        //    var content = new MultipartFormDataContent
        //    {
        //        { new StringContent(lnurlwid), "lnurl" },
        //    };

        //    string url = "https://lnbits.novaannotation.com/withdraw/img/" + lnurlwid;
        //    var client = new HttpClient();
        //    var response = await client.GetAsync(url);
        //    var responseString = await response.Content.ReadAsByteArrayAsync();
        //    //byte[] explanation = System.Convert.FromBase64String(responseString);

        //    //BitmapImage LnUrlWQR = new BitmapImage();
        //    //LnUrlWQR.BeginInit();
        //    //LnUrlWQR.StreamSource = new System.IO.MemoryStream(explanation);
        //    //LnUrlWQR.EndInit();
        //    //LnUrlWQR.Freeze();

        //    return responseString;

        //}

        public async Task<Dictionary<string,string>> getLNURLw(Lightning.LightningWallet wallet, int amount)
        {
            var content = new MultipartFormDataContent
            {
                { new StringContent(wallet.admin_key), "wallet_admin_key" },
                { new StringContent(amount.ToString()), "amount" },

            };

            string url = Defaults.Lightning.LNBitsEndpoint + "/getLNURLw";
            var client = new HttpClient();
            var response = await client.PostAsync(url, content);
            var responseString = await response.Content.ReadAsStringAsync();
            var responseDic = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseString);

            if (responseDic["success"] == "success")
            {
                return responseDic;

            }

            else return responseDic;
        }


        public async Task<string> resolveLNaddress(string username, string domain, uint sats)
        {

          
            var client = new HttpClient();
            string url = "https://" + domain + "/.well-known/lnurlp/"  + username;
            try
            {
                HttpResponseMessage response;
                try
                {
                    response = await client.GetAsync(url);
                }
                catch(Exception e)

                {
                    return e.Message;
                }
                
                var responseString = await response.Content.ReadAsStringAsync();
                var responseDic = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseString);
  
               if(responseDic["tag"] == "payRequest"){
                //string getInvoice = responseDic[""] + 
                
                string invoiceurl = responseDic["callback"] + "?amount=" + sats.ToString();
                var responseinvoice = await client.GetAsync(invoiceurl);
                var responseinvoiceString = await responseinvoice.Content.ReadAsStringAsync();
                dynamic data = JObject.Parse(responseinvoiceString);

                string payment_request = data["pr"];
                return payment_request;
                

                }
                else return "error";

            }
            catch (Exception e)
            {
                return e.Message;
            }


          
        
        }
    }

}
