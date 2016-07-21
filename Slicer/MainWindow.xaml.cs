using Microsoft.Win32;
using System;
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
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public MainWindow()
        {
            InitializeComponent();
            ColorCode = 0;
        }

        #region Images
        private int _ImageWidth = 128;
        private int _ImageHeight = 128;
        private double _ImageScale = 4;
        private double _FrontendImageWidth = 512;

        //Is the Mouse currently drawing?
        private bool _IsDrawing = false;

        private string _LastFilePath = "";

        


        #region Properties
        private byte[] _ImageBytes;
        /// <summary>
        /// Contains the bytes from the SplashScreen
        /// </summary>
        public byte[] ImageBytes
        {
            get
            {
                return _ImageBytes;
            }
            set
            {
                _ImageBytes = value;
                OnPropertyChanged("ImageBytes");
                OrderBits(null);
            }
        }

        private System.Collections.BitArray _ImageBits;
        /// <summary>
        /// Contains the bytes from the SplashScreen
        /// </summary>
        public System.Collections.BitArray ImageBits
        {
            get
            {
                return _ImageBits;
            }
            set
            {
                _ImageBits = value;
                OnPropertyChanged("ImageBits");
                OnPropertyChanged("DrawImage");
                OnPropertyChanged("ImageBytesBase4");
                OnPropertyChanged("ImageBytesBase4Image");
                OnPropertyChanged("PreviewImage");
                OnPropertyChanged("DrawImage");
            }
        }
               

        public WriteableBitmap PreviewImage { get { return GetPreviewImage(); } }

        public WriteableBitmap DrawImage
        {
            get
            {
                return GetImage();
            }
        }

        private byte _ColorCode = 0;
        public byte ColorCode
        {
            get
            {
                return _ColorCode;
            }
            set
            {
                if (value < 4)
                {
                    _ColorCode = value;
                    OnPropertyChanged("ColorCode");
                    OnPropertyChanged("IsBlack");
                    OnPropertyChanged("IsDarkGray");
                    OnPropertyChanged("IsLightGray");
                    OnPropertyChanged("IsWhite");
                }

            }
        }
        public bool IsBlack { get { return ColorCode == 0; } set { ColorCode = 0; } }
        public bool IsDarkGray { get { return ColorCode == 2; } set { ColorCode = 2; } }
        public bool IsLightGray { get { return ColorCode == 1; } set { ColorCode = 1; } }
        public bool IsWhite { get { return ColorCode == 3; } set { ColorCode = 3; } }

        #endregion

        private System.Collections.BitArray OrderBits(System.Collections.BitArray toOrderBits)
        {
            var splashScreenBits = toOrderBits == null ? new System.Collections.BitArray(ImageBytes) : toOrderBits;

            //Go through the Bits and Shift them to the correct order for further handling
            for (int i = 0; i < splashScreenBits.Count - 1; i += 8)
            {
                var byteBits = new bool[] 
                { 
                    splashScreenBits.Get(i), 
                    splashScreenBits.Get(i+1), 
                    splashScreenBits.Get(i+2), 
                    splashScreenBits.Get(i+3), 
                    splashScreenBits.Get(i+4), 
                    splashScreenBits.Get(i+5), 
                    splashScreenBits.Get(i+6), 
                    splashScreenBits.Get(i+7) 
                };

                splashScreenBits.Set(i, byteBits[6]);
                splashScreenBits.Set(i + 1, byteBits[7]);
                splashScreenBits.Set(i + 2, byteBits[4]);
                splashScreenBits.Set(i + 3, byteBits[5]);
                splashScreenBits.Set(i + 4, byteBits[2]);
                splashScreenBits.Set(i + 5, byteBits[3]);
                splashScreenBits.Set(i + 6, byteBits[0]);
                splashScreenBits.Set(i + 7, byteBits[1]);
            }

            return splashScreenBits;
        }

        private string GetImageBase4()
        {
            if (ImageBytes == null || ImageBytes.Length == 0 || ImageBits == null || ImageBits.Count == 0)
                return "";

            string base4String = "";

            //Go through the Bits. Each 2 Bits are 1 Pixel. 00 = Black, 01 = dark gray, 10 = light gray, 11 = White
            for (int i = 0; i < ImageBits.Count; i += 2)
            {
                base4String += (2 * (ImageBits.Get(i) ? 1 : 0)) + (ImageBits.Get(i + 1) ? 1 : 0);
                if ((i) % 256 == 0 && i != 0)
                    base4String += System.Environment.NewLine;
            }

            return base4String;
        }

        private WriteableBitmap GetPreviewImage()
        {
            if (ImageBytes == null || ImageBits == null)
                return null;
            
            //For saving the 2BitPerPixel-Image to 1BytePerPixel-Image
            var img1bpp = new byte[ImageBytes.Length * 4];
            //Go through the Bits. Each 2 Bits are 1 Pixel. 00 = Black, 01 = dark gray, 10 = light gray, 11 = White
            for (int i = 0; i < ImageBits.Count; i += 2)
            {
                var colorNumber = (2 * (ImageBits.Get(i) ? 1 : 0)) + (ImageBits.Get(i + 1) ? 1 : 0);
                switch(colorNumber)
                {
                    case 0:
                        img1bpp[i/2] = 0;
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

            //var img = new WriteableBitmap(128, 128, 96, 96, PixelFormats.Gray8, BitmapPalettes.Gray256);
            //img.WritePixels(new Int32Rect(0, 0, img.PixelWidth, img.PixelHeight), img1bpp, 128, 0);

            var img = new WriteableBitmap(_ImageWidth, _ImageHeight, 96, 96, PixelFormats.Gray8, BitmapPalettes.Gray256);
            img.WritePixels(new Int32Rect(0, 0, img.PixelWidth, img.PixelHeight), img1bpp, img.PixelWidth, 0);

            return img;
            
        }

        public void SaveImage(System.Collections.BitArray bits, string path)
        {
            var disorderedBits = OrderBits(bits);
            var bytesToWrite = new byte[disorderedBits.Length / 8];
            disorderedBits.CopyTo(bytesToWrite, 0);

            var sW = new System.IO.FileStream(path, System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.Write);
            sW.Write(bytesToWrite, 0, bytesToWrite.Length);
            sW.Close();
        }

        private WriteableBitmap GetImage()
        {
            if (ImageBits == null)
                return null;

            //For saving the 2BitPerPixel-Image to 1BytePerPixel-Image
            var img1bpp = new byte[ImageBits.Length / 2];
            //Go through the Bits. Each 2 Bits are 1 Pixel. 00 = Black, 01 = dark gray, 10 = light gray, 11 = White
            for (int i = 0; i < ImageBits.Count; i += 2)
            {
                var colorNumber = (2 * (ImageBits.Get(i) ? 1 : 0)) + (ImageBits.Get(i + 1) ? 1 : 0);
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

            var img = new WriteableBitmap(_ImageWidth, _ImageHeight, 96, 96, PixelFormats.Gray8, BitmapPalettes.Gray256);
            img.WritePixels(new Int32Rect(0, 0, img.PixelWidth, img.PixelHeight), img1bpp, img.PixelWidth, 0);

            return img;
        }

        private void ChangePixel(int x, int y, byte colorCode)
        {
            if (ImageBits == null)
                return;

            var bitIndex = y * _ImageWidth * 2 + x * 2;
            var bitColor1 = (colorCode == 1 || colorCode == 3 ? true : false);
            var bitColor2 = (colorCode == 2 || colorCode == 3 ? true : false);

            if (bitIndex + 1 > ImageBits.Count)
                return;

            ImageBits.Set(bitIndex, bitColor1);
            ImageBits.Set(bitIndex + 1, bitColor2);
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
            SaveImage(ImageBits, @"C:\temp\image.dat");
        }

        private void btnImport_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog oFD = new OpenFileDialog();
            if (oFD.ShowDialog() == true)
            {
                LoadFile(oFD.FileName);
            }
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            if (_LastFilePath.Length != 0)
                LoadFile(_LastFilePath);
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
            //ChangePixel((int)pos.X / _ImageScale/2, (int)pos.Y / _ImageScale/2, ColorCode);
            ChangePixel((int)(pos.X / _ImageScale), (int)(pos.Y / _ImageScale), ColorCode);
        }

        private void Image_MouseLeave(object sender, MouseEventArgs e)
        {
            _IsDrawing = false;
        }

        #endregion

        private void LoadFile(string path)
        {
            var ext = System.IO.Path.GetExtension(path);
            switch (ext.ToLower())
            {
                case ".dat":
                case ".dot":
                    //Get File Size
                    var fileInfo = new System.IO.FileInfo(path);
                    if (fileInfo.Length == 37248) //Full Screen Images (i.e. Flash Screen)
                    {
                        _ImageWidth = 128;
                        _ImageHeight = 128;
                        _ImageScale = _FrontendImageWidth / 128;
                    }
                    else if (fileInfo.Length == 512)
                    {
                        _ImageWidth = 64;
                        _ImageHeight = 32;
                        _ImageScale = _FrontendImageWidth / 64;
                    }
                    else if (fileInfo.Length == 2048)
                    {
                        _ImageWidth = 128;
                        _ImageHeight = 64;
                        _ImageScale = _FrontendImageWidth / 128;
                    }
                    else if (fileInfo.Length == 1152) //Shape, after it is selected
                    {
                        _ImageWidth = 96;
                        _ImageHeight = 48;
                        _ImageScale = _FrontendImageWidth / 96;
                    }
                    else if (fileInfo.Length == 200) //Thumbnail for Menus
                    {
                        _ImageWidth = 40;
                        _ImageHeight = 20;
                        _ImageScale = _FrontendImageWidth / 40;
                    }
                    else if (fileInfo.Length == 384) //Icon like Battery low
                    {
                        throw new Exception("todi");
                        _ImageWidth = 40;
                        _ImageHeight = 20;
                        _ImageScale = _FrontendImageWidth / 40;
                    }
                    else
                    {
                        MessageBox.Show("Unsupported Format");
                        return;
                    }

                    _LastFilePath = path;

                    //Direct import
                    ImageBytes = System.IO.File.ReadAllBytes(path);
                    ImageBits = OrderBits(null);
                    break;
                case ".bmp":
                    //Convert (todo)
                    break;
            }

            OnPropertyChanged("ImageBits");
            OnPropertyChanged("DrawImage");
            OnPropertyChanged("ImageBytesBase4");
            OnPropertyChanged("ImageBytesBase4Image");
            OnPropertyChanged("PreviewImage");
            OnPropertyChanged("DrawImage");
        }
        #endregion

        #region Cutter
        private string _CutPath = @"D:\Kriss\Downloads\Slice30.iso_\Slice30\11\11-16\11-16-11\11-16-11.DN2";
        private byte[] _CutData;
        public string CutTranslation { get; set; }
        private Dictionary<byte, List<int>> _CutStatistics = new Dictionary<byte, List<int>>();
        public string CutStatistics
        {
            get
            {
                var statistic = "";                
                foreach (var aSet in _CutStatistics)
                {
                    statistic += 
                        aSet.Key.ToString("x") + System.Environment.NewLine + 
                        "- Count: " + aSet.Value[0] + System.Environment.NewLine + 
                        "- Amount: " + aSet.Value[1] + System.Environment.NewLine;
                }
                return statistic;
            }
        }

        private void LoadCutData(string path)
        {
            _CutData = System.IO.File.ReadAllBytes(path);
        }

        private void TranslateCutData(byte[] cutData)
        {
            for (int i = 0; i < cutData.Length; i += 2)
            {
                if (!_CutStatistics.ContainsKey(cutData[i + 1]))
                    _CutStatistics.Add(cutData[i + 1], new List<int> { 0, 0 });

                _CutStatistics[cutData[i + 1]][0]++;
                _CutStatistics[cutData[i + 1]][1]+=cutData[i];

                CutTranslation += 
                    cutData[i].ToString("x") + "\t" + 
                    cutData[i + 1].ToString("x") + "\t" + 
                    TranslateAmount(cutData[i]) + "\t" + 
                    TranslateAction(cutData[i + 1]) +
                    System.Environment.NewLine;
            }
            OnPropertyChanged("CutTranslation");
            OnPropertyChanged("CutStatistics");
        }

        private string TranslateAmount(byte amount)
        {
            return (Math.Round(amount / 2.55,2) + "%");
        }

        private string TranslateAction(byte action)
        {
            switch (action)
            {
                case 0x0:
                    return "Ignored";                    
                case 0x20:
                    return "Knife up";
                case 0x30:
                    return "Knife down";
                case 0x40:
                    return "Turn 25,5°";
                case 0x41:
                    return "Turn 51°";
                case 0x42:
                    return "Turn 76,5°";
                case 0x43:
                    return "Turn 102°";
                case 0x44:
                    return "Turn 127,5°";
                case 0x45:
                    return "Turn 153°";
                case 0x46:
                    return "Turn 188,5°";
                case 0x47:
                    return "Turn 209°";
                case 0x48:
                    return "Turn 234,5°";
                case 0x49:
                    return "Turn 255°";
                case 0x4A:
                    return "Turn 280,5°";
                case 0x4B:
                    return "Turn 306°";
                case 0x4C:
                    return "Turn 331,5°";                    
                case 0x4D:
                    return "Turn 357°";
                case 0x4E:
                    return "Turn 382,5°";
                case 0x4F:
                    return "Turn 408°";
                case 0x50:
                    return "Knife inner";
                case 0x51:
                    return "Knife middle";
                case 0x52:
                    return "Knife outer";
                case 0x60:
                    return "Turn 25,5° CW";
                case 0x61:
                    return "Turn 25,5° CCW";
                case 0x62:
                    return "Turn 25,5° CW (unused?)";
                case 0x70:
                    return "Knife inner";
                case 0x71:
                    return "Knife middle";                    
                case 0x72:
                    return "Knife (unused?)-------";
                case 0x80:
                    return "Turn 25,5° CW";
                case 0x81:
                    return "Turn 51° CW";
                case 0x82:
                    return "Turn 76,5° CW";
                case 0x90:
                    return "Knife inner + Wait start";
                case 0x91:
                    return "Knife middle + Wait start";
                case 0x92:
                    return "Knife outer + Wait start";
                case 0xF0:
                    return "File End";
            }
            return "Unknown Command";
        }

        #endregion

        private void btnLoadCut_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog oFD = new OpenFileDialog();
            if (oFD.ShowDialog() == true)
            {
                CutTranslation = "";
                _CutStatistics = new Dictionary<byte, List<int>> ();
                _CutPath = oFD.FileName;
                LoadCutData(oFD.FileName);
                TranslateCutData(_CutData);
            }
            
        }
    }
}
