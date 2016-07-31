using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace Slicer
{
    /// <summary>
    /// Interaction logic for ImageCreator.xaml
    /// </summary>
    public partial class ImageCreator : UserControl, INotifyPropertyChanged
    {
        public ImageCreator()
        {
            InitializeComponent();
            ColorCode = 0;
        }
        
        //Is the Mouse currently drawing?
        private bool _IsDrawing = false;
        private string _LastFilePath = "";
        private BitArray _ImageBits;

        #region Properties
        public Visibility IsGridVisible { get; set; } = Visibility.Collapsed;

        public WriteableBitmap PreviewImage { get { return GetPreviewImage(); } }

        public WriteableBitmap DrawImage { get; private set; }

        public byte ColorCode
        {
            get { return (byte)GetValue(ColorCodeProperty); }
            set { SetValue(ColorCodeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ColorCode.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ColorCodeProperty =
            DependencyProperty.Register("ColorCode", typeof(byte), typeof(ImageCreator), new PropertyMetadata((byte)0));

        #endregion


        /// <summary>
        /// Gets the PreviewImage
        /// </summary>
        /// <returns></returns>
        private WriteableBitmap GetPreviewImage()
        {
            if (_ImageBits == null)
                return null;

            //For saving the 2BitPerPixel-Image to 1BytePerPixel-Image
            var img1bpp = new byte[_ImageBits.Length / 2];
            //Go through the Bits. Each 2 Bits are 1 Pixel. 00 = Black, 01 = dark gray, 10 = light gray, 11 = White
            for (int i = 0; i < _ImageBits.Count; i += 2)
            {
                var colorNumber = (2 * (_ImageBits.Get(i) ? 1 : 0)) + (_ImageBits.Get(i + 1) ? 1 : 0);
                switch (colorNumber)
                {
                    case 0:
                        img1bpp[i / 2] = 0;
                        break;
                    case 1:
                        img1bpp[i / 2] = 90;
                        break;
                    case 2:
                        img1bpp[i / 2] = 150;
                        break;
                    case 3:
                        img1bpp[i / 2] = 255;
                        break;
                }
            }

            var img = new WriteableBitmap((int)DrawImage.Width, (int)DrawImage.Height, 96, 96, PixelFormats.Gray8, BitmapPalettes.Gray256);
            img.WritePixels(new Int32Rect(0, 0, img.PixelWidth, img.PixelHeight), img1bpp, img.PixelWidth, 0);

            return img;

        }

        /// <summary>
        /// Saves the Image to a file
        /// </summary>
        /// <param name="bits"></param>
        /// <param name="path"></param>
        public void SaveImage(BitArray bits, string path)
        {
            var disorderedBits = ImageHelper.OrderBits(bits);
            var bytesToWrite = new byte[disorderedBits.Length / 8];
            disorderedBits.CopyTo(bytesToWrite, 0);

            var sW = new System.IO.FileStream(path, System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.Write);
            sW.Write(bytesToWrite, 0, bytesToWrite.Length);
            sW.Close();
        }
        
        /// <summary>
        /// Changes the color of a defined Pixel
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="colorCode"></param>
        private void ChangePixel(int x, int y, byte colorCode)
        {
            if (_ImageBits == null)
                return;

            var bitIndex = y * (int)DrawImage.Width * 2 + x * 2;
            var bitColor1 = (colorCode == 1 || colorCode == 3 ? true : false);
            var bitColor2 = (colorCode == 2 || colorCode == 3 ? true : false);

            if (bitIndex + 1 > _ImageBits.Count)
                return;

            _ImageBits.Set(bitIndex, bitColor1);
            _ImageBits.Set(bitIndex + 1, bitColor2);

            DrawImage = ImageHelper.GetImage(_ImageBits, (int)DrawImage.Width, (int)DrawImage.Height);

            OnPropertyChanged("DrawImage");
            OnPropertyChanged("PreviewImage");
        }
        
        #region Events
        // Declare the event
        public event PropertyChangedEventHandler PropertyChanged;

        // Create the OnPropertyChanged method to raise the event
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void btnImport_Click(object sender, RoutedEventArgs e)
        {
            Import();
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            Clear();
        }

        private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _IsDrawing = true;
        }

        private void Image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _IsDrawing = false;
        }

        private void Image_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_IsDrawing)
                return;

            //Change Pixels
            var pos = e.GetPosition(imgImageEditor);

            var scale = imgImageEditor.ActualWidth / DrawImage.Width;
            ChangePixel((int)(pos.X / scale), (int)(pos.Y / scale), ColorCode);
        }

        private void Image_MouseLeave(object sender, MouseEventArgs e)
        {
            _IsDrawing = false;
        }

        #endregion

        public void Import()
        {
            OpenFileDialog oFD = new OpenFileDialog();
            if (oFD.ShowDialog() == true)
            {
                _LastFilePath = oFD.FileName;
                var bits = ImageHelper.LoadBits(oFD.FileName);

                if (bits == null)
                {
                    MessageBox.Show("Unsupported image format");
                    return;
                }

                _ImageBits = bits;
                DrawImage = ImageHelper.LoadImage(bits);

                OnPropertyChanged("DrawImage");
                OnPropertyChanged("PreviewImage");
            }
        }

        public void Clear()
        {
            if (_LastFilePath.Length != 0)
            {
                var bits = ImageHelper.LoadBits(_LastFilePath);
                _ImageBits = bits;
                DrawImage = ImageHelper.LoadImage(bits);

                OnPropertyChanged("DrawImage");
                OnPropertyChanged("PreviewImage");
            }
        }

        public void Save()
        {
            var sfd = new SaveFileDialog();
            if (sfd.ShowDialog() == true)
            {
                SaveImage(_ImageBits, sfd.FileName);
            }
        }

        public void SetColor(byte colorCode)
        {
            ColorCode = colorCode;
        }

        public void ShowGrid(bool showGrid)
        {
            if (showGrid)
                IsGridVisible = Visibility.Visible;
            else
                IsGridVisible = Visibility.Collapsed;

            OnPropertyChanged("IsGridVisible");
        }
    }
}
