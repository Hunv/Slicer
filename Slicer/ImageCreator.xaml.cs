using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        }

        public int ZoomFactor { get; set; } = 1;
        public double StrokeThin { get; set; } = 0.3;
        public double StrokeThick { get; set; } = 1;
        public double MouseX { get; set; }
        public double MouseY { get; set; }
        public double PolarLenght { get; set; }
        public double PolarAngle { get; set; }
        public ObservableCollection<string> SelectedCoordinates { get; set; } = new ObservableCollection<string>();
        public List<CutterPathItem> CutterPath { get; set; } = new List<CutterPathItem>();
        public Visibility Class2Visible { get; set; } = Visibility.Collapsed;
        public Visibility Class3Visible { get; set; } = Visibility.Collapsed;
        public Thickness CutterEmulatorPosition { get; set; } = new Thickness(0);
        public ImageSource PolarGridBackgroundImage { get; set; }

        private const double _DefaultStrokeThin = 0.3;
        private const double _DefaultStrokeThick = 1;
        private const int _MaximumZoomFactor = 512;
        private List<Point> Coordinates = new List<Point>();

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
        

        private void ButtonZoom_Click(object sender, RoutedEventArgs e)
        {
            if (ZoomFactor * 2 <= _MaximumZoomFactor)
            {
                ZoomFactor *= 2;
                StrokeThin /= 2;
                svImageCreator.ScrollToVerticalOffset(svImageCreator.VerticalOffset * 2);
                svImageCreator.ScrollToHorizontalOffset(svImageCreator.HorizontalOffset * 2);
            }
            else
            {
                ZoomFactor = 512;
                StrokeThin *= _DefaultStrokeThin / _MaximumZoomFactor;
            }

            if (ZoomFactor >= 4)
            {
                if (ZoomFactor >= 16)
                {
                    if (Class2Visible != Visibility.Visible)
                    {
                        Class2Visible = Visibility.Visible;
                        OnPropertyChanged("Class2Visible");
                    }

                    if (Class3Visible != Visibility.Visible)
                    {
                        Class3Visible = Visibility.Visible;
                        OnPropertyChanged("Class3Visible");
                    }
                }
                else
                {
                    if (Class2Visible != Visibility.Visible)
                    {
                        Class2Visible = Visibility.Visible;
                        OnPropertyChanged("Class2Visible");
                    }
                }
            }

            OnPropertyChanged("ZoomFactor");
            OnPropertyChanged("StrokeThin");
        }

        private void ButtonUnzoom_Click(object sender, RoutedEventArgs e)
        {
            if (ZoomFactor / 2 >= 1)
            {
                ZoomFactor /= 2;
                StrokeThin *= 2;
                svImageCreator.ScrollToVerticalOffset(svImageCreator.VerticalOffset / 2);
                svImageCreator.ScrollToHorizontalOffset(svImageCreator.HorizontalOffset / 2);
            }
            else
            {
                ZoomFactor = 1;
                StrokeThin = _DefaultStrokeThin;
                svImageCreator.ScrollToVerticalOffset(0);
                svImageCreator.ScrollToHorizontalOffset(0);
            }

            if (ZoomFactor < 16)
            {
                if (ZoomFactor < 4)
                {
                    if (Class2Visible != Visibility.Collapsed)
                    {
                        Class2Visible = Visibility.Collapsed;
                        OnPropertyChanged("Class2Visible");
                    }
                    if (Class3Visible != Visibility.Collapsed)
                    {
                        Class3Visible = Visibility.Collapsed;
                        OnPropertyChanged("Class3Visible");
                    }
                }
                else
                {
                    if (Class3Visible != Visibility.Collapsed)
                    {
                        Class3Visible = Visibility.Collapsed;
                        OnPropertyChanged("Class3Visible");
                    }
                }
            }

            OnPropertyChanged("ZoomFactor");
            OnPropertyChanged("StrokeThin");
        }

        private void UserControl_MouseMove(object sender, MouseEventArgs e)
        {
            MouseX = e.GetPosition(grdCoord).X;
            MouseY = e.GetPosition(grdCoord).Y;
            
            ((ToolTip)this.ToolTip).HorizontalOffset = e.GetPosition(svImageCreator).X + 5;
            ((ToolTip)this.ToolTip).VerticalOffset = e.GetPosition(svImageCreator).Y - 30;

            var centeredX = MouseX - 765;
            var centeredY = MouseY - 765;
            int quadrantAddition = 0;

            if (centeredX > 0 && centeredY > 0)
                quadrantAddition = -180;
            else if (centeredX > 0 && centeredY <= 0)
                quadrantAddition = 180;
                        
            if (centeredX == 0) //Avoid dividing by zero
                centeredX = 0.001;

            PolarLenght = Math.Sqrt(centeredX * centeredX + centeredY * centeredY);
            PolarAngle = (Math.Atan(centeredY / centeredX) * 180 / Math.PI) + quadrantAddition;

            OnPropertyChanged("MouseX");
            OnPropertyChanged("MouseY");
            OnPropertyChanged("PolarLenght");
            OnPropertyChanged("PolarAngle");
        }

        private void grdCoord_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            AddCutterPathPoint(e.GetPosition(grdCoord).X, e.GetPosition(grdCoord).Y);

            //Set CutterEmulator Position
            CutterEmulatorPosition = new Thickness(
                (int)(e.GetPosition(grdCoord).X - elCutter.Width / 2),
                (int)(e.GetPosition(grdCoord).Y - elCutter.Height / 2),
                (int)(grdCoord.Width - e.GetPosition(grdCoord).X - elCutter.Width / 2),
                (int)(grdCoord.Height - e.GetPosition(grdCoord).Y - elCutter.Height / 2)
                );

            OnPropertyChanged("CutterEmulatorPosition");
        }

        /// <summary>
        /// Adds a new Point on the Path of the Cutter
        /// </summary>
        /// <param name="positionX"></param>
        /// <param name="positionY"></param>
        private void AddCutterPathPoint(double positionX, double positionY)
        {
            //get relative position to last point
            if (Coordinates.Count == 0)
            {
                //Set the Center as start postion
                Coordinates.Add(new Point(765, 765));
            }

            //Get last point
            var lastCenteredX = Coordinates.Last().X - 765;
            var lastCenteredY = Coordinates.Last().Y - 765;
            var lastCenteredLength = Math.Sqrt(lastCenteredX * lastCenteredX + lastCenteredY * lastCenteredY);

            var lastQudarant = 0;
            if (lastCenteredX > 0 && lastCenteredY > 0)
                lastQudarant = -180;
            else if (lastCenteredX > 0 && lastCenteredY <= 0)
                lastQudarant = 180;
            if (lastCenteredX == 0)
                lastCenteredX = 0.001;

            var lastCenteredAngle = (Math.Atan(lastCenteredY / lastCenteredX) * 180 / Math.PI) + lastQudarant;



            //Get this actual point
            var thisCenteredX = positionX - 765;
            var thisCenteredY = positionY - 765;
            var thisCenteredLength = Math.Sqrt(thisCenteredX * thisCenteredX + thisCenteredY * thisCenteredY);

            var thisQudarant = 0;
            if (thisCenteredX > 0 && thisCenteredY > 0)
                thisQudarant = -180;
            else if (thisCenteredX > 0 && thisCenteredY <= 0)
                thisQudarant = 180;
            if (thisCenteredX == 0)
                thisCenteredX = 0.001;

            var thisCenteredAngle = (Math.Atan(thisCenteredY / thisCenteredX) * 180 / Math.PI) + thisQudarant;

            //Get the "Field" where the last and current positions are
            var lastStepLength = Math.Floor(lastCenteredLength);
            var thisStepLength = Math.Floor(thisCenteredLength);
            var lastStepAngle = Math.Floor(lastCenteredAngle * 10);
            var thisStepAngle = Math.Floor(thisCenteredAngle * 10);


            //Get delta steps
            var deltaStepsLength = thisStepLength - lastStepLength;
            var deltaStepsAngle = thisStepAngle - lastStepAngle;

            //Get the shortest way for the rotor:
            if (deltaStepsAngle > 1800)
                deltaStepsAngle = (deltaStepsAngle - 3600);
            else if (deltaStepsAngle < -1800)
                deltaStepsAngle = (deltaStepsAngle + 3600);


            //Add currect position to path record
            Coordinates.Add(new Point(positionX, positionY));
            CutterPath.Add(new CutterPathItem((int)deltaStepsAngle, (int)deltaStepsLength));

            //For tooltip
            SelectedCoordinates.Add(Math.Round(positionX, 2) + "/" + Math.Round(positionY, 2) +
                "   " + deltaStepsLength + " Steps Slide/ " + deltaStepsAngle + " Steps Rotor");

            //update GUI
            OnPropertyChanged("SelectedCoordinates");
            
        }

        /// <summary>
        /// Creates the Bytes that describes the shape (="CutterCode")
        /// </summary>
        private void GenerateCutterCode()
        {
            List<byte> cutterCommands = new List<byte>();

            foreach (var aPoint in CutterPath)
            {
                //On first item, Adjust Slice to the start point
                if (aPoint == CutterPath.First())
                {
                    //Do a absolute calibration and not relative
                    cutterCommands.Add(0); //Header
                    cutterCommands.Add(0); //Header
                    cutterCommands.Add(0); //Header
                    cutterCommands.Add(0); //Header

                    var initAngle = GetInitialAngleBytes(aPoint.DeltaAngle);
                    var initLength = GetInitialSlideBytes(aPoint.DeltaLenght);

                    cutterCommands.Add(initAngle.Value); //Amount of Rotation
                    cutterCommands.Add(initAngle.Key); //Command of Rotation
                    cutterCommands.Add(initLength.Value); //Amount of Slide
                    cutterCommands.Add(initLength.Key); //Command of Slide

                    //Knife down
                    cutterCommands.Add(0x0);
                    cutterCommands.Add((byte)CutterCode.KnifeDown);
                    continue;
                }

                var slide = GetDeltaSlideBytes(aPoint.DeltaLenght);
                var rotor = GetDeltaAngleBytes(aPoint.DeltaAngle);

                cutterCommands.AddRange(rotor);
                cutterCommands.AddRange(slide);
            }

            //Footer:
            cutterCommands.Add(0);
            cutterCommands.Add((byte)CutterCode.KnifeUp);
            cutterCommands.Add(0);
            cutterCommands.Add((byte)CutterCode.Finish);

            SaveCutterCode(cutterCommands.ToArray());
        }

        /// <summary>
        /// Saves the given ByteArray to a file
        /// </summary>
        /// <param name="cutterCode"></param>
        private void SaveCutterCode(byte[] cutterCode)
        {
            var sFD = new SaveFileDialog();
            if (sFD.ShowDialog() == true)
            {
                try
                {
                    var sW = new System.IO.FileStream(sFD.FileName, System.IO.FileMode.OpenOrCreate);
                    sW.Write(cutterCode, 0, cutterCode.Length);
                    sW.Close();

                    MessageBox.Show("Saved");
                }
                catch (Exception ea)
                {
                    MessageBox.Show("Unable to save CutterCodeFile. Reason: " + ea.Message);
                }
            }

            
        }

        /// <summary>
        /// Gets the command for the slide as a delta for the given angle
        /// </summary>
        /// <param name="angleSteps">the amount of steps to do for the rotor</param>
        /// <returns>A list of bytes. The List contains a set of Command byte and Amount byte. If steps are more than 255, there will be a list of 4 bytes etc.</returns>
        private List<byte> GetDeltaAngleBytes(int angleSteps) 
        {
            //Return of the byte for the given angle
            var cutCodeBytes = new List<byte>();

            //Take the shortest way
            if (angleSteps > 1800)
                angleSteps -= 3600;
            else if (angleSteps < -1800)
                angleSteps += 3600;

            //If the steps are more than one command can parse, add additional commands for the steps
            while (angleSteps > 255 || angleSteps < -255)
            {
                var factor = angleSteps < 0 ? -1 : 1;                
                cutCodeBytes.AddRange(GetDeltaAngleBytes(255 * factor));
                angleSteps -= 255 * factor;
            }            

            //Get the values for CMD Byte and Amount byte
            var cmdByte = angleSteps < 0 ? (byte)CutterCode.TurnCW :(byte)CutterCode.TurnCCW;
            var stepByte = angleSteps < 0 ? (byte)(angleSteps * -1) : (byte)angleSteps;

            //Add the bytes to return code
            cutCodeBytes.Add(stepByte);
            cutCodeBytes.Add(cmdByte);

            return cutCodeBytes;
        }

        /// <summary>
        /// Gets the command for the slide as a delta for the given lenght
        /// </summary>
        /// <param name="lengthSteps">the amount of steps to do for the slide</param>
        /// <returns>A list of bytes. The List contains a set of Command byte and Amount byte. If steps are more than 255, there will be a list of 4 bytes etc.</returns>
        private List<byte> GetDeltaSlideBytes(int lengthSteps)
        {
            var cutCodeBytes = new List<byte>();

            while (lengthSteps > 255 || lengthSteps < -255)
            {
                var factor = lengthSteps < 0 ? -1 : 1;
                cutCodeBytes.AddRange(GetDeltaSlideBytes(255 * factor));
                lengthSteps -= 255;
            }

            var cmdByte = (lengthSteps > 0 ? (byte)CutterCode.SledgeOut : (byte)CutterCode.SledgeIn);
            var stepByte = (lengthSteps < 0 ? (byte)(lengthSteps*-1) : (byte)lengthSteps);

            cutCodeBytes.Add(stepByte);
            cutCodeBytes.Add(cmdByte);

            return cutCodeBytes;
        }

        /// <summary>
        /// Gets the Rotorposition for the given Steps as absolute position
        /// Usually only used on start
        /// </summary>
        /// <param name="angleSteps">The steps of the rotor to do (1 step = 0.1°)</param>
        /// <returns>A KVP of CommandByte (Key) and AmountByte (Value)</returns>
        private KeyValuePair<byte, byte> GetInitialAngleBytes(int angleSteps) //returns <CmdByte, AmountByte>
        {
            var cmdByte = (byte)CutterCode.StartTurnCCW25;//Set to 0x80
            if (angleSteps < 0)
            {
                cmdByte = (byte)CutterCode.StartTurnCW25; //Change to 0x40 if CCW rotation is required
                angleSteps *= -1;
            }
                        
            while (angleSteps > 255)
            {
                cmdByte++; //Increase the cmdByte to the next 25,5°
                angleSteps -= 255;
            }
            //Ste the Steps for the Angle
            var stepByte = (byte)angleSteps;

            //Return the angle
            return new KeyValuePair<byte, byte>(cmdByte, stepByte);
        }

        /// <summary>
        /// Gets the Slideposition for the given Steps as absolute position
        /// Usually only used on start
        /// </summary>
        /// <param name="lengthSteps">The steps of the slider to do (1 step = 1/255 of an inch)</param>
        /// <returns>A KVP of CommandByte (Key) and AmountByte (Value)</returns>
        private KeyValuePair<byte, byte> GetInitialSlideBytes(int lengthSteps) //reurns <CmdByte, AmountByte>
        {
            var cmdByte = (byte)CutterCode.StartSledgeAndWaitForGoCenter; //Set Postion to Center (0x90)

            if (lengthSteps < 0)
                lengthSteps *= -1;

            //increase the cmdByte as long ans there is the value above one byte
            while(lengthSteps > 255)
            {
                cmdByte++;
                lengthSteps -= 255;
            }

            var stepByte = (byte)lengthSteps;

            return new KeyValuePair<byte, byte>(cmdByte, stepByte);
        }

        private void btnGenCutterCode_Click(object sender, RoutedEventArgs e)
        {
            GenerateCutterCode();            
        }

        private void btnKnifeDown_Click(object sender, RoutedEventArgs e)
        {
            //Todo
        }

        private void btnKnifeUp_Click(object sender, RoutedEventArgs e)
        {
            //Todo
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            CutterEmulatorPosition = new Thickness(
                grdCoord.Width / 2 - elCutter.Width/2, 
                grdCoord.Height / 2 - elCutter.Height/2, 
                grdCoord.Width / 2 - elCutter.Width/2, 
                grdCoord.Height / 2 - elCutter.Height/2
                );
            OnPropertyChanged("CutterEmulatorPosition");
        }

        private void UserControl_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.W:
                    CutterEmulatorPosition = new Thickness(CutterEmulatorPosition.Left, CutterEmulatorPosition.Top - 1, CutterEmulatorPosition.Right, CutterEmulatorPosition.Bottom + 1);
                    break;
                case Key.S:
                    CutterEmulatorPosition = new Thickness(CutterEmulatorPosition.Left, CutterEmulatorPosition.Top + 1, CutterEmulatorPosition.Right, CutterEmulatorPosition.Bottom - 1);
                    break;
                case Key.A:
                    CutterEmulatorPosition = new Thickness(CutterEmulatorPosition.Left - 1, CutterEmulatorPosition.Top, CutterEmulatorPosition.Right + 1, CutterEmulatorPosition.Bottom);
                    break;
                case Key.D:
                    CutterEmulatorPosition = new Thickness(CutterEmulatorPosition.Left + 1, CutterEmulatorPosition.Top, CutterEmulatorPosition.Right - 1, CutterEmulatorPosition.Bottom);
                    break;
            }
            AddCutterPathPoint(CutterEmulatorPosition.Left, CutterEmulatorPosition.Top);
            OnPropertyChanged("CutterEmulatorPosition");
        }

        private void btnLoadBackgroundImage_Click(object sender, RoutedEventArgs e)
        {
            var oFD = new OpenFileDialog();
            oFD.Filter = "Image files|*.png;*.bmp;*.jpg;*.jpeg;*.gif";
            if (oFD.ShowDialog() == true)
            {
                try
                {                    
                    var bitmap = new BitmapImage(new Uri(oFD.FileName));
                    PolarGridBackgroundImage = bitmap;
                    OnPropertyChanged("PolarGridBackgroundImage");
                }
                catch (Exception ea)
                {
                    MessageBox.Show("Unable to load image. Maybe it is a unsupported format." + System.Environment.NewLine + "Errormessage: " + ea.Message);
                }
            }
        }
    }
}
