using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DeltaE;

namespace CustomTile
{
    public class Tile
    {
        private bool privateEnabled = true;
        public bool enabled
        {
            get
            {
                return privateEnabled;
            }
            set
            {
                privateEnabled = value;
                checkBox.IsChecked = privateEnabled;                
            }
        }        

        public CheckBox checkBox { get; set; } = null;

        private Color privateColor;
        public Color color
        {
            get
            {
                return privateColor;
            }
            set
            {
                privateColor = value;
                drawingColor = System.Drawing.Color.FromArgb(privateColor.A, privateColor.R, privateColor.G, privateColor.B);
                colorFormulas = new ColorFormulas(privateColor.R, privateColor.G, privateColor.B);
            }
        }
        public System.Drawing.Color drawingColor { get; set; } = System.Drawing.Color.FromArgb(0, 0, 0, 0);
        public ColorFormulas colorFormulas { get; set; } = null;

        private BitmapSource privateBmSource = null;
        public BitmapSource bmSource
        {
            get
            {
                return privateBmSource;
            }
            set
            {
                privateBmSource = value;

                var bitmapSource = (BitmapSource)privateBmSource;

                var bmScaled = new TransformedBitmap(bitmapSource, new ScaleTransform(1.0 / bitmapSource.PixelWidth, 1.0 / bitmapSource.PixelHeight));

                var pixels = new byte[4];
                bmScaled.CopyPixels(pixels, 4, 0);
                Color c = Color.FromRgb(pixels[2], pixels[1], pixels[0]);

                color = c;
            }
        }
        //public double difference { get; set; } = 0.0;
        //public TextBlock differenceText { get; set; } = null;

        public Tile()
        {

        }
    }
}
