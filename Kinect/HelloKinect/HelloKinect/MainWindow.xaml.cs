using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
using System.IO.Ports;
using System.Threading;

namespace HelloKinect
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        SerialPort serialPort;
        KinectSensor _sensor;
        int numPeople = 0;
        bool isClose = false;
        double distance = 0.0;

        enum State
        {
            NO_PEOPLE,
            ONE_PERSON_CLOSE,
            ONE_PERSON_FAR,
            MANY_CLOSE,
            MANY_FAR
        }

        State prevState = State.NO_PEOPLE;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (KinectSensor.KinectSensors.Count > 0)
            {
                _sensor = KinectSensor.KinectSensors[0];
                if (_sensor.Status == KinectStatus.Connected)
                {
                    _sensor.ColorStream.Enable();
                    _sensor.DepthStream.Enable();
                    _sensor.SkeletonStream.Enable();
                    _sensor.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(_sensor_AllFramesReady);
                    _sensor.Start();
                }

            }
            if (serialPort == null)
                openSerialPort("COM11", 9600);
            //kinectSensorChooser1.KinectSensorChanged += new DependencyPropertyChangedEventHandler(kinectSensorChooser1_KinectSensorChanged);
            foreach (string port in SerialPort.GetPortNames())
            {
                comboBox1.Items.Add(port);
            }
        }

        KinectAudioSource audioSource = null;

        void kinectSensorChooser1_KinectSensorChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            KinectSensor oldSensor = (KinectSensor)e.OldValue;
            StopKinect(oldSensor);
            KinectSensor newSensor = (KinectSensor)e.NewValue;
            newSensor.ColorStream.Enable();
            newSensor.DepthStream.Enable();
            newSensor.SkeletonStream.Enable();
            newSensor.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(_sensor_AllFramesReady);
            try
            {
                newSensor.Start();
            }
            catch (System.IO.IOException)
            {
                kinectSensorChooser1.AppConflictOccurred();
            }

            audioSource = newSensor.AudioSource;
            audioSource.SoundSourceAngleChanged += new EventHandler<SoundSourceAngleChangedEventArgs>(audioSource_SoundSourceAngleChanged);
            audioSource.Start();

        }

        void audioSource_SoundSourceAngleChanged(object sender, SoundSourceAngleChangedEventArgs e)
        {
            //label2.Content = audioSource.SoundSourceAngle;
        }

        double distanceSquared(SkeletonPoint p1, SkeletonPoint p2)
        {
            return Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2) + Math.Pow(p1.Z - p2.Z, 2);
        }

        void _sensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
            {
                if (depthFrame == null)
                {
                    return;
                }
                byte[] pixels = GenerateColoredBytes(depthFrame);
                int stride = depthFrame.Width * 4;
                image1.Source = BitmapSource.Create(depthFrame.Width, depthFrame.Height,
                    96, 96, PixelFormats.Bgr32, null, pixels, stride);
            }

            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame == null)
                {
                    return;
                }
                Skeleton[] skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                skeletonFrame.CopySkeletonDataTo(skeletons);
                label1.Content = "";
                int skeletonsFound = 0;
                double minDistance = Double.MaxValue;
                foreach (Skeleton skeleton in skeletons)
                {
                    if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
                    {
                        skeletonsFound++;
                        SkeletonPoint homePoint = new SkeletonPoint();
                        homePoint.X = 0;
                        homePoint.Y = 0;
                        homePoint.Z = 0;
                        if (distanceSquared(skeleton.Position, homePoint) < minDistance)
                        {
                            minDistance = distanceSquared(skeleton.Position, homePoint);
                        }

                    }
                    label1.Content += pointString(skeleton.Position);
                    //label1.Content += skeleton.TrackingState.ToString();
                    label1.Content += "\n";
                }
                numPeople = skeletonsFound;
                distance = minDistance;

                if (serialPort != null && serialPort.IsOpen)
                {
                    if (skeletonsFound == 0 && prevState != State.NO_PEOPLE)
                    {
                        serialPort.WriteLine(Convert.ToString(skeletonsFound) + ",close");
                        prevState = State.NO_PEOPLE;
                        label2.Content = "No people";
                    }
                    else if (skeletonsFound == 1 && minDistance < 2 && prevState != State.ONE_PERSON_CLOSE)
                    {
                        serialPort.WriteLine(Convert.ToString(skeletonsFound) + ",close");
                        prevState = State.ONE_PERSON_CLOSE;
                        label2.Content = "One person close";
                    }
                    else if (skeletonsFound == 1 && minDistance >= 2 && prevState != State.ONE_PERSON_FAR)
                    {
                        serialPort.WriteLine(Convert.ToString(skeletonsFound) + ",far");
                        prevState = State.ONE_PERSON_FAR;
                        label2.Content = "One person far";
                    }
                    else if (skeletonsFound > 1 && minDistance < 2 && prevState != State.MANY_CLOSE)
                    {
                        serialPort.WriteLine(Convert.ToString(skeletonsFound) + ",close");
                        prevState = State.MANY_CLOSE;
                        label2.Content = "Many close";
                    }
                    else if (skeletonsFound > 1 && minDistance >= 2 && prevState != State.MANY_FAR)
                    {
                        serialPort.WriteLine(Convert.ToString(skeletonsFound) + ",far");
                        prevState = State.MANY_FAR;
                        label2.Content = "Many far";
                    }
                }
            }
        }

        String pointString(SkeletonPoint pt)
        {
            return "(" + pt.X.ToString() + ", " + pt.Y.ToString() + ", " + pt.Z.ToString() + ")";
        }

        private byte[] GenerateColoredBytes(DepthImageFrame depthFrame)
        {
            short[] rawDepthData = new short[depthFrame.PixelDataLength];
            depthFrame.CopyPixelDataTo(rawDepthData);

            byte[] pixels = new byte[depthFrame.Height * depthFrame.Width * 4];

            const int BlueIndex = 0;

            for (int depthIndex = 0, colorIndex = 0;
                depthIndex < rawDepthData.Length && colorIndex < pixels.Length;
                depthIndex++, colorIndex += 4)
            {
                int player = rawDepthData[depthIndex] & DepthImageFrame.PlayerIndexBitmask;
                int depth = rawDepthData[depthIndex] >> DepthImageFrame.PlayerIndexBitmaskWidth;

                pixels[colorIndex + BlueIndex] = depthToByte(depth, depthFrame.MaxDepth);
            }

            return pixels;
        }

        byte depthToByte(int depth, int maxDepth)
        {

            if (depth <= 10)
            {
                depth = 10;
            }
            return (byte)((depth * 255) / maxDepth);
        }

        void StopKinect(KinectSensor sensor)
        {
            if (sensor != null)
            {
                sensor.Stop();
                sensor.AudioSource.Stop();
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            StopKinect(kinectSensorChooser1.Kinect);
            if (audioSource != null)
            {
                audioSource.Stop();
            }
            closeOpenedSerialPort();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        private void openSerialPort(String portName, int baudRate)
        {
            serialPort = new SerialPort();
            serialPort.PortName = portName;
            serialPort.BaudRate = 9600;
            serialPort.DtrEnable = true;
            //serialPort.WriteTimeout = 50;
            serialPort.DataReceived += new SerialDataReceivedEventHandler(serialPort_DataReceived);
            serialPort.Open();
        }

        void serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            //static int count = 0; 
            String cmd = serialPort.ReadLine();
            //count++;
            //label3.Content = Convert.ToString(count);
            /*if (serialPort != null && serialPort.IsOpen)
            {
                if (distance < 1)
                {
                    serialPort.WriteLine(Convert.ToString(numPeople) + ",close");
                }
                else
                {
                    serialPort.WriteLine(Convert.ToString(numPeople) + ",far");
                }
            }*/
        }

        private void closeOpenedSerialPort()
        {
            if (serialPort != null && serialPort.IsOpen)
            {
                serialPort.Close();
            }
        }

        private void comboBox1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            closeOpenedSerialPort();
            openSerialPort(comboBox1.SelectedItem.ToString(), 9600);
        }
    }
}
