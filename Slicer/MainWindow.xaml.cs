using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Ribbon;
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
    public partial class MainWindow : RibbonWindow, INotifyPropertyChanged
    {
        public MainWindow()
        {
            InitializeComponent();
        }


        public List<ShapeDirectory> ShapeList { get; set; }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            OpenFolder();
        }

        private void OpenFolder()
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
                    //Is it a Folder for Shapes?
                    if (System.IO.File.Exists(fbd.SelectedPath + "\\MSPBOOT.BIN"))
                    {
                        LoadFolder(fbd.SelectedPath);
                    }
                    else
                    {
                        MessageBox.Show("This is not a Shape-Folder");
                    }
                }
            }
        }

        /// <summary>
        /// Loads a Folder with the Slice Files
        /// </summary>
        /// <param name="name"></param>
        private void LoadFolder(string name)
        {
            if (!System.IO.Directory.Exists(name + "\\11"))
            {
                MessageBox.Show("The selected folder seems not te be a shape-folder");
                return;
            }


            ShapeList = new List<ShapeDirectory>();

            //Load the Image and Cutter-Icons
            Bitmap imageBitmap = Properties.Resources.ImageIcon.ToBitmap();
            IntPtr hImageBitmap = imageBitmap.GetHbitmap();
            ImageSource imageIcon = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                      hImageBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            Bitmap cutterBitmap = Properties.Resources.CutterIcon.ToBitmap();
            IntPtr hCutterBitmap = cutterBitmap.GetHbitmap();
            ImageSource cutterIcon = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                      hCutterBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());


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
                    sh.Shapes = new List<ShapeDirectory>();

                    foreach(var aShapeTye in System.IO.Directory.GetFiles(aShape))
                    {
                        if (aShape.EndsWith("fdata.txt"))
                            continue;

                        var st = new ShapeDirectory();
                        st.Path = aShapeTye;
                        switch (aShapeTye.Split('\\').Last().Split('.').Last().ToLower())
                        {
                            case "dat":
                                st.Name = "Normal Thumbnail";
                                st.Icon = new WriteableBitmap((BitmapSource)imageIcon);
                                break;
                            case "dot":
                                st.Name = "Normal Start";
                                st.Icon = new WriteableBitmap((BitmapSource)imageIcon);
                                break;
                            case "mat":
                                st.Name = "Mirror Thumbnail";
                                st.Icon = new WriteableBitmap((BitmapSource)imageIcon);
                                break;
                            case "mot":
                                st.Name = "Mirror Start";
                                st.Icon = new WriteableBitmap((BitmapSource)imageIcon);
                                break;
                            case "sat":
                                st.Name = "Shadow Thumbnail";
                                st.Icon = new WriteableBitmap((BitmapSource)imageIcon);
                                break;
                            case "sot":
                                st.Name = "Shadow Start";
                                st.Icon = new WriteableBitmap((BitmapSource)imageIcon);
                                break;
                        }

                        //If name is not set until here, it is a Shape
                        if (st.Name == null)
                        {
                            var ext = aShapeTye.Split('\\').Last().Split('.').Last().ToLower();
                            if (ext.StartsWith("dn")) { st.Name = "Normal Shape "; }
                            else if (ext.StartsWith("dp")) { st.Name = "Normal ??? "; }
                            else if (ext.StartsWith("mn")) { st.Name = "Mirror Shape "; }
                            else if (ext.StartsWith("mp")) { st.Name = "Mirror ??? "; }
                            else if (ext.StartsWith("sn")) { st.Name = "Shadow Shape "; }
                            else if (ext.StartsWith("sp")) { st.Name = "Shadow ??? "; }

                            if (ext.EndsWith("2")) { st.Name += "1 Inch"; }
                            else if (ext.EndsWith("3")) { st.Name += "1.5 Inch"; }
                            else if (ext.EndsWith("4")) { st.Name += "2 Inch"; }
                            else if (ext.EndsWith("5")) { st.Name += "2.5 Inch"; }
                            else if (ext.EndsWith("6")) { st.Name += "3 Inch"; }
                            else if (ext.EndsWith("7")) { st.Name += "3.5 Inch"; }
                            else if (ext.EndsWith("8")) { st.Name += "4 Inch"; }

                            st.Icon = new WriteableBitmap((BitmapSource)cutterIcon);
                        }

                        //st.Icon = sh.Icon;

                        sh.Shapes.Add(st);
                    }

                    sd.Shapes.Add(sh);
                }

                ShapeList.Add(sd);
            }

            OnPropertyChanged("ShapeList");
        }
        
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

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count != 1)
            {
                ribShape.Visibility = Visibility.Collapsed;
                ribImage.Visibility = Visibility.Collapsed;
                return;
            }

            if (((ShapeDirectory)e.AddedItems[0]).IsImage)
            {
                tabContent.SelectedIndex = 0;
                ribImage.Visibility = Visibility.Visible;
                ribShape.Visibility = Visibility.Collapsed;
                ribImage.IsSelected = true;

                imgCreator.Import(((ShapeDirectory)e.AddedItems[0]).Path);
            }
            else
            {
                tabContent.SelectedIndex = 1;
                ribShape.Visibility = Visibility.Visible;
                ribImage.Visibility = Visibility.Collapsed;
                ribShape.IsSelected = true;
            }
        }
        
        private void btnImageImport_Click(object sender, RoutedEventArgs e)
        {
            imgCreator.Import();
        }

        private void btnImageClear_Click(object sender, RoutedEventArgs e)
        {
            imgCreator.Clear();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            imgCreator.Save();
        }

        private void rbImageWhite_Checked(object sender, RoutedEventArgs e)
        {
            if (imgCreator != null)
                imgCreator.SetColor(3);
        }

        private void rbImageLightGray_Checked(object sender, RoutedEventArgs e)
        {
            if (imgCreator != null)
                imgCreator.SetColor(1);
        }

        private void rbImageDarkGray_Checked(object sender, RoutedEventArgs e)
        {
            if (imgCreator != null)
                imgCreator.SetColor(2);
        }

        private void rbImageBlack_Checked(object sender, RoutedEventArgs e)
        {
            if (imgCreator != null)
                imgCreator.SetColor(0);
        }

        private void chkImageShowGrid_Click(object sender, RoutedEventArgs e)
        {
            if (((RibbonCheckBox)sender).IsChecked == true)
                imgCreator.ShowGrid(true);
            else
                imgCreator.ShowGrid(false);
        }

        private void btnShapeSave_Click(object sender, RoutedEventArgs e)
        {
            shaCreator.GenerateCutterCode();
        }

        private void btnShapeLoadSvg_Click(object sender, RoutedEventArgs e)
        {
            shaCreator.LoadSvg();
        }

        private void btnShapeLoadBackground_Click(object sender, RoutedEventArgs e)
        {
            shaCreator.LoadBackgroundImage();
        }
        
        private void btnShapeZoomIn_Click(object sender, RoutedEventArgs e)
        {
            shaCreator.ZoomIn();
        }

        private void btnShapeZoomOut_Click(object sender, RoutedEventArgs e)
        {
            shaCreator.ZoomOut();
        }

        private void btnOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            OpenFolder();
        }

        private void btnAddShape_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
