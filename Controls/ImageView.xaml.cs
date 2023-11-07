using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
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
    /// Interaktionslogik für ImageView.xaml
    /// </summary>
    public partial class ImageView : Window
    {
        System.Drawing.Image image;
        string inputfilename;

        public ImageView(System.Drawing.Image image, string filename)
        {
            InitializeComponent();
            this.image = image;
            this.inputfilename = filename;
            using (var ms = new MemoryStream())
            {
                image.Save(ms, ImageFormat.Bmp);
                ms.Seek(0, SeekOrigin.Begin);

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = ms;
                bitmapImage.EndInit();

                Image.Source = bitmapImage;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string filename = FileTools.SaveFileDialog(inputfilename, ".jpg", "Image (*.jpg)|*.jpg", "");
            if (filename == null) return;
            image.Save(filename);
        }
    }
}
