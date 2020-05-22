using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TabMenu2
{
    //to do
    //margin constants in xaml
    public partial class MainWindow : Window
    {
        //declaring global variables

        BitmapSource bmMain;

        double xRatio = 1;
        double yRatio = 1;

        bool imageLoaded = false;
        bool tilesLoaded = false;

        int tilesMargin = 5;
        int tilesImageSize = 60;
        int tilesSize = 1;

        public MainWindow()
        {
            InitializeComponent();
        }

        //opens folder to select image upon mouse click
        private void Rectangle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "Images (.png)|*.png";
            Nullable<bool> result = dlg.ShowDialog();
            if (result == true)
            {
                loadImage(dlg.FileName);
            }
        }

        void loadImage(string sFileName)
        {
            bmMain = new BitmapImage(new Uri(sFileName));
            imgMain.Source = bmMain;

            xRatio = Convert.ToDouble(bmMain.PixelHeight) / Convert.ToDouble(bmMain.PixelWidth);
            yRatio = 1 / xRatio;

            //xScaling.Text = Convert.ToString(bmMain.PixelWidth);
            //yScaling.Text = Convert.ToString(bmMain.PixelHeight);

            imageLoaded = true;
            //tryEnablingBoxes();

            rectMain.Visibility = Visibility.Hidden;
        }

        private void Rectangle_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Note that you can have more than one file.
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                // Assuming you have one file that you care about, pass it off to whatever
                // handling code you have defined.
                loadImage(files[0]);
            }
        }
    }
}
