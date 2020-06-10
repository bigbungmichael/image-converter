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
using PanAndZoom;
using DeltaE;
using CustomTile;



namespace ImageConverter
{
    
    //to do
    //margin constants in xaml
    //make tile panel grid width better
    //make tilesheet only update with valid data
    //set lower tile size limit
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
        int tilesMainDisplaySize = 60;
        int tilesSize = 1;
        int tilesRowsInSheet = 1;
        int tilesColsInSheet = 1;

        int tilesPanelCols = 3;

        int tilesOptionsDisplaySize = 70;

        int defaultTileSize = 32;



        List<Tile> tilesList = new List<Tile>();

        public MainWindow()
        {
            InitializeComponent();
        }      
       

        private void btnCreate_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;

            try
            {
                var bmScaled = new TransformedBitmap(bmMain, new ScaleTransform(Convert.ToDouble(xScaling.Text) / bmMain.PixelWidth, Convert.ToDouble(yScaling.Text) / bmMain.PixelHeight));
                imgMain.Source = bmScaled;

                int stride = (bmScaled.PixelWidth * bmScaled.Format.BitsPerPixel + 7) / 8;
                byte[] pixels = new byte[bmScaled.PixelHeight * stride];

                bmScaled.CopyPixels(pixels, stride, 0);

                WriteableBitmap background = new WriteableBitmap(tilesSize * (Convert.ToInt32(xScaling.Text)), tilesSize * (Convert.ToInt32(yScaling.Text)), bmMain.DpiX, bmMain.DpiY, bmMain.Format, null);

                for (int i = 0; i <= pixels.Length - 4; i = i + 4)
                {
                    Color c = Color.FromArgb(pixels[i + 3], pixels[i + 2], pixels[i + 1], pixels[i]);

                    SolidColorBrush color = new SolidColorBrush();
                    color.Color = c;
                    rectTest.Fill = color;

                    var closestTile = findClosestTile(tilesList, c);
                    int row = (i / 4) / (Convert.ToInt32(xScaling.Text) + 0);
                    int col = (i / 4) % (Convert.ToInt32(xScaling.Text) + 0);

                    addTileToImage(closestTile, row, col, background);
                }

                imgMain.Source = imgResult.Source;

                btnSave.IsEnabled = true;

            }
            finally
            {
                Mouse.OverrideCursor = null;
            }

            

        }

        void addTileToImage(Tile tile, int row, int col, WriteableBitmap background)
        {

            int sourceBytesPerPixel = bmMain.Format.BitsPerPixel / 8;
            int sourceBytesPerLine = tile.bmSource.PixelWidth * sourceBytesPerPixel;
            byte[] sourcePixels = new byte[sourceBytesPerLine * tile.bmSource.PixelHeight];

            tile.bmSource.CopyPixels(sourcePixels, sourceBytesPerLine, 0);

            Int32Rect sourceRect = new Int32Rect(col * tilesSize, row * tilesSize, tile.bmSource.PixelWidth, tile.bmSource.PixelHeight);
            background.WritePixels(sourceRect, sourcePixels, sourceBytesPerLine, 0);

            imgResult.Source = background;
        }

        Tile findClosestTile(List<Tile> tilesList, Color c)
        {

            //float factorSaturation = 100;

            var drawingColor = System.Drawing.Color.FromArgb(c.A, c.R, c.G, c.B);

            var hue1 = drawingColor.GetHue();
            var sat1 = drawingColor.GetSaturation();
            var bright1 = drawingColor.GetBrightness();

            var smallestDifference = Double.MaxValue;
            Tile closestTile = null;

            ColorFormulas deltaEPixelcolor = new ColorFormulas(c.R, c.G, c.B);

            foreach (Tile tile in tilesList)
            {
                //var difference =                     
                //    getHueDistance(tile.drawingColor, drawingColor) + 
                //    getSaturationDistance(tile.drawingColor.GetSaturation(), sat1) + 
                //    getBrightnessDistance(tile.drawingColor.GetBrightness(), bright1);

                //var euclideanDifference = getEuclideanDistance(tile.color, c);

                var deltaEDifference = tile.colorFormulas.CompareTo(deltaEPixelcolor);

                tile.difference = deltaEDifference;
                tile.differenceText.Text = Convert.ToString(deltaEDifference);

                if ((deltaEDifference < smallestDifference))
                {
                    smallestDifference = deltaEDifference;
                    closestTile = tile;
                }
            }



            return closestTile;

        }

        

        //opens dialog to select image upon mouse click
        private void rectMain_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "Images (*.png;*.jpg)|*.png;*.jpg";
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
                    //creates array of the filenames dropped in
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                    //loads the first file as the source image
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


            imageLoaded = true;
            tryEnablingBoxes();

            rectMain.Visibility = Visibility.Hidden;

        }


        //opens a dialog to either select 1 file (a tilesheet) or many (individual tiles)
        private void rectTilesPreview_MouseDown(object sender, MouseButtonEventArgs e)
        {


            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "Images (*.png;*.jpg)|*.png;*.jpg";
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
                    tilesSize = defaultTileSize;

                    tilesPreviewGrid.Children.Clear();
                    tilesPreviewGrid.RowDefinitions.Clear();
                    
                    int index = 0;                    

                    foreach (string fileName in dlg.FileNames)
                    {
                        ProcessTile(fileName, index++);                        
                    }
                }



                tilesLoaded = true;
                tryEnablingBoxes();

                addTilesToOptionList();

            }
        }


        //loads 1 tilesheet or many tiles upon being drag and dropped in
        private void rectTilesPreview_Drop(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    rectTilesPreview.Visibility = Visibility.Hidden;
                    textTilesPreview.Visibility = Visibility.Hidden;

                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                    if (files.Length == 1)
                    {
                        loadTilesheet(files[0]);
                    }
                    else
                    {
                        tilesSize = defaultTileSize;

                        tilesPreviewGrid.Children.Clear();
                        tilesPreviewGrid.RowDefinitions.Clear();
                        tilesList.Clear();

                        int index = 0;                       

                        foreach (string fileName in files)
                        {
                            ProcessTile(fileName, index);                   
                        }
                    }

                    tilesLoaded = true;
                    tryEnablingBoxes();

                    addTilesToOptionList();

                }
            }
            catch (Exception e1) { }

        }

        void loadTilesheet(string sFileName)
        {

            textBoxRow.Height = new GridLength(100);

            bmTilesheet = new BitmapImage(new Uri(sFileName));

            try
            {
                addTilesheetBitsToList();
            }
            catch (System.FormatException e1) { }
        }

        void addTilesheetBitsToList()
        {

            tilesPreviewGrid.Children.Clear();
            tilesPreviewGrid.RowDefinitions.Clear();
            tilesList.Clear();

            // get the width and height of a single tile from the text box
            tilesSize = Convert.ToInt32(tilesSizeBox.Text);

            //limit tile size to above 8
            if (tilesSize < 8)
            {
                return;
            }

            tilesRowsInSheet = Convert.ToInt32(Math.Floor(Convert.ToDouble(bmTilesheet.PixelHeight) / Convert.ToDouble(tilesSize)));
            tilesColsInSheet = Convert.ToInt32(Math.Floor(Convert.ToDouble(bmTilesheet.PixelWidth) / Convert.ToDouble(tilesSize)));

            // calculate the stride (how many bytes in a single row of the image) of the tile sheet
            int stride = (bmTilesheet.PixelWidth * bmTilesheet.Format.BitsPerPixel + 7) / 8;

            // allocating space to hold the comnplete tile sheet
            byte[] data = new byte[stride * bmTilesheet.PixelHeight];

            // copying the tilesheet into the buffer
            bmTilesheet.CopyPixels(data, stride, 0);

            int index = 0;

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

                    addImageToList(background, index++);                  

                }
            }


        }

        void addTilesToOptionList()
        {
            //iterate through all children of the grid

            tilesOptionsGrid.Children.Clear();
            tilesOptionsGrid.RowDefinitions.Clear();

            int tilesOptionsGridRow = 0;

            int tilesOptionsDisplaySize = 70;

            int index = 0;


            foreach (Tile tile in tilesList)
            {

                RowDefinition rowDefinition = new RowDefinition();
                rowDefinition.Height = GridLength.Auto;
                tilesOptionsGrid.RowDefinitions.Add(rowDefinition);

                //add a checkbox for each row                
                CheckBox cbTileEnabled = new CheckBox();
                cbTileEnabled.IsChecked = tile.enabled;
                cbTileEnabled.HorizontalAlignment = HorizontalAlignment.Center;
                cbTileEnabled.Click += cbTileEnabled_Click;
                cbTileEnabled.Tag = index++;

                tilesOptionsGrid.Children.Add(cbTileEnabled);
                Grid.SetColumn(cbTileEnabled, 0);
                Grid.SetRow(cbTileEnabled, tilesOptionsGridRow);

                //add an image of each tile for each row
                Image newTileImage = new Image();

                newTileImage.Source = tile.bmSource;
                newTileImage.Height = tilesOptionsDisplaySize;
                newTileImage.Width = tilesOptionsDisplaySize;
                newTileImage.Stretch = Stretch.Fill;
                newTileImage.Margin = new Thickness(2, 0, 2, 0);


                tilesOptionsGrid.Children.Add(newTileImage);
                Grid.SetColumn(newTileImage, 1);
                Grid.SetRow(newTileImage, tilesOptionsGridRow);

                //add an image of the average color of each tile for each row
                Rectangle tileAveragecolor = new Rectangle();

                tileAveragecolor.Height = tilesOptionsDisplaySize;
                tileAveragecolor.Width = tilesOptionsDisplaySize;
                SolidColorBrush color = new SolidColorBrush();
                color.Color = tile.color;
                tileAveragecolor.Fill = color;
                tileAveragecolor.Margin = new Thickness(2, 0, 2, 0);


                tilesOptionsGrid.Children.Add(tileAveragecolor);
                Grid.SetColumn(tileAveragecolor, 2);
                Grid.SetRow(tileAveragecolor, tilesOptionsGridRow);

                //add difference 

                TextBlock tbDifference = new TextBlock();
                tbDifference.Text = "?";
                tbDifference.HorizontalAlignment = HorizontalAlignment.Center;

                tilesOptionsGrid.Children.Add(tbDifference);
                Grid.SetColumn(tbDifference, 3);
                Grid.SetRow(tbDifference, tilesOptionsGridRow);

                tile.differenceText = tbDifference;



                tilesOptionsGridRow++;

            }
        }

        private void cbTileEnabled_Click(object sender, RoutedEventArgs e)
        {
            tilesList[Convert.ToInt32((sender as CheckBox).Tag)].enabled = Convert.ToBoolean((sender as CheckBox).IsChecked);
        }

        void ProcessTile(string fileName, int index)
        {
          
            // create a new image for each tile and add it to the stackPanel

            BitmapSource bmTile = new BitmapImage(new Uri(fileName));

            var bmScaled = new TransformedBitmap(bmTile, new ScaleTransform((double)defaultTileSize / bmTile.PixelWidth, (double)defaultTileSize / bmTile.PixelHeight));            

            addImageToList(bmScaled, index);

        }

        void addImageToList(BitmapSource bmTile, int index)
        {

            //create a new custom tile. provide it with the bitmap, then add it to the internal list of tiles

            Tile tile = new Tile();
            tile.bmSource = bmTile;
            tilesList.Add(tile);

            //add the tile image to the tiles grid on the main page

            addToMainPageGrid(bmTile, index);

            // add to the list view on the options page

            addToOptionsList(tile, index);

           
        }

        void addToMainPageGrid(BitmapSource bmTile, int index)
        {
            Image newTileImage = new Image();

            newTileImage.Source = bmTile;
            newTileImage.Height = tilesMainDisplaySize - tilesMargin;
            newTileImage.Width = tilesMainDisplaySize;
            newTileImage.Stretch = Stretch.Fill;
            newTileImage.Margin = new Thickness(2, 2, 2, 2);

            int row = index / tilesPanelCols;
            int col = index % tilesPanelCols;

            if (col == 0)
            {
                var rowDefinition = new RowDefinition();
                rowDefinition.Height = GridLength.Auto;
                tilesPreviewGrid.RowDefinitions.Add(rowDefinition);
            }

            tilesPreviewGrid.Children.Add(newTileImage);
            Grid.SetColumn(newTileImage, col);
            Grid.SetRow(newTileImage, row);
            
            
        }

        void addToOptionsList(Tile tile, int index)
        {
            RowDefinition rowDefinition = new RowDefinition();
            rowDefinition.Height = GridLength.Auto;
            tilesOptionsGrid.RowDefinitions.Add(rowDefinition);

            //add a checkbox for each row                
            CheckBox cbTileEnabled = new CheckBox();
            cbTileEnabled.IsChecked = tile.enabled;
            cbTileEnabled.HorizontalAlignment = HorizontalAlignment.Center;
            cbTileEnabled.Click += cbTileEnabled_Click;
            cbTileEnabled.Tag = index;

            tilesOptionsGrid.Children.Add(cbTileEnabled);
            Grid.SetColumn(cbTileEnabled, 0);
            Grid.SetRow(cbTileEnabled, index);

            //add an image of each tile for each row
            Image newTileImage = new Image();

            newTileImage.Source = tile.bmSource;
            newTileImage.Height = tilesOptionsDisplaySize;
            newTileImage.Width = tilesOptionsDisplaySize;
            newTileImage.Stretch = Stretch.Fill;
            newTileImage.Margin = new Thickness(2, 0, 2, 0);


            tilesOptionsGrid.Children.Add(newTileImage);
            Grid.SetColumn(newTileImage, 1);
            Grid.SetRow(newTileImage, index);

            //add an image of the average color of each tile for each row
            Rectangle tileAveragecolor = new Rectangle();

            tileAveragecolor.Height = tilesOptionsDisplaySize;
            tileAveragecolor.Width = tilesOptionsDisplaySize;
            SolidColorBrush color = new SolidColorBrush();
            color.Color = tile.color;
            tileAveragecolor.Fill = color;
            tileAveragecolor.Margin = new Thickness(2, 0, 2, 0);


            tilesOptionsGrid.Children.Add(tileAveragecolor);
            Grid.SetColumn(tileAveragecolor, 2);
            Grid.SetRow(tileAveragecolor, index);

            //add difference 

            TextBlock tbDifference = new TextBlock();
            tbDifference.Text = "?";
            tbDifference.HorizontalAlignment = HorizontalAlignment.Center;

            tilesOptionsGrid.Children.Add(tbDifference);
            Grid.SetColumn(tbDifference, 3);
            Grid.SetRow(tbDifference, index);

            tile.differenceText = tbDifference;            
        }

        void tryEnablingBoxes()
        {
            if ((imageLoaded == true) && (tilesLoaded == true))
            {
                xScaling.IsEnabled = true;
                yScaling.IsEnabled = true;

                xScaling.Text = Convert.ToString((Math.Floor(Convert.ToDouble(bmMain.PixelWidth) / Convert.ToDouble(tilesSize))));
                yScaling.Text = Convert.ToString((Math.Floor(Convert.ToDouble(bmMain.PixelHeight) / Convert.ToDouble(tilesSize))));
            }
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

        private void tilesSizeBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
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
            addTilesToOptionList();
        }

        private void tilesSizeBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                addTilesheetBitsToList();
                addTilesToOptionList();
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.Filter = "Images (.png)|*.png";
            Nullable<bool> result = dlg.ShowDialog();
            if (result == true)
            {
                using (var fileStream = new FileStream(dlg.FileName, FileMode.Create))
                {
                    BitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create((BitmapSource) imgMain.Source));
                    encoder.Save(fileStream);
                }
            }
        }
    }
}