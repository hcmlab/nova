using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ssi
{
    /// <summary>
    /// Interaktionslogik für Window1.xaml
    /// </summary>
    public partial class LightningTipps : Window
    {
        Lightning lightning = new Lightning();
        MainHandler handler;
        public LightningTipps(MainHandler handler)
        {
            InitializeComponent();
            this.handler = handler;
            if(!MainHandler.ENABLE_LIGHTNING)
            {
                Withdraw.Visibility = Visibility.Hidden;
                internalwalletlabel.Visibility = Visibility.Hidden;
    }
        }

        private async void GenerateInvoice_Click(object sender, RoutedEventArgs e)
        {
            clipboard.Content = "Creating invoice, please wait..";
            Lightning.LightningInvoice invoice = await lightning.CreateInvoice(Defaults.Lightning.LNTippingInvoiceID, Convert.ToUInt32(DepositSats.Text), "NOVA Tipp (External)");
            DepositAdress.Text = invoice.payment_request;
            System.Windows.Clipboard.SetText(invoice.payment_request);
            clipboard.Content = "Copied to clipboard";

            Bitmap bmp = await lightning.GetQRImage(invoice.payment_request);
            QR.Source = ConvertBitmap(bmp);
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
            //DemoCode
            Lightning.LightningInvoice invoice = await lightning.CreateInvoice(Defaults.Lightning.LNTippingInvoiceID, Convert.ToUInt32(DepositSats.Text), "NOVA Tipp (Internal)");
            if (MainHandler.myWallet == null)
            {
                LightningWalletMangement ln = new LightningWalletMangement(handler);
                ln.Show();
                
            }
            else
            {
                string message = await lightning.PayInvoice(MainHandler.myWallet, invoice.payment_request);
                statuswithdraw.Content = message;
                if (message == "Success")
                {
                    statuswithdraw.Foreground = System.Windows.Media.Brushes.Green;
                    MainHandler.myWallet.balance = await lightning.GetWalletBalance(MainHandler.myWallet.admin_key);

                }
                else
                {
                    statuswithdraw.Foreground = System.Windows.Media.Brushes.Red;
                }
            }
           

        }
    }
}
