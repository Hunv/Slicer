using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;

namespace Slicer
{
    /// <summary>
    /// Interaction logic for ImageCreator.xaml
    /// </summary>
    public partial class ShapeCreator : UserControl, INotifyPropertyChanged
    {
        [DllImport("gdi32")]
        static extern uint GetEnhMetaFileBits(IntPtr hemf, uint cbBuffer, byte[] lpbBuffer);

        public ShapeCreator()
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
        
        public List<List<CutterPathItem>> CutterPath { get; set; } = new List<List<CutterPathItem>>();
        public Visibility Class2Visible { get; set; } = Visibility.Collapsed;
        public Visibility Class3Visible { get; set; } = Visibility.Collapsed;
        public Thickness CutterEmulatorPosition { get; set; } = new Thickness(0);
        public ImageSource PolarGridBackgroundImage { get; set; }
        public PointCollection CutterVisualizerPath { get; set; } = new PointCollection();

        private const double _DefaultStrokeThin = 0.3;
        private const double _DefaultStrokeThick = 1;
        private const int _MaximumZoomFactor = 512;
        private List<System.Windows.Point> Coordinates = new List<System.Windows.Point>();
        //private List<Point> _SvgPathPoints = new List<Point>();

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

            //add new Path, if not path exists
            if (CutterPath.Count == 0)
                CutterPath.Add(new List<CutterPathItem>());
            
            //Get last point
            var lastCenteredX = Coordinates.Last().X - 765;
            var lastCenteredY = Coordinates.Last().Y - 765;
            var lastCenteredLength = Math.Sqrt(lastCenteredX * lastCenteredX + lastCenteredY * lastCenteredY);

            var lastQudarant = 0;
            if (lastCenteredX >= 0 && lastCenteredY > 0)
                lastQudarant = 180;
            else if (lastCenteredX >= 0 && lastCenteredY <= 0)
                lastQudarant = 180;
            //else if (lastCenteredX < 0)
            //    lastQudarant = 360;

            if (lastCenteredX == 0)
                lastCenteredX = 0.001;

            var lastCenteredAngle = (Math.Atan(lastCenteredY / lastCenteredX) * 180 / Math.PI) + lastQudarant;



            //Get this actual point
            var thisCenteredX = positionX - 765;
            var thisCenteredY = positionY - 765;
            var thisCenteredLength = Math.Sqrt(thisCenteredX * thisCenteredX + thisCenteredY * thisCenteredY);

            var thisQudarant = 0;
            if (thisCenteredX >= 0 && thisCenteredY > 0)
                thisQudarant = 180;
            else if (thisCenteredX >= 0 && thisCenteredY <= 0)
                thisQudarant = 180;
            //else if (thisCenteredX < 0)
            //    thisQudarant = 360;

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
            CutterPath.Last().Add(new CutterPathItem((int)deltaStepsAngle, (int)deltaStepsLength));
            
            //update GUI
            OnPropertyChanged("SelectedCoordinates");
        }

        /// <summary>
        /// Creates the Bytes that describes the shape (="CutterCode")
        /// </summary>
        public void GenerateCutterCode()
        {
            List<byte> cutterCommands = new List<byte>();

            //Add the Header   
            cutterCommands.Add(0x48); //Header
            cutterCommands.Add(0x75); //Header
            cutterCommands.Add(0x6E); //Header
            cutterCommands.Add(0x76); //Header

            foreach (var aCutterPath in CutterPath)
            {
                //For each part of the Cutterpath
                foreach (var aPoint in aCutterPath)
                {
                    //On first item, Adjust Slice to the start point
                    if (aPoint == aCutterPath.First() && aCutterPath == CutterPath.First())
                    {
                        //Do a absolute calibration and not relative
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
                    
                    if (rotor[0] > 0)
                        cutterCommands.AddRange(rotor);
                    if (slide[0] > 0)
                        cutterCommands.AddRange(slide);

                    if (aPoint == aCutterPath.First() && aCutterPath != CutterPath.First()) //if it is the first point of a path that is not the first path
                    {
                        //Knife down
                        cutterCommands.Add(0x0);
                        cutterCommands.Add((byte)CutterCode.KnifeDown);
                        continue;
                    }
                }


                //Knife up, when cutting of the part is done
                cutterCommands.Add(0);
                cutterCommands.Add((byte)CutterCode.KnifeUp);
            }

            //Footer:
            cutterCommands.Add(0);
            cutterCommands.Add((byte)CutterCode.Finish);

            //Save the bytes to file
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
                    var sW = new System.IO.FileStream(sFD.FileName, System.IO.FileMode.Create);
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

        

        /// <summary>
        /// Loads the Path from SVG file
        /// </summary>
        /// <param name="svgPathString"></param>
        /// <param name="overwrite"></param>
        private void loadSvgPath(string svgPathString)
        {
            //Add a new Path
            CutterPath.Add(new List<CutterPathItem>());

            //Remove the letters and get the coordinates 
            svgPathString = svgPathString.Replace('C', ' ').TrimStart('M');
            var svgPathStringArray = svgPathString.Split(' ');

            //Remove the Period
            for (var i = 0; i < svgPathStringArray.Length; i++)
            {
                svgPathStringArray[i] = svgPathStringArray[i].Split('.')[0];
            }

            var svgPathPoints = new  List<Point>();

            //Convert to Points for Grid
            for (var i = 0; i < svgPathStringArray.Length; i += 2)
            {
                svgPathPoints.Add(new Point(Convert.ToInt32(svgPathStringArray[i]) + 255, Convert.ToInt32(svgPathStringArray[i + 1]) + 255));
            }

            //Remove Points that are existing twice
            for (var i = 0; i < svgPathPoints.Count - 1; i++)
            {
                if (svgPathPoints[i].X == svgPathPoints[i + 1].X && svgPathPoints[i].Y == svgPathPoints[i + 1].Y)
                {
                    svgPathPoints.RemoveAt(i + 1);
                    i--;
                }
            }

            setCutterVisualizerPath(svgPathPoints);
        }

        /// <summary>
        /// Draws the Path of the cutter including the offset
        /// </summary>
        /// <param name="offsetX"></param>
        /// <param name="offsetY"></param>
        private void setCutterVisualizerPath(List<Point> svgPathPoints)
        {
            var offsetSvgPath = new Point[svgPathPoints.Count];
            svgPathPoints.CopyTo(offsetSvgPath);

            for (var i = 0; i < offsetSvgPath.Length; i++)
            {
                AddCutterPathPoint(offsetSvgPath[i].X, offsetSvgPath[i].Y);
            }
            
            CutterVisualizerPath = new PointCollection(CutterVisualizerPath.Concat(offsetSvgPath));
            OnPropertyChanged("CutterVisualizerPath");
        }   

        public void ZoomIn()
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

        public void ZoomOut()
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

        public void LoadBackgroundImage()
        {
            var oFD = new OpenFileDialog();
            oFD.Filter = "Image files|*.png;*.bmp;*.jpg;*.jpeg;*.gif";
            if (oFD.ShowDialog() == true)
            {
                try
                {
                    var bitmapImage = new BitmapImage(new Uri(oFD.FileName));
                    PolarGridBackgroundImage = bitmapImage;
                    OnPropertyChanged("PolarGridBackgroundImage");
                }
                catch (Exception ea)
                {
                    MessageBox.Show("Unable to load image. Maybe it is a unsupported format." + System.Environment.NewLine + "Errormessage: " + ea.Message);
                }
            }
        }

        public void LoadSvg()
        {
            var oFD = new OpenFileDialog();
            oFD.Filter = "SVG File|*.svg";
            if (oFD.ShowDialog() == true)
            {
                Coordinates.Clear();
                CutterPath.Clear();

                //Load SVG File
                var xDoc = new XmlDocument();
                xDoc.Load(oFD.FileName);

                var gNode = xDoc.DocumentElement;

                foreach (XmlNode aPath in gNode["g"].ChildNodes)
                {
                    if (aPath.Name.ToLower() == "path" && aPath.Attributes["d"] != null)
                    {
                        //Load the path and add it to Image Path
                        loadSvgPath(aPath.Attributes["d"].InnerText);
                    }
                }
            }
        }        
    }
}
