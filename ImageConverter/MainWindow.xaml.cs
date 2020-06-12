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
using System.Timers;

namespace ImageConverter
{    
    
    public partial class MainWindow : Window
    {
        // declare some global variables
        // first are the bitmap sources for the main page image, tilesheet, and output image respectively
        BitmapSource bmMain;
        BitmapSource bmTilesheet;
        BitmapSource bmResult;

        double xRatio = 1;
        double yRatio = 1;

        bool imageLoaded = false;
        bool tilesLoaded = false;

        // setup some default constants regarding the size and presentation of tile displays
        int tilesMargin = 5;
        int tilesMainDisplaySize = 60;
        int tilesSize = 1;
        int tilesRowsInSheet = 1;
        int tilesColsInSheet = 1;

        int tilesPanelCols = 3;

        int tilesOptionsDisplaySize = 70;

        int defaultTileSize = 32;

        // declare the internal list of tiles, tiles being a custom class I created
        List<Tile> tilesList = new List<Tile>();

        public MainWindow()
        {
            InitializeComponent();
        }      
       
        // create the output image upon clicking the create button
        private void btnCreate_Click(object sender, RoutedEventArgs e)
        {                      

            // check that there is at least one enable tile to create the output image with
            bool allTilesDisabled = true;

            foreach (Tile tile in tilesList)
            {
                if (tile.enabled == true)
                {
                    allTilesDisabled = false;
                    break;
                }
            }
            if (allTilesDisabled)
            {
                // if every tile is disabled, show the relevant error message then return                
                showErrorMessage();
                return;
            }

            // set the mouse to a wait cursor for the duration of the image processing
            Mouse.OverrideCursor = Cursors.Wait;

            try
            {
                // scale down the original image to the dimensions specified in the xScaling and yScaling text boxes
                var bmScaled = new TransformedBitmap(bmMain, new ScaleTransform(Convert.ToDouble(xScaling.Text) / bmMain.PixelWidth, Convert.ToDouble(yScaling.Text) / bmMain.PixelHeight));

                // calculate the stride (how many bytes in a single row of the image) of the resulting image
                int stride = (bmScaled.PixelWidth * bmScaled.Format.BitsPerPixel + 7) / 8;
                // create a byte array to copy this image into
                byte[] pixels = new byte[bmScaled.PixelHeight * stride];
                // copy the scaled imaged into the byte array
                bmScaled.CopyPixels(pixels, stride, 0);

                // create the output image, with size equal to the specified dimensions * the size of a tile, since the dimensions are given in 'tile' units.
                WriteableBitmap background = new WriteableBitmap(tilesSize * (Convert.ToInt32(xScaling.Text)), tilesSize * (Convert.ToInt32(yScaling.Text)), bmMain.DpiX, bmMain.DpiY, bmMain.Format, null);

                // iterate over the byte array, incrementing by 4 since there are 4 bytes in each pixel (A,R,G,B)
                for (int i = 0; i <= pixels.Length - 4; i = i + 4)
                {
                    // calcluate the color of the current pixel from each set of 4 bytes
                    Color c = Color.FromArgb(pixels[i + 3], pixels[i + 2], pixels[i + 1], pixels[i]);               
                    
                    // for this pixel, find the closest tile in the tiles list 
                    var closestTile = findClosestTile(tilesList, c);
                    // calculate the position where this tile should be placed from the index
                    int row = (i / 4) / (Convert.ToInt32(xScaling.Text) + 0);
                    int col = (i / 4) % (Convert.ToInt32(xScaling.Text) + 0);

                    // add the found closest tile to the output image
                    addTileToImage(closestTile, row, col, background);
                }

                // show the output image, and enable saving
                imgMain.Source = bmResult;
                btnSave.IsEnabled = true;

            }
            finally
            {
                // reset the mouse cursor
                Mouse.OverrideCursor = null;
            }   

        }

        void showErrorMessage()
        {
            rowWarning.Height = new GridLength(23);
            updateWarning("Couldn't create an image as no tiles are enabled");
        }
        
        void addTileToImage(Tile tile, int row, int col, WriteableBitmap background)
        {
            // calclutate the stride and create the byte array to hold the tile to be copied
            int sourceBytesPerPixel = bmMain.Format.BitsPerPixel / 8;
            int sourceBytesPerLine = tile.bmSource.PixelWidth * sourceBytesPerPixel;
            byte[] sourcePixels = new byte[sourceBytesPerLine * tile.bmSource.PixelHeight];

            // copy the tile into the byte array
            tile.bmSource.CopyPixels(sourcePixels, sourceBytesPerLine, 0);

            // calculate the source rectangle in which the tile should be placed into the output image
            Int32Rect sourceRect = new Int32Rect(col * tilesSize, row * tilesSize, tile.bmSource.PixelWidth, tile.bmSource.PixelHeight);

            // copy the tile into the output image, then update the result bitmap
            background.WritePixels(sourceRect, sourcePixels, sourceBytesPerLine, 0);
            bmResult = background;
        }

        Tile findClosestTile(List<Tile> tilesList, Color c)
        {           
            
            var smallestDifference = Double.MaxValue;
            Tile closestTile = null;

            // express the colour as a Delta-E colour to be used in the colour distance calculations
            ColorFormulas deltaEPixelcolor = new ColorFormulas(c.R, c.G, c.B);

            foreach (Tile tile in tilesList)
            {                    
                // ignore all tiles that have been disabled
                if (tile.enabled == false)
                {
                    continue;
                }

                // calculate the colour difference using the Delta-E formulas
                var deltaEDifference = tile.colorFormulas.CompareTo(deltaEPixelcolor);
                
                if ((deltaEDifference < smallestDifference))
                {
                    smallestDifference = deltaEDifference;
                    closestTile = tile;
                }
            }
            return closestTile;
        }

        

        // open dialog to select source image upon mouse click of the main rectangle
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

        // load image upon dropping it into the main rectangle
        private void rectMain_Drop(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    // create array of the filenames dropped in
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                    // load the first file as the source image
                    loadImage(files[0]);
                }
            }
            catch (Exception e1) { }

        }

        // load an image into the main rectangle, given its file path
        void loadImage(string sFileName)
        {
            bmMain = new BitmapImage(new Uri(sFileName));
            imgMain.Source = bmMain;

            // calculate the ratio between the height and width of this image, and its reciprocal as well
            xRatio = Convert.ToDouble(bmMain.PixelHeight) / Convert.ToDouble(bmMain.PixelWidth);
            yRatio = 1 / xRatio;


            imageLoaded = true;
            // enable the dimension text boxes if the tiles have also been loaded
            tryEnablingBoxes();
            // hide the underlying rectangle and text
            rectMain.Visibility = Visibility.Hidden;
            tbMain.Visibility = Visibility.Hidden;
        }


        // open a dialog to either select 1 file (a tilesheet) or many (individual tiles)
        private void rectTilesPreview_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "Images (*.png;*.jpg)|*.png;*.jpg";
            dlg.Multiselect = true;
            Nullable<bool> result = dlg.ShowDialog();
            if (result == true)
            {
                // hide the underlying rectangle and text
                rectTilesPreview.Visibility = Visibility.Hidden;
                textTilesPreview.Visibility = Visibility.Hidden;

                // check whether the selected stuff is a single file or many
                if (dlg.FileNames.Length == 1)
                {
                    // if there is only one file, treat it as a tilesheet
                    loadTilesheet(dlg.FileName);
                }
                else
                {
                    // if there are many files, treat them as individual tiles

                    // set a constant tile size, since the separately selected images may be of varying size 
                    tilesSize = defaultTileSize;

                    // clear the internal tiles list as well as all tile displays, since there may be an existing tileset the user is loading over
                    tilesPreviewGrid.Children.Clear();
                    tilesPreviewGrid.RowDefinitions.Clear();
                    tilesList.Clear();
                    tilesOptionsGrid.Children.Clear();
                    tilesOptionsGrid.RowDefinitions.Clear();

                    int index = 0;                    

                    // process each tile given, adding them to the main page list and options page list
                    foreach (string fileName in dlg.FileNames)
                    {
                        ProcessTile(fileName, index++);                        
                    }
                }

                // enable the dimensions text boxes if this and the main image are loaded
                tilesLoaded = true;
                tryEnablingBoxes();

            }
        }


        // load one tilesheet or many tiles upon being dropped in
        // this is essentially the same code as the above function, with the exception being that it loads in the files directly, rather than through a selected file path
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
                        tilesOptionsGrid.Children.Clear();
                        tilesOptionsGrid.RowDefinitions.Clear();

                        int index = 0;                       

                        foreach (string fileName in files)
                        {
                            ProcessTile(fileName, index);                   
                        }
                    }

                    tilesLoaded = true;
                    tryEnablingBoxes();

                }
            }
            catch (Exception e1) { }

        }

        void ProcessTile(string fileName, int index)
        {

            // create a new image for each tile and add it to the stackPanel

            BitmapSource bmTile = new BitmapImage(new Uri(fileName));

            var bmScaled = new TransformedBitmap(bmTile, new ScaleTransform((double)defaultTileSize / bmTile.PixelWidth, (double)defaultTileSize / bmTile.PixelHeight));

            addImageToLists(bmScaled, index);

        }

        void loadTilesheet(string sFileName)
        {
            // show the text box that lets the user change the size of one tile
            textBoxRow.Height = new GridLength(100);
            // load the tilesheet image into a bitmap
            bmTilesheet = new BitmapImage(new Uri(sFileName));

            try
            {
                // split the tilesheet into pieces of the specified size and add them to the list on the main page, and on the options page
                addTilesheetBitsToList();
            }
            catch (System.FormatException e1) { }
        }

        void addTilesheetBitsToList()
        {
            // clear all existing tiles in all lists
            tilesPreviewGrid.Children.Clear();
            tilesPreviewGrid.RowDefinitions.Clear();
            tilesList.Clear();
            tilesOptionsGrid.Children.Clear();
            tilesOptionsGrid.RowDefinitions.Clear();

            // get the width and height of a single tile from the text box
            tilesSize = Convert.ToInt32(tilesSizeBox.Text);

            // limit tile size to above 8 pixels, as 8 is a the lowest conventionally used tile size, and any lower is very intensive and may crash the program
            if (tilesSize < 8)
            {
                return;
            }

            // calculate the number of rows and columns in the tilesheet given the size of a single tile
            tilesRowsInSheet = Convert.ToInt32(Math.Floor(Convert.ToDouble(bmTilesheet.PixelHeight) / Convert.ToDouble(tilesSize)));
            tilesColsInSheet = Convert.ToInt32(Math.Floor(Convert.ToDouble(bmTilesheet.PixelWidth) / Convert.ToDouble(tilesSize)));

            // calculate the stride of the tile sheet
            int stride = (bmTilesheet.PixelWidth * bmTilesheet.Format.BitsPerPixel + 7) / 8;
            // create a byte array to copy the tilesheet into
            byte[] data = new byte[stride * bmTilesheet.PixelHeight];
            // copy the tilesheet into the array
            bmTilesheet.CopyPixels(data, stride, 0);

            int index = 0;

            // iterate over each tile in the tilesheet
            for (int tilesheetRow = 0; tilesheetRow < tilesRowsInSheet; tilesheetRow++)
            {
                for (int tilesheetCol = 0; tilesheetCol < tilesColsInSheet; tilesheetCol++)
                {

                    // create a bitmap to hold the tile
                    WriteableBitmap background = new WriteableBitmap(tilesSize, tilesSize, bmTilesheet.DpiX, bmTilesheet.DpiY, bmTilesheet.Format, null);

                    // copy the tile at the given row and column into the bitmap
                    background.WritePixels
                        (
                          new Int32Rect(tilesheetCol * tilesSize, tilesheetRow * tilesSize, tilesSize, tilesSize),
                          data,
                          stride,
                          0,
                          0
                        );

                    // add this new tile to various lists
                    addImageToLists(background, index++);                  

                }
            }


        }    

        void addImageToLists(BitmapSource bmTile, int index)
        {

            // add the tile to the internal list
            Tile tile = new Tile();
            tile.bmSource = bmTile;
            tilesList.Add(tile);

            // add it to the grid on the main page
            addToMainPageGrid(bmTile, index);

            // add it to the list on the options page
            addToOptionsList(tile, index);
           
        }

        void addToMainPageGrid(BitmapSource bmTile, int index)
        {
            // create the image to add to the grid
            Image newTileImage = new Image();
            // provide it with its source, size, and other display settings
            newTileImage.Source = bmTile;
            newTileImage.Height = tilesMainDisplaySize - tilesMargin;
            newTileImage.Width = tilesMainDisplaySize;
            newTileImage.Stretch = Stretch.Fill;
            newTileImage.Margin = new Thickness(2, 2, 2, 2);

            // calculate the row and column to add the image at, given the index
            int row = index / tilesPanelCols;
            int col = index % tilesPanelCols;

            // create a new row every time the column resets to 0
            if (col == 0)
            {
                var rowDefinition = new RowDefinition();
                rowDefinition.Height = GridLength.Auto;
                tilesPreviewGrid.RowDefinitions.Add(rowDefinition);
            }

            // add the image as a child of the grid at the calculated row and column
            tilesPreviewGrid.Children.Add(newTileImage);
            Grid.SetColumn(newTileImage, col);
            Grid.SetRow(newTileImage, row);            
            
        }

        void addToOptionsList(Tile tile, int index)
        {
            // create a new row in the list, to add various items to
            RowDefinition rowDefinition = new RowDefinition();
            rowDefinition.Height = GridLength.Auto;
            tilesOptionsGrid.RowDefinitions.Add(rowDefinition);

            // in this row, add a checkbox to enable or disable each tile              
            CheckBox cbTileEnabled = new CheckBox();
            cbTileEnabled.IsChecked = tile.enabled;
            cbTileEnabled.HorizontalAlignment = HorizontalAlignment.Center;
            cbTileEnabled.Click += cbTileEnabled_Click;
            cbTileEnabled.MouseEnter += colEnabled_MouseEnter;
            
            cbTileEnabled.Tag = index;
            tile.checkBox = cbTileEnabled;

            tilesOptionsGrid.Children.Add(cbTileEnabled);
            Grid.SetColumn(cbTileEnabled, 0);
            Grid.SetRow(cbTileEnabled, index);

            // then, add the image of the tile
            Image newTileImage = new Image();            
            newTileImage.Source = tile.bmSource;
            newTileImage.Height = tilesOptionsDisplaySize;
            newTileImage.Width = tilesOptionsDisplaySize;
            newTileImage.Stretch = Stretch.Fill;
            newTileImage.Margin = new Thickness(4, 0, 4, 0);


            tilesOptionsGrid.Children.Add(newTileImage);
            Grid.SetColumn(newTileImage, 1);
            Grid.SetRow(newTileImage, index);

            // finally, add a rectangle to display the average colour of the tile
            Rectangle tileAveragecolor = new Rectangle();
            tileAveragecolor.Height = tilesOptionsDisplaySize;
            tileAveragecolor.Width = tilesOptionsDisplaySize;
            SolidColorBrush color = new SolidColorBrush();
            color.Color = tile.color;
            tileAveragecolor.Fill = color;
            tileAveragecolor.Margin = new Thickness(4, 0, 4, 0);

            tilesOptionsGrid.Children.Add(tileAveragecolor);
            Grid.SetColumn(tileAveragecolor, 2);
            Grid.SetRow(tileAveragecolor, index);
                       
        }

        void tryEnablingBoxes()
        {
            // enable the xScaling and yScaling boxes, as well as the create button, if both and image and tileset have been loaded
            if ((imageLoaded == true) && (tilesLoaded == true))
            {
                xScaling.IsEnabled = true;
                yScaling.IsEnabled = true;
                btnCreate.IsEnabled = true;

                // set the default value of these textboxes to the amount of tiles it would take to fill the source image
                xScaling.Text = Convert.ToString((Math.Floor(Convert.ToDouble(bmMain.PixelWidth) / Convert.ToDouble(tilesSize))));
                yScaling.Text = Convert.ToString((Math.Floor(Convert.ToDouble(bmMain.PixelHeight) / Convert.ToDouble(tilesSize))));
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            // open a dialog to select where to save the image to
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.Filter = "Images (.png)|*.png";
            Nullable<bool> result = dlg.ShowDialog();
            if (result == true)
            {
                using (var fileStream = new FileStream(dlg.FileName, FileMode.Create))
                {
                    BitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create((BitmapSource)imgMain.Source));
                    encoder.Save(fileStream);
                }
            }
        }

        private void rectMain_DragEnter(object sender, DragEventArgs e)
        {
            // make the main rectangle get lighter in response to an image being dragged over it
            ColorAnimation ca = new ColorAnimation((Color)ColorConverter.ConvertFromString("#f0f0f0"), new Duration(TimeSpan.FromSeconds(0.2)));
            rectMain.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#dddddd"));
            rectMain.Fill.BeginAnimation(SolidColorBrush.ColorProperty, ca);
        }

        private void rectMain_DragLeave(object sender, DragEventArgs e)
        {
            // reset the main rectangle colour in response to an image being dragged off of it
            ColorAnimation ca = new ColorAnimation((Color)ColorConverter.ConvertFromString("#dddddd"), new Duration(TimeSpan.FromSeconds(0.2)));
            rectMain.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f0f0f0"));
            rectMain.Fill.BeginAnimation(SolidColorBrush.ColorProperty, ca);
        }

        private void rectTilesPreview_DragEnter(object sender, DragEventArgs e)
        {
            // similarly lighten and darken the tiles rectangle upon drag enter and leave
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

        // limit all of the textboxes to numbers only
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

        // whenever either of the dimension text boxes are changed, update the other one to maintain their ratio
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

        // reload the tilesheet splitting upon changing the tile size text box
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

        // when an enabled check box is ticked or unticked, update the corresponding tile's enabled value
        private void cbTileEnabled_Click(object sender, RoutedEventArgs e)
        {
            tilesList[Convert.ToInt32((sender as CheckBox).Tag)].enabled = Convert.ToBoolean((sender as CheckBox).IsChecked);
        }
       
        // buttons to enable or disable all tiles, respectively
        private void btnEnableAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (Tile tile in tilesList)
            {
                tile.enabled = true;
            }            
        }

        private void btnDisableAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (Tile tile in tilesList)
            {
                tile.enabled = false;
            }
        }

        void updateTooltip(String tooltip)
        {
            tbTooltip.Text = tooltip;
            tbOptionsTooltip.Text = tooltip;
            iconTooltip.Visibility = Visibility.Visible;
            iconOptionsTooltip.Visibility = Visibility.Visible;
        }

        private void resetTooltip(object sender, MouseEventArgs e)
        {
            tbTooltip.Text = "";
            tbOptionsTooltip.Text = "";
            iconTooltip.Visibility = Visibility.Hidden;
            iconOptionsTooltip.Visibility = Visibility.Hidden;
        }

        void resetTooltipDynamic(MouseEventHandler e)
        {
            tbTooltip.Text = "";
            tbOptionsTooltip.Text = "";
            iconTooltip.Visibility = Visibility.Hidden;
            iconOptionsTooltip.Visibility = Visibility.Hidden;
        }

        void updateWarning(String warning)
        {            
            tbWarning.Visibility = Visibility.Visible;            
            iconWarning.Visibility = Visibility.Visible;

            tbWarning.Text = warning;            
        }

        private void closeWarning_MouseDown(object sender, MouseButtonEventArgs e)
        {
            rowWarning.Height = new GridLength(0);           
            tbWarning.Visibility = Visibility.Hidden;            
            iconWarning.Visibility = Visibility.Hidden;

            tbWarning.Text = "";
        }
       
        private void rectMain_MouseEnter(object sender, MouseEventArgs e)
        {
            updateTooltip("Drop or browse to the image to be converted");
        }
       
        private void xScaling_MouseEnter(object sender, MouseEventArgs e)
        {
            updateTooltip("The width of the output image, in tiles");
        }
       
        private void yScaling_MouseEnter(object sender, MouseEventArgs e)
        {
            updateTooltip("The height of the output image, in tiles");
        }
      
        private void tilesSizeBox_MouseEnter(object sender, MouseEventArgs e)
        {
            updateTooltip("The size of a single tile, in pixels");
        }

        private void tilesPanelGrid_MouseEnter(object sender, MouseEventArgs e)
        {
            updateTooltip("Drop or browse to a tilesheet or selection of several tiles");
        }

        private void btnCreate_MouseEnter(object sender, MouseEventArgs e)
        {
            updateTooltip("Recreate the loaded image out of the loaded tiles");
        }

        private void btnSave_MouseEnter(object sender, MouseEventArgs e)
        {
            updateTooltip("Open a file dialog and browse to where you want to save the image");
        }

        private void imgMain_MouseEnter(object sender, MouseEventArgs e)
        {
            updateTooltip("You can scroll to zoom, and drag with the right mouse button to pan across the image. You can also left-click or drop a new image in");
        }        

        private void colEnabled_MouseEnter(object sender, MouseEventArgs e)
        {
            updateTooltip("Disabled tiles won't be used in the output image");
        }

        private void colTile_MouseEnter(object sender, MouseEventArgs e)
        {
            updateTooltip("");
        }

        private void colColor_MouseEnter(object sender, MouseEventArgs e)
        {
            updateTooltip("The average colour of each tile");
        }

        
    }
}