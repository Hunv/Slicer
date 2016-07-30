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
        }


        public List<ShapeDirectory> ShapeList { get; set; }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //Find and Load SD-Card
            var drives = System.IO.DriveInfo.GetDrives();

            //a drive was found?
            var driveFound = false;

            foreach (var drive in drives)
            {
                if (drive.DriveType == System.IO.DriveType.Removable)
                {
                    if (drive.IsReady)
                    {
                        //Is it a SD Card for Shapes?
                        if (System.IO.File.Exists(drive.Name + "MSPBOOT.BIN"))
                        {
                            LoadFolder(drive.Name);
                            driveFound = true;
                            break;
                        }
                    }
                }
            }

            //Let user select folder
            if (!driveFound)
            {
                var fbd = new System.Windows.Forms.FolderBrowserDialog();
                if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    LoadFolder(fbd.SelectedPath);
                }
            }
        }

        /// <summary>
        /// Loads a Folder with the Slice Files
        /// </summary>
        /// <param name="name"></param>
        private void LoadFolder(string name)
        {
            ShapeList = new List<ShapeDirectory>();
            //Get the Shapes in Root folder of images
            foreach (var aDir in System.IO.Directory.GetDirectories(name + "\\11"))
            {
                var sd = new ShapeDirectory();
                sd.Path = aDir;
                sd.Name = aDir.Split('\\').Last();
                sd.Icon = ImageHelper.LoadImage(ImageHelper.LoadBits(aDir + ".DAT"));
                sd.Shapes = new List<ShapeDirectory>();

                foreach (var aShape in System.IO.Directory.GetDirectories(aDir))
                {
                    var sh = new ShapeDirectory();
                    sh.Path = aShape;
                    sh.Name = aShape.Split('\\').Last();
                    sh.Icon = ImageHelper.LoadImage(ImageHelper.LoadBits(aShape + ".DAT"));

                    sd.Shapes.Add(sh);
                }

                ShapeList.Add(sd);
            }

            OnPropertyChanged("ShapeList");
        }

        //#region Cutter
        //private string _CutPath = @"D:\Kriss\Downloads\Slice30.iso_\Slice30\11\11-16\11-16-11\11-16-11.DN2";
        //private byte[] _CutData;
        //public string CutTranslation { get; set; }
        //private Dictionary<byte, List<int>> _CutStatistics = new Dictionary<byte, List<int>>();
        //public string CutStatistics
        //{
        //    get
        //    {
        //        var statistic = "";                
        //        foreach (var aSet in _CutStatistics)
        //        {
        //            statistic += 
        //                aSet.Key.ToString("x") + System.Environment.NewLine + 
        //                "- Count: " + aSet.Value[0] + System.Environment.NewLine + 
        //                "- Amount: " + aSet.Value[1] + System.Environment.NewLine;
        //        }
        //        return statistic;
        //    }
        //}

        //private void LoadCutData(string path)
        //{
        //    _CutData = System.IO.File.ReadAllBytes(path);
        //}

        //private void TranslateCutData(byte[] cutData)
        //{
        //    for (int i = 0; i < cutData.Length; i += 2)
        //    {
        //        if (!_CutStatistics.ContainsKey(cutData[i + 1]))
        //            _CutStatistics.Add(cutData[i + 1], new List<int> { 0, 0 });

        //        _CutStatistics[cutData[i + 1]][0]++;
        //        _CutStatistics[cutData[i + 1]][1]+=cutData[i];

        //        CutTranslation += 
        //            cutData[i].ToString("x") + "\t" + 
        //            cutData[i + 1].ToString("x") + "\t" + 
        //            TranslateAmount(cutData[i]) + "\t" + 
        //            TranslateAction(cutData[i + 1]) +
        //            System.Environment.NewLine;
        //    }
        //    OnPropertyChanged("CutTranslation");
        //    OnPropertyChanged("CutStatistics");
        //}

        //private string TranslateAmount(byte amount)
        //{
        //    return (Math.Round(amount / 2.55,2) + "%");
        //}

        //private string TranslateAction(byte action)
        //{
        //    switch (action)
        //    {
        //        case 0x0:
        //            return "Ignored";                    
        //        case 0x20:
        //            return "Knife up";
        //        case 0x30:
        //            return "Knife down";
        //        case 0x40:
        //            return "Rotor CW 0°-25,5°";
        //        case 0x41:
        //            return "Rotor CW 25°-51°";
        //        case 0x42:
        //            return "Rotor CW 51°-76,5°";
        //        case 0x43:
        //            return "Rotor CW 76,5°-102°";
        //        case 0x44:
        //            return "Rotor CW 102°-127,5°";
        //        case 0x45:
        //            return "Rotor CW 127,5°-153°";
        //        case 0x46:
        //            return "Rotor CW 153°-188,5°";
        //        case 0x47:
        //            return "Rotor CW 188,5°-209°";
        //        case 0x48:
        //            return "Rotor CW 209°-234,5°";
        //        case 0x49:
        //            return "Rotor CW 234,5°-255°";
        //        case 0x4A:
        //            return "Rotor CW 255°-280,5°";
        //        case 0x4B:
        //            return "Rotor CW 280,5°-306°";
        //        case 0x4C:
        //            return "Rotor CW 306°-331,5°";                    
        //        case 0x4D:
        //            return "Rotor CW 331,5°-357°";
        //        case 0x4E:
        //            return "Rotor CW 357°-382,5°";
        //        case 0x4F:
        //            return "Rotor CW 382,5°-408°";
        //        case 0x50:
        //            return "Slide 1'";
        //        case 0x51:
        //            return "Slide 2'";
        //        case 0x52:
        //            return "Slide 3'";
        //        case 0x60:
        //            return "Turn CW";
        //        case 0x61:
        //            return "Turn CCW";
        //        case 0x70:
        //            return "Slide move in";
        //        case 0x71:
        //            return "Slide move out";
        //        case 0x80:
        //            return "Rotor CCW 0°-25,5°";
        //        case 0x81:
        //            return "Rotor CCW 25°-51°";
        //        case 0x82:
        //            return "Rotor CCW 51°-76,5°";
        //        case 0x83:
        //            return "Rotor CCW 76,5°-102°";
        //        case 0x84:
        //            return "Rotor CCW 102°-127,5°";
        //        case 0x85:
        //            return "Rotor CCW 127,5°-153°";
        //        case 0x86:
        //            return "Rotor CCW 153°-188,5°";
        //        case 0x87:
        //            return "Rotor CCW 188,5°-209°";
        //        case 0x88:
        //            return "Rotor CCW 209°-234,5°";
        //        case 0x89:
        //            return "Rotor CCW 234,5°-255°";
        //        case 0x8A:
        //            return "Rotor CCW 255°-280,5°";
        //        case 0x8B:
        //            return "Rotor CCW 280,5°-306°";
        //        case 0x8C:
        //            return "Rotor CCW 306°-331,5°";
        //        case 0x8D:
        //            return "Rotor CCW 331,5°-357°";
        //        case 0x8E:
        //            return "Rotor CCW 357°-382,5°";
        //        case 0x8F:
        //            return "Rotor CCW 382,5°-408°";
        //        case 0x90:
        //            return "Knife 1' + Wait start";
        //        case 0x91:
        //            return "Knife 2' + Wait start";
        //        case 0x92:
        //            return "Knife 3' + Wait start";
        //        case 0xF0:
        //            return "File End";
        //    }
        //    return "Unknown Command";
        //}

        //#endregion

        //private void btnLoadCut_Click(object sender, RoutedEventArgs e)
        //{
        //    OpenFileDialog oFD = new OpenFileDialog();
        //    if (oFD.ShowDialog() == true)
        //    {
        //        CutTranslation = "";
        //        _CutStatistics = new Dictionary<byte, List<int>> ();
        //        _CutPath = oFD.FileName;
        //        LoadCutData(oFD.FileName);
        //        TranslateCutData(_CutData);
        //    }

        //}

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
    }
}
