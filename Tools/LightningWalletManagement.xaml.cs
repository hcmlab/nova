using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Brushes = System.Windows.Media.Brushes;

namespace ssi
{
    /// <summary>
    /// Interaktionslogik für Window1.xaml
    /// </summary>
    public partial class LightningWalletMangement : Window
    {
        Lightning lightning = new Lightning();
        MainHandler handler;
        private System.Timers.Timer checkInvoicePaidTimer;
        Lightning.LightningInvoice currentinvoice = null;
        public LightningWalletMangement(MainHandler handler)
        {
            InitializeComponent();
            if(checkInvoicePaidTimer != null) checkInvoicePaidTimer.Stop();
         
            this.handler = handler;
          
            //createWallet();
            if (MainHandler.myWallet == null)
            {
                Lightning.Visibility = Visibility.Collapsed;
                LightningCreate.Visibility = Visibility.Visible;
                walletaddress.Text = " ";
                statusBar.Visibility = Visibility.Hidden;
                export.Visibility = Visibility.Hidden;
            }

            else
            {
                walletaddress.Text = MainHandler.myWallet.lnaddressname;
               // walletid.Content = "ID: " + MainHandler.myWallet.wallet_id;
                balance.Content = "Balance: " + (MainHandler.myWallet.balance / 1000) + " Sats";
                loadbalancewithCurrency();
                Lightning.Visibility = Visibility.Visible;
                LightningCreate.Visibility = Visibility.Collapsed;
                statusBar.Visibility = Visibility.Visible;
                export.Visibility = Visibility.Visible;
            }
        }

        private async void loadbalancewithCurrency()
        {
            balance.Content = "Balance: " + (MainHandler.myWallet.balance / 1000) + " Sats (" + (await lightning.PriceinCurrency(MainHandler.myWallet.balance / 1000, "EUR")).ToString("0.00") + "€)";
        }

        private async void GenerateInvoice_Click(object sender, RoutedEventArgs e)
        {
           clipboard.Content = "Creating invoice, please wait..";
            try
            {
               int sats = -1;


               if (DepositSats.Text.ToLower().StartsWith("ln") || DepositSats.Text.ToLower().StartsWith("lightning:ln"))
                {
                    clipboard.Content = "Reading LNURL..";

                    if (lightning.lnurlcallbackpaylink != "" && lightning.lnurlcallbackpaylink != "error")
                    {
                        string message = await lightning.payLNURLcb(lightning.lnurlcallbackpaylink);
                        if (message == "Success")
                        {
                            clipboard.Foreground = Brushes.Green;
                        }
                        else clipboard.Foreground = Brushes.Red;
                        clipboard.Content = message;
                        UpdateBalance();
                    }
                    else {
                        clipboard.Foreground = Brushes.Red;
                        clipboard.Content = "Error";
                    }



                }
               else if(Int32.TryParse(DepositSats.Text, out sats))
                {

                    clipboard.Content = "Creating invoice, please wait..";

                    Lightning.LightningInvoice invoice = await lightning.CreateInvoice(MainHandler.myWallet.invoice_key, (uint)sats, "Deposit to NOVA Wallet");
                //InitCheckInvoicePaidTimer(invoice);
                currentinvoice = invoice;
                InitCheckLightningBalanceTimer();

                DepositAdress.Text = invoice.payment_request;
                System.Windows.Clipboard.SetText(invoice.payment_request);
                clipboard.Content = "Copied to clipboard";

                Bitmap bmp = await lightning.GetQRImage(invoice.payment_request);
                QR2.Visibility = Visibility.Collapsed;
                Withdrawaddress.Text = "";
                QR.Visibility = Visibility.Visible;
                QR.Source = ConvertBitmap(bmp);

                }


            }
            catch(Exception ex)
            {
                clipboard.Content = ex.ToString();
            }
    
        }
        public void InitCheckLightningBalanceTimer()
        {
            if (checkInvoicePaidTimer != null){
                checkInvoicePaidTimer.Stop();
                checkInvoicePaidTimer.Close();
            }
            checkInvoicePaidTimer = new System.Timers.Timer();
            checkInvoicePaidTimer.Elapsed += new ElapsedEventHandler(timer_Tick);
            checkInvoicePaidTimer.Interval = 2000; // in miliseconds
            checkInvoicePaidTimer.Start();
        }

        private async void timer_Tick(object sender, EventArgs e)
        {

            //checkInvoicePaidTimer.Stop();
            if (MainHandler.ENABLE_LIGHTNING && MainHandler.myWallet != null)
            {
               
             

                bool ispaid = await lightning.checkInvoiceIsPaid(MainHandler.myWallet, currentinvoice) == "Success";
                if (ispaid)
                {
                    checkInvoicePaidTimer.Stop();
                    checkInvoicePaidTimer.Close();
                    this.QR.Dispatcher.Invoke(() => {
                        QR.Source = new BitmapImage(new Uri("/Resources/suc.png", UriKind.Relative));
                    });


                    UpdateBalance();
                }
            }

        }


        public async void UpdateBalance()
        {
            try
            {


                MainHandler.myWallet.balance = await lightning.GetWalletBalance(MainHandler.myWallet.admin_key);
                string sats = "Loading";
                try
                {
                    sats = "Balance: " + (MainHandler.myWallet.balance / 1000) + " Sats (" + (await lightning.PriceinCurrency(MainHandler.myWallet.balance / 1000, "EUR")).ToString("0.00") + "€)";
                }
                catch (Exception exp)
                {
                    sats = "Balance: " + (MainHandler.myWallet.balance / 1000) + " Sats";
                }

                handler.control.navigator.satsbalance.Dispatcher.Invoke(() => {
                    handler.control.navigator.satsbalance.Content = "\u26a1 " + (MainHandler.myWallet.balance / 1000) + " Sats";
                });
                this.balance.Dispatcher.Invoke(() => {
                    balance.Content = sats;
                });

            }
            catch (Exception ex)
            {
                handler.control.navigator.satsbalance.Dispatcher.Invoke(() => {
                    handler.control.navigator.satsbalance.Content = "\u26a1 " + "Error connecting to Lightning wallet.. please try again later.";
                });
                this.balance.Dispatcher.Invoke(() => {
                    balance.Content = ex;
                });
            }
        }


      
        public BitmapImage ConvertBitmap(Bitmap src)
        {
            MemoryStream ms = new MemoryStream();
            ((System.Drawing.Bitmap)src).Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            ms.Seek(0, SeekOrigin.Begin);
            image.StreamSource = ms;
            image.EndInit();
            return image;
        }

        private async void Withdraw_Click(object sender, RoutedEventArgs e)
        {
            statuswithdraw.Content = "";
            Lightning.LightningInvoice invoice = new Lightning.LightningInvoice();
            string payment_request = "";

            if (WithdrawUrl.Text.Contains("@") || WithdrawUrl.Text.ToLower().StartsWith("ln")) { 

                if (WithdrawUrl.Text.Contains("@")) //Check for Lightning address
            {
                string[] split = WithdrawUrl.Text.Split('@');
                payment_request = await lightning.resolveLNaddress(split[0], split[1], Convert.ToUInt32(WithdrawAmount.Text) * 1000);
                if(payment_request != "error")
                {
                    invoice.payment_request = payment_request;
                      

                    }
                else
                {
                    statuswithdraw.Content = "Error resolving LN Address";
                    statuswithdraw.Foreground = System.Windows.Media.Brushes.Red;
                }
            }

            else if(WithdrawUrl.Text.ToLower().StartsWith("ln"))

            {
                invoice.payment_request = WithdrawUrl.Text;
            }

          

            if (payment_request != "error")
            {
                statuswithdraw.Content = "Transacting, please wait..";
                statuswithdraw.Foreground = System.Windows.Media.Brushes.Black;
                    try
                    {
                        string message = await lightning.PayInvoice(MainHandler.myWallet, invoice.payment_request);
                        statuswithdraw.Content = message;
                        if (message == "Success")
                        {
                            statuswithdraw.Foreground = System.Windows.Media.Brushes.Green;
                            MainHandler.myWallet.balance = await lightning.GetWalletBalance(MainHandler.myWallet.admin_key);
                            balance.Content = "Balance: " + (MainHandler.myWallet.balance / 1000) + " Sats";
                        }
                        else
                        {
                            statuswithdraw.Foreground = System.Windows.Media.Brushes.Red;

                        }
                    }
                    catch(Exception ex)
                    {
                        statuswithdraw.Foreground = System.Windows.Media.Brushes.Red;
                        statuswithdraw.Content = ex;
                    }
              
            }

            }

            else if (Int32.TryParse(WithdrawUrl.Text, out int value))
            {
                if(value >= 10)
                {
                    statuswithdraw.Content = "Generating Withdraw Url";
                    statuswithdraw.Foreground = System.Windows.Media.Brushes.Black;
                    var lnurl = await lightning.getLNURLw(MainHandler.myWallet, value);
                    //string lnurlwstring = "lightning:" + lnurl["lnurl"];

                    //Bitmap bmp = await lightning.GetQRImage(lnurlwstring);
                    Bitmap bmp = await lightning.GetLNURLQRImage(lnurl["id"]);
                    //bmp.SetResolution(200, 200);
                    QR2.Visibility = Visibility.Visible;
                    QR2.Source = ConvertBitmap(bmp);
                    DepositAdress.Text = "";
                    QR.Visibility = Visibility.Collapsed;
                    System.Windows.Clipboard.SetText(lnurl["lnurl"]);
                    Withdrawaddress.Visibility = Visibility.Visible;
                    Withdrawaddress.Text = lnurl["lnurl"];
                    statuswithdraw.Content = "Copied LNURL to clipboard";
                }
                else 
                {
                   statuswithdraw.Content = "Amount in Withdraw LNURL must be > 10 sats";
                   statuswithdraw.Foreground = System.Windows.Media.Brushes.Red;
                }

            }



        }

        private void WithdrawUrl_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (WithdrawUrl.Text.StartsWith("lnbc"))
            {
                int sats = lightning.DecodeBolt11(WithdrawUrl.Text);
                if(sats == -1)
                {
                    satsWithdrawLabel.Content = "Error in Payment String";
                }
                else
                {
                    satsWithdrawLabel.Content = sats + " Sats";                    
                }
 
            }
            else if (withdrawsatspanel!= null)
            {
                if (WithdrawUrl.Text.Contains("@"))
                {
                    withdrawsatspanel.Visibility = Visibility.Visible;
                    satsWithdrawLabel.Content = "Sats";

                }
                else
                {
                    withdrawsatspanel.Visibility = Visibility.Collapsed;
                    satsWithdrawLabel.Content = "";
                }
                   
            }

           

        }

        private void WithdrawUrl_GotFocus(object sender, RoutedEventArgs e)
        {
        TextBox box = sender as TextBox;
        box.Text = String.Empty;
        }

        private async void createWallet()
        {
            string username = Properties.Settings.Default.MongoDBUser;
            Dictionary<string, string> response = await lightning.CreateWallet(DatabaseHandler.GetUserInfo(username), Properties.Settings.Default.DatabaseAddress);
            Dictionary<string, string> response2 = await lightning.CreateLockedWallet(DatabaseHandler.GetUserInfo(username), Properties.Settings.Default.DatabaseAddress);

            DatabaseUser user = DatabaseHandler.GetUserInfo(username);
            if (response == null) return;
            string admin_key = response["adminkey"];
            string invoice_key = response["inkey"];
            string wallet_id = response["wallet_id"];

            string admin_key_locked = response2["adminkey"];
            string invoice_key_locked = response2["inkey"];
            string wallet_id_locked = response2["wallet_id"];

            string user_id = response["user_id"];
            string pass = MainHandler.Decode(Properties.Settings.Default.MongoDBPass);
            user.ln_admin_key = MainHandler.Cipher.AES.EncryptText(admin_key, pass);  //encrypt
            user.ln_invoice_key = invoice_key;
            user.ln_wallet_id = wallet_id;
            user.ln_admin_key_locked = MainHandler.Cipher.AES.EncryptText(admin_key_locked, pass);  //encrypt
            user.ln_invoice_key_locked = invoice_key_locked;
            user.ln_wallet_id_locked = wallet_id_locked;

            user.ln_user_id = user_id;

            string rnd = RandomString(8);
            string lnaddressname = "user-" + rnd;
            string pin = await lightning.TestLNaddress(lnaddressname, invoice_key);

            user.ln_addressname = lnaddressname;
            user.ln_addresspin = pin;

            DatabaseHandler.ChangeUserCustomData(user);
            MainHandler.myWallet = new Lightning.LightningWallet();
            MainHandler.myWallet.admin_key = admin_key;
            MainHandler.myWallet.invoice_key = invoice_key;
            MainHandler.myWallet.wallet_id = wallet_id;
            MainHandler.myWallet.admin_key_locked = admin_key_locked;
            MainHandler.myWallet.invoice_key_locked = invoice_key_locked;
            MainHandler.myWallet.wallet_id_locked = wallet_id_locked;
            MainHandler.myWallet.user_id = user_id;
            MainHandler.myWallet.lnaddressname = lnaddressname;
            MainHandler.myWallet.lnaddresspin = pin;
            MainHandler.myWallet.balance = await lightning.GetWalletBalance(MainHandler.myWallet.admin_key);
            Lightning.Visibility = Visibility.Visible;
            LightningCreate.Visibility = Visibility.Collapsed;
            walletaddress.Text = MainHandler.myWallet.lnaddressname;
            statusBar.Visibility = Visibility.Visible;
        }

        public string RandomString(int size, bool lowerCase = false)
        {
            var builder = new StringBuilder(size);

            // Unicode/ASCII Letters are divided into two blocks
            // (Letters 65–90 / 97–122):
            // The first group containing the uppercase letters and
            // the second group containing the lowercase.  

            // char is a single Unicode character  
            char offset = lowerCase ? 'a' : 'A';
            const int lettersOffset = 26; // A...Z or a..z: length=26  
            Random _random = new Random();
            for (var i = 0; i < size; i++)
            {
                var @char = (char)_random.Next(offset, offset + lettersOffset);
                builder.Append(@char);
            }

            return lowerCase ? builder.ToString().ToLower() : builder.ToString();
        }

        private void createButton_Click(object sender, RoutedEventArgs e)
        {
           createButton.IsEnabled = false;
           stat.Text = "Please wait, wallet is being created..";
           createWallet();
        }

        private async void help_Click(object sender, RoutedEventArgs e)
        {
            LightningCreate.Visibility = Visibility.Visible;
            createButton.IsEnabled = true;
            Lightning.Visibility = Visibility.Collapsed;
            createButton.Content = "Back to Wallet";
            createButton.Click -= createButton_Click;
            createButton.Click += help_Click2;
            ExportToWallet.Visibility = Visibility.Visible;
            Bitmap bmp = await lightning.GetQRImage("lndhub://admin:" + MainHandler.myWallet.admin_key + "@"+ Defaults.Lightning.LNBitsEndPoint + "/lndhub/ext/");
            ExportToWallet.Source = ConvertBitmap(bmp);

        }
        private void help_Click2(object sender, RoutedEventArgs e)
        {
            LightningCreate.Visibility = Visibility.Collapsed;
            Lightning.Visibility = Visibility.Visible;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if(checkInvoicePaidTimer != null)
            {
                checkInvoicePaidTimer.Stop();
                checkInvoicePaidTimer.Close();
            }
           
        }

        private async void walletid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            statuswithdraw.Foreground = Brushes.Black;
            statuswithdraw.Content = "Copied LNaddress to clipboard";
            System.Windows.Clipboard.SetText(MainHandler.myWallet.lnaddressname + Defaults.Lightning.LnAddressHost);  
        }

        private void balance_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            UpdateBalance();
        }
    

        private async void walletid_KeyUp_1(object sender, KeyEventArgs e)
        {
            if (walletaddress.Text != MainHandler.myWallet.lnaddressname)
            {
                applylnbutton.Visibility = Visibility.Visible;
            }
            else applylnbutton.Visibility = Visibility.Hidden;
            
            e.Handled = true;
        }

        private async void applylnbutton_Click(object sender, RoutedEventArgs e)
        {
                DatabaseUser user = DatabaseHandler.GetUserInfo(Properties.Settings.Default.MongoDBUser);

                string lnaddressname = walletaddress.Text;
                string pin = await lightning.TestLNaddress(lnaddressname, MainHandler.myWallet.invoice_key, MainHandler.myWallet.lnaddresspin, MainHandler.myWallet.lnaddressname);
                if (pin.Contains("Error"))
                {
                    MessageBox.Show(pin);
                    return;
                }
    

                user.Password = MainHandler.Decode(Properties.Settings.Default.MongoDBPass);
                user.ln_invoice_key = MainHandler.myWallet.invoice_key;
                user.ln_wallet_id = MainHandler.myWallet.wallet_id;
                user.ln_user_id = MainHandler.myWallet.user_id;
                user.ln_admin_key = MainHandler.Cipher.AES.EncryptText(MainHandler.myWallet.admin_key, user.Password);
                user.ln_admin_key_locked = MainHandler.Cipher.AES.EncryptText(MainHandler.myWallet.admin_key_locked, user.Password);
            user.ln_invoice_key_locked = MainHandler.myWallet.invoice_key_locked;
            user.ln_wallet_id_locked = MainHandler.myWallet.wallet_id_locked;
            user.ln_addressname = lnaddressname;
                user.ln_addresspin = pin;
                DatabaseHandler.ChangeUserCustomData(user);
                MainHandler.myWallet.lnaddresspin = user.ln_addresspin;
                MainHandler.myWallet.lnaddressname = user.ln_addressname;

                walletaddress.Text = MainHandler.myWallet.lnaddressname;
              
                applylnbutton.Visibility = Visibility.Hidden;
         
        }

        private void walletid_GotFocus(object sender, RoutedEventArgs e)
        {
            walletaddress.BorderThickness = new Thickness(1);
        }

        private void walletid_LostFocus(object sender, RoutedEventArgs e)
        {
            walletaddress.BorderThickness = new Thickness(0);
        }

        private async void DepositSats_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (DepositSats != null && (DepositSats.Text.ToLower().StartsWith("lnurl") || DepositSats.Text.ToLower().StartsWith("lightning:lnurl")))
            {
                string lnurl = DepositSats.Text;
                if(DepositSats.Text.StartsWith("lightning:"))
                {
                    //if lnurl starts with lightning, that's fine but we remove it here for following steps.
                    lnurl = lnurl.Replace("lightning:", "");
                }

                string value = await lightning.decodeLNURL(MainHandler.myWallet, lnurl);
                clipboard.Foreground = Brushes.Green;
                clipboard.Content = "You will receive " + value +" Sats";
                if (GenerateInvoice != null)  GenerateInvoice.Content = "Receive";

            }
            else if (GenerateInvoice != null) GenerateInvoice.Content = "Generate Invoice";
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Escape) {
                this.Close();
            }

        }
    }

}
