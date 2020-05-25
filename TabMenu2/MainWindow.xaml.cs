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
using System.Text.RegularExpressions;
using System.IO;
using System.Windows.Media.Animation;

namespace TabMenu2
{
    //to do
    //margin constants in xaml
    //make tile panel grid width better
    //make tilesheet only update with valid data
    public partial class MainWindow : Window
    {
        //declaring global variables

        BitmapSource bmMain;
        BitmapSource bmTilesheet;

        double xRatio = 1;
        double yRatio = 1;

        bool imageLoaded = false;
        bool tilesLoaded = false;

        int tilesMargin = 5;
        int tilesImageSize = 60;
        int tilesSize = 1;

        int tilesPanelCols = 3;

        public MainWindow()
        {
            InitializeComponent();
        }

        //opens folder to select image upon mouse click
        private void rectMain_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "Images (.png)|*.png";            
            Nullable<bool> result = dlg.ShowDialog();
            if (result == true)
            {
                loadImage(dlg.FileName);
            }
        }

        //loads image upon dragging and dropping it in
        private void rectMain_Drop(object sender, DragEventArgs e)
        {
            try
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
            catch (Exception e1) { }

        }

        //loads a provided image displays it on the main page
        void loadImage(string sFileName)
        {
            bmMain = new BitmapImage(new Uri(sFileName));
            imgMain.Source = bmMain;

            xRatio = Convert.ToDouble(bmMain.PixelHeight) / Convert.ToDouble(bmMain.PixelWidth);
            yRatio = 1 / xRatio;

            xScaling.Text = Convert.ToString(bmMain.PixelWidth);
            yScaling.Text = Convert.ToString(bmMain.PixelHeight);

            imageLoaded = true;
            //tryEnablingBoxes();

            rectMain.Visibility = Visibility.Hidden;

        }

        private void rectTilesPreview_MouseDown(object sender, MouseButtonEventArgs e)
        {

            
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "Images (.png)|*.png";
            dlg.Multiselect = true;            
            Nullable<bool> result = dlg.ShowDialog();
            if (result == true)
            {

                rectTilesPreview.Visibility = Visibility.Hidden;
                textTilesPreview.Visibility = Visibility.Hidden;

                

                if (dlg.FileNames.Length == 1)
                {
                    loadTilesheet(dlg.FileName);
                }
                else
                {
                    tilesPreviewGrid.Children.Clear();
                    tilesPreviewGrid.RowDefinitions.Clear();

                    int row = 0;
                    int col = 0;

                    var rowDefinition = new RowDefinition();
                    rowDefinition.Height = GridLength.Auto;
                    tilesPreviewGrid.RowDefinitions.Add(rowDefinition);

                    foreach (string fileName in dlg.FileNames)
                    {
                        ProcessTile(fileName, row, col);
                        col++;
                        if (col == tilesPanelCols)
                        {
                            col = 0;
                            row++;
                            rowDefinition = new RowDefinition();
                            rowDefinition.Height = GridLength.Auto;
                            tilesPreviewGrid.RowDefinitions.Add(rowDefinition);
                        }
                    }
                }

               

                tilesLoaded = true;
                //tryEnablingBoxes();
            }
        }


        void loadTilesheet(string sFileName)
        {

            tilesPreviewGrid.Children.Clear();
            tilesPreviewGrid.RowDefinitions.Clear();

            textBoxRow.Height = new GridLength(100);
            bmTilesheet = new BitmapImage(new Uri(sFileName));

            try
            {
                addTilesheetBitsToList();
            }
            catch(System.FormatException e1) { }
        }

        void addTilesheetBitsToList()
        {
            // get the width and height of a single tile
            tilesSize = Convert.ToInt32(tilesSizeBox.Text);

            int tilesRowsInSheet = Convert.ToInt32(Convert.ToDouble(bmTilesheet.PixelHeight) / Convert.ToDouble(tilesSize));
            int tilesColsInSheet = Convert.ToInt32(Convert.ToDouble(bmTilesheet.PixelWidth) / Convert.ToDouble(tilesSize));

            // calculate the stride (how many bytes in a single row of the image) of the tile sheet
            int stride = bmTilesheet.PixelWidth * (bmTilesheet.Format.BitsPerPixel + 7) / 8;

            // allocating space to hold the comnplete tile sheet
            byte[] data = new byte[stride * bmTilesheet.PixelHeight];

            // copying the tilesheet into the buffer
            bmTilesheet.CopyPixels(data, stride, 0);

            int row = 0;
            int col = 0;

            var rowDefinition = new RowDefinition();
            rowDefinition.Height = GridLength.Auto;
            tilesPreviewGrid.RowDefinitions.Add(rowDefinition);

            for (int tilesheetRow = 0; tilesheetRow < tilesRowsInSheet; tilesheetRow++)
            {
                for (int tilesheetCol = 0; tilesheetCol < tilesColsInSheet; tilesheetCol++)
                {

                    // creating the new image to hold the tile
                    WriteableBitmap background = new WriteableBitmap(tilesSize, tilesSize, bmTilesheet.DpiX, bmTilesheet.DpiY, bmTilesheet.Format, null);

                    background.WritePixels
                        (
                          new Int32Rect(tilesheetCol * tilesSize, tilesheetRow * tilesSize, tilesSize, tilesSize),
                          data,
                          stride,
                          0,
                          0
                        );

                    addImageToList(background, row, col);

                    col++;
                    if (col == tilesPanelCols)
                    {
                        col = 0;
                        row++;
                        rowDefinition = new RowDefinition();
                        rowDefinition.Height = GridLength.Auto;
                        tilesPreviewGrid.RowDefinitions.Add(rowDefinition);
                    }

                }
            }
        }

        

        void ProcessTile(string fileName, int row, int col)
        {
            Console.WriteLine(fileName);
            // create a new image for each tile and add it to the stackPanel
          
            BitmapSource bmTile = new BitmapImage(new Uri(fileName));

            addImageToList(bmTile, row, col);

        }

        void addImageToList(BitmapSource bmTile, int row, int col)
        {

            Image newTileImage = new Image();

            newTileImage.Source = bmTile;
            newTileImage.Height = tilesImageSize - tilesMargin;
            newTileImage.Width = tilesImageSize;
            newTileImage.Stretch = Stretch.Fill;
            newTileImage.Margin = new Thickness(2, 2, 2, 2);

            tilesPreviewGrid.Children.Add(newTileImage);
            Grid.SetColumn(newTileImage, col);
            Grid.SetRow(newTileImage, row);
        }

        private void rectTilesPreview_Drop(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    // Note that you can have more than one file.
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                    
                }
            }
            catch (Exception e1) { }

        }

        private void rectMain_DragEnter(object sender, DragEventArgs e)
        {
            ColorAnimation ca = new ColorAnimation((Color)ColorConverter.ConvertFromString("#f0f0f0"), new Duration(TimeSpan.FromSeconds(0.2)));
            rectMain.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#dddddd"));
            rectMain.Fill.BeginAnimation(SolidColorBrush.ColorProperty, ca);
        }

        private void rectMain_DragLeave(object sender, DragEventArgs e)
        {
            ColorAnimation ca = new ColorAnimation((Color)ColorConverter.ConvertFromString("#dddddd"), new Duration(TimeSpan.FromSeconds(0.2)));
            rectMain.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f0f0f0"));
            rectMain.Fill.BeginAnimation(SolidColorBrush.ColorProperty, ca);
        }

        private void rectTilesPreview_DragEnter(object sender, DragEventArgs e)
        {
            ColorAnimation ca = new ColorAnimation((Color)ColorConverter.ConvertFromString("#f0f0f0"), new Duration(TimeSpan.FromSeconds(0.2)));
            rectTilesPreview.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#dddddd"));
            rectTilesPreview.Fill.BeginAnimation(SolidColorBrush.ColorProperty, ca);
        }

        private void rectTilesPreview_DragLeave(object sender, DragEventArgs e)
        {
            ColorAnimation ca = new ColorAnimation((Color)ColorConverter.ConvertFromString("#dddddd"), new Duration(TimeSpan.FromSeconds(0.2)));
            rectTilesPreview.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f0f0f0"));
            rectTilesPreview.Fill.BeginAnimation(SolidColorBrush.ColorProperty, ca);
        }

        private void xScaling_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void yScaling_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void xScaling_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (xScaling.IsFocused)
                {
                    yScaling.Text = Convert.ToString(Math.Floor(Convert.ToInt32(xScaling.Text) * xRatio));
                }
            }
            catch (System.FormatException e1) { }
        }

        private void yScaling_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (yScaling.IsFocused)
                {
                    xScaling.Text = Convert.ToString(Math.Floor(Convert.ToInt32(yScaling.Text) * yRatio));
                }
            }
            catch (System.FormatException e1) { }
        }

        private void tilesSizeBox_LostFocus(object sender, RoutedEventArgs e)
        {
            addTilesheetBitsToList();
        }

        private void tilesSizeBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                addTilesheetBitsToList();
            }
        }
    }
}
