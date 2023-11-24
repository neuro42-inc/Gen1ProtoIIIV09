using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using System.IO.Ports;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using System.ComponentModel;
using PdfSharp.Pdf;
using PdfSharp.Drawing;
using System.Diagnostics;
using System.Reflection;
using SharpDX.XInput;
using OxyPlot;
using System.Web.UI.WebControls.WebParts;
using Windows.Networking.XboxLive;
using System.Threading.Tasks;
//using static XBoxController;
using DocumentFormat.OpenXml.Drawing;
using System.Linq;
using System.Timers;
using System.Windows.Threading;
using System.Runtime.CompilerServices;
using Windows.ApplicationModel.VoiceCommands;
using System.Collections.Concurrent;
using System.Collections.Concurrent;
using System.Collections.Generic;
using NationalInstruments.DAQmx;
using MaterialDesignThemes.Wpf;
using SharpDX.Direct3D11;
using System.Windows.Forms;
using SharpDX.DirectWrite;
using DocumentFormat.OpenXml.Bibliography;
//using System.Drawing;

namespace n42_Robot_PROTO_III
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>


    public partial class MainWindow : Window, INotifyPropertyChanged /*IDisposable*/
    {

        public MainWindow()
        {
            InitializeComponent();
            CopyResources();
        }


        public RelayCommand StopCommand { get; set; }
        public bool IsReading { get; set; } = false;
        private bool Index_Stop = false;
        public bool Btn_Stop_Clicked { get; set; } = false;
        public bool Btn_Connect_Clicked { get; set; } = false;
        public bool Btn_Start_Clicked { get; set; } = false;
        public int BYTES_MAX { get; private set; }
        public Func<double, string> Formatter { get; set; }

        private double _xvPointer;
        private double _yvPointer;
        private double _xfPointer;
        private double _yfPointer;
        private string _potvalue;
        private bool current_Pulse_M1;
        private bool current_Pulse_M2;
        private bool last_Pulse_M1;
        private bool last_Pulse_M2;
        bool[] sensorL_Pulse_array;
        bool[] sensorC_Pulse_array;
        private double _dof1_Angle = 00.0;
        private double _dof2_Angle = 00.0;

        private bool MoCo_Connected;
        private bool Leo_Connected;
        private bool HC_Connected;
        private bool NIDAQ_Connected;
        private bool LEDStat = false;
        public Gamepad gamepad;
        private Controller _controller;
        
        private DigitalSingleChannelReader last_Pulse;
        private DigitalSingleChannelReader current_Pulse;
        public XBox_XInputController XBox_connected { get; set; }

        private double _rightThumbstick;

        public double RightThumbstick
        {
            get
            {
                return _rightThumbstick;
            }
            set
            {
                if (value == _rightThumbstick) return;
                _rightThumbstick = value;
                OnPropertyChanged();
            }
        }

        public event SerialDataReceivedEventHandler DataReceived;
        private string MoCo_PortNumber;
        private string Leo_PortNumber;
        private static SerialPort sp_R = new SerialPort();
        private static SerialPort sp_L = new SerialPort();

        private const double Rate = 50000;
        private const int NumberOfSamples = 20000;
        private const bool V = true;
        private int Btn_Connect_Count = 0;
        private string ResourcePath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        private string sensor;
        public DispatcherTimer runTimer = new DispatcherTimer();
        public int tickmSec { get; set; }
        public ConcurrentQueue<char> serialDataQueue = new ConcurrentQueue<char>();
        // Creating a task to Read data from NI DAQ
        NationalInstruments.DAQmx.Task DataReadTask = new NationalInstruments.DAQmx.Task();

        //-------------------------------------------------------------------------------------------------------------
        // *** Connect To Hardware / Stand-alone Mode  ***
        //-------------------------------------------------------------------------------------------------------------
        private void Btn_Connect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Btn_Start_Clicked = false;
                this.Btn_Connect_Clicked = true;
                this.MoCo_Connected = false;
                this.Leo_Connected = false;
                this.HC_Connected = false;
                this.NIDAQ_Connected = false;

                Connection_Establish();

                if (MoCo_Connected && HC_Connected && Leo_Connected && NIDAQ_Connected)
                {
                    IsReading = true;
                    Index_Stop = false;
                    this.Btn_Stop.IsEnabled = true;
                    this.Btn_Connect.IsEnabled = false;
                    this.Btn_Start.IsEnabled = true;
                    this.btn_GoToPosition.IsEnabled = true;

                    var brush1 = new ImageBrush();
                    brush1.ImageSource = new BitmapImage(new Uri(ResourcePath + @"\\Resources_N42\\Stop_enabled2.png"));
                    Btn_Stop.Background = brush1;
                    Btn_Stop.Cursor = System.Windows.Input.Cursors.Hand;

                    var brush2 = new ImageBrush();
                    brush2.ImageSource = new BitmapImage(new Uri(ResourcePath + @"\\Resources_N42\\Start_enabled2.png"));
                    Btn_Start.Background = brush2;
                    Btn_Start.Cursor = System.Windows.Input.Cursors.Hand;

                    var brush3 = new ImageBrush();
                    brush3.ImageSource = new BitmapImage(new Uri(ResourcePath + @"\\Resources_N42\\Goto_enabled.png"));
                    btn_GoToPosition.Background = brush3;
                    btn_GoToPosition.Cursor = System.Windows.Input.Cursors.Hand;

                    sp_R.PortName = MoCo_PortNumber;
                    sp_R.BaudRate = 9600;
                    sp_R.Close();

                    sp_L.PortName = Leo_PortNumber;
                    sp_L.BaudRate = 9600;
                    sp_L.Close();

                    XBox_Position();
                }
            }
            catch
            {
                System.Windows.MessageBox.Show("Error: Btn_Connect_Click");
            }
        }

        //-------------------------------------------------------------------------------------------------------------
        // *** Connection_Establish  ***
        //-------------------------------------------------------------------------------------------------------------
        private void Connection_Establish()
        {

            /// <summary>
            /// Connecting to the Hand Controller through XBox_XInputController.cs
            /// </summary>
            XBox_XInputController _xbox_connected = new XBox_XInputController();
            if (_xbox_connected.XBox_connected)
            {
                //_ = MessageBox.Show(string.Format("The XBox controller is connected"));
                HC_Connected = true;
                ConnectedToHC.Visibility = Visibility.Visible;
                PS4_0421.Visibility = Visibility.Visible;
                HC_LED.Opacity = 50;
                HC_LED.Content = new Image()
                {
                    Source = new BitmapImage(new Uri(ResourcePath + @"\\Resources_N42\\Green-Led.png"))
                };
            }
            else
            {
                ConnectedToHC.Visibility = Visibility.Hidden;
                PS4_0421.Visibility = Visibility.Hidden;
                HC_LED.Opacity = 0.2;
                HC_LED.Content = new Image()
                {
                    Source = null
                };
                IsReading = false;
            }


            if (MoCo_Connected && HC_Connected && Leo_Connected)
            {
                this.Btn_Stop.IsEnabled = true;
                this.Btn_Start.IsEnabled = true;
                this.Btn_Connect.IsEnabled = false;

            }

            /// <summary>
            /// Connecting to the Motion Controller (Arduino Mega2560) through COM port
            /// </summary>
            List<string> portname2 = ComPortNames("2341", "0042"); //Mega2560
            //List<string> portname2 = ComPortNames("2341", "003D"); //Due
            if (portname2.Count > 0)
            {
                foreach (String port2 in SerialPort.GetPortNames())
                {
                    if (portname2.Contains(port2))
                    {
                        //_ = MessageBox.Show(string.Format("The Mega Controller is connected to: {0}", port2));
                        MoCo_PortNumber = port2;
                        MoCo_Connected = true;
                        ConnectedToMoCo.Visibility = Visibility.Visible;
                        ATMEGA_2560.Visibility = Visibility.Visible;
                        RB_MoControl.Opacity = 50;
                        RB_MoControl.Content = new Image()
                        {
                            Source = new BitmapImage(new Uri(ResourcePath + @"\\Resources_N42\\Green-Led.png"))
                        };
                    }
                }
                if (MoCo_Connected is false)
                {
                    ConnectedToMoCo.Visibility = Visibility.Hidden;
                    ATMEGA_2560.Visibility = Visibility.Hidden;
                    RB_MoControl.Opacity = 0.2;
                    //_AmRoMsgBox.EffectArea = this;
                    //_AmRoMsgBox.ParentWindow = this;
                    MessageBoxImage icon = MessageBoxImage.Exclamation;
                    System.Windows.MessageBox.Show("Error! Mega Controller Not Found", "Motion Controller Error!", MessageBoxButton.OKCancel, icon);
                }

                // <summary>
                /// Connecting to the NI DAQ
                /// </summary>
                string[] devs = NationalInstruments.DAQmx.DaqSystem.Local.Devices;
                if (devs.Contains("Dev1") || devs.Contains("Dev2") || devs.Contains("Dev3"))
                {
                    NIDAQ_Connected = true;
                    ConnectedToNIDAQ.Visibility = Visibility.Visible;
                    NIUSB6501.Visibility = Visibility.Visible;
                    NIDAQ_LED.Opacity = 50;
                    NIDAQ_LED.Content = new Image()
                    {
                        Source = new BitmapImage(new Uri(ResourcePath + @"\\Resources_N42\\Green-Led.png"))
                    };
                }
                else
                {
                    NIDAQ_Connected = false;
                    ConnectedToNIDAQ.Visibility = Visibility.Hidden;
                    NIUSB6501.Visibility = Visibility.Hidden;
                    NIDAQ_LED.Opacity = 0.2;
                    MessageBoxImage icon = MessageBoxImage.Exclamation;
                    System.Windows.MessageBox.Show("Error! NI USB-6501 Not Found", "NI USB-6501 Scan Error!", MessageBoxButton.OKCancel, icon);
                }
            }


            /// <summary>
            /// Connecting to the Motion Controller (Arduino Leonardo) through COM port
            /// </summary>
            List<string> portname3 = ComPortNames("2341", "8036"); //Leonardo
            if (portname3.Count > 0)
            {
                foreach (String port3 in SerialPort.GetPortNames())
                {
                    if (portname3.Contains(port3))
                    {
                        //_ = MessageBox.Show(string.Format("The Leonardo Controller is connected to: {0}", port3));
                        Leo_PortNumber = port3;
                        Leo_Connected = true;
                        ConnectedToLeo.Visibility = Visibility.Visible;
                        Leonardo.Visibility = Visibility.Visible;
                        RB_LeoControl.Opacity = 50;
                        RB_LeoControl.Content = new Image()
                        {
                            Source = new BitmapImage(new Uri(ResourcePath + @"\\Resources_N42\\Green-Led.png"))
                        };
                    }
                }
                if (Leo_Connected is false)
                {
                    ConnectedToLeo.Visibility = Visibility.Hidden;
                    Leonardo.Visibility = Visibility.Hidden;
                    RB_LeoControl.Opacity = 0.2;
                    MessageBoxImage icon = MessageBoxImage.Exclamation;
                    System.Windows.MessageBox.Show("Error! Leonardo Controller Not Found", "Motion Controller Error!", MessageBoxButton.OKCancel, icon);
                }
            }
        }

        //-------------------------------------------------------------------------------------------------------------
        // *** Btn_Start (Start)***
        //-------------------------------------------------------------------------------------------------------------
        private void Btn_Start_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //ReadDataFromNI();
                this.Btn_Start_Clicked = true;

                // Create a channel for each line P2.0 - P2.7 --> here lines 0 to 7
                DataReadTask.DIChannels.CreateChannel("Dev1/Port2/line0:7", "", ChannelLineGrouping.OneChannelForAllLines);
                Debug.WriteLine("Channel created");


                // Create the reader
                // Reading the last data from the created channel
                last_Pulse = new DigitalSingleChannelReader(DataReadTask.Stream);
                sensorL_Pulse_array = last_Pulse.ReadSingleSampleMultiLine();
 
                sp_R.Open();
                sp_L.Open();
            }
            catch
            {
                System.Windows.MessageBox.Show("Error: Btn_Start_Click");
            }
        }

        //-------------------------------------------------------------------------------------------------------------
        // *** Btn_Stop ***
        //-------------------------------------------------------------------------------------------------------------
        private void Btn_Stop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Btn_Stop_Clicked = true;
                this.Btn_Connect_Clicked = false;
                this.Btn_Start_Clicked = false;

                this.Index_Stop = true;
                DataReadTask.Stop();
                DataReadTask.Dispose();
                sp_R.Close();
                sp_L.Close();
            }
            catch
            {
                System.Windows.MessageBox.Show("Error: Btn_Stop_Click");
            }
        }


        //-------------------------------------------------------------------------------------------------------------
        // *** Read XBox Position: DOF_1 and DOF_2 ***
        //-------------------------------------------------------------------------------------------------------------
        private void XBox_Position()
        {
            try
            {
                XBoxController controller = new XBoxController();

                var guiDisp = System.Windows.Application.Current.Dispatcher;

                //Thumb Positions Left, Right
                controller.RightThumbstick.ValueChanged += (s, e) => guiDisp.Invoke(() =>
                {
                    RightThumbPositionsCircle.Margin = new Thickness(180.0 * e.Value.X, -180.0 * e.Value.Y, 0.0, 20);
                });

                controller.LeftThumbstick.ValueChanged += (s, e) => guiDisp.Invoke(() =>
                {
                    LeftThumbPositionsCircle.Margin = new Thickness(180.0 * e.Value.X, -180.0 * e.Value.Y, 0.0, 20);
                });

                StartTimer();
            }

            catch
            {
                if (!IsReading)
                {
                    System.Windows.MessageBox.Show("Error in Reading DOF_1 and DOF_2 Positions");
                }
            }
        }

        //--------------------------------------------------------------------------------------------------------------------------
        // ****  Start Timer   ***  
        //--------------------------------------------------------------------------------------------------------------------------
        public void StartTimer()
        {
            _controller = new Controller(UserIndex.One);


            runTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(10) };
            runTimer.Tick += _timer_Tick;
            runTimer.Start();
        }

        //--------------------------------------------------------------------------------------------------------------------------
        // ****  Timer Tick  ***  
        //--------------------------------------------------------------------------------------------------------------------------
        private void _timer_Tick(object s, EventArgs e)
        {
            DataReadWrite();
        }

        //--------------------------------------------------------------------------------------------------------------------------
        // ****  Data read from the Joystick, data read from the NI DAQ, and data write to the serial port (motion controllers)  ***  
        //--------------------------------------------------------------------------------------------------------------------------
        private void DataReadWrite()
        { 
            try
            {
                var guiDisp = System.Windows.Application.Current.Dispatcher;
                var state = _controller.GetState();

                guiDisp.Invoke(() =>
                {
                    int LVY = state.Gamepad.LeftThumbY;
                    int RVX = state.Gamepad.RightThumbX;


                    if (Btn_Start_Clicked)
                    {
                        string array_R = "<" + Convert.ToString(RVX) + ">";
                        string array_L = "<" + Convert.ToString(LVY) + ">";
                        string arrayStr_R = string.Join("", array_R);
                        string arrayStr_L = string.Join("", array_L);

                        if (!sp_R.IsOpen || !sp_L.IsOpen)
                        {
                            sp_R.Open();
                            sp_L.Open();
                        }
                        sp_R.Write(array_R);
                        sp_L.Write(array_L);
                        Pot_Value = Convert.ToString(RVX) + "," + Convert.ToString(LVY);

                        // Create the reader
                        // Reading the current data from the created channel
                        current_Pulse = new DigitalSingleChannelReader(DataReadTask.Stream);
                        sensorC_Pulse_array = current_Pulse.ReadSingleSampleMultiLine();
                        
                        if (sensorC_Pulse_array[4] != sensorL_Pulse_array[4] && RVX!=0)
                        {
                            if (RVX > 0)
                            {
                               _dof1_Angle += 0.07;
                               DOF1_Angle = Convert.ToString(_dof1_Angle);
                            }
                            else
                            {
                                _dof1_Angle -= 0.07;
                                DOF1_Angle = Convert.ToString(_dof1_Angle);
                            }
                        }
                        if (sensorC_Pulse_array[6] != sensorL_Pulse_array[6] && LVY != 0)
                        {
                            if (LVY > 0)
                            {
                                _dof2_Angle += 0.07;
                                DOF1_Angle = Convert.ToString(_dof1_Angle);
                            }
                            else
                            {
                                _dof1_Angle -= 0.07;
                                DOF1_Angle = Convert.ToString(_dof1_Angle);
                            }
                        }

                    }

                });
            }
            catch (Exception ex)
            {
                DataReadTask.Dispose();
                System.Windows.MessageBox.Show("Error: Display Controller/Sensor Information");
            }
            DataContext = this;
        }

        //-------------------------------------------------------------------------------------------------------------
        // *** COM port Names ***
        //-------------------------------------------------------------------------------------------------------------
        private List<string> ComPortNames(String VID, String PID)
        {
            String pattern = String.Format("^VID_{0}.PID_{1}", VID, PID);
            Regex _rx = new Regex(pattern, RegexOptions.IgnoreCase);
            List<string> comports = new List<string>();
            RegistryKey rk1 = Registry.LocalMachine;
            RegistryKey rk2 = rk1.OpenSubKey("SYSTEM\\CurrentControlSet\\Enum");
            foreach (String s3 in rk2.GetSubKeyNames())
            {
                RegistryKey rk3 = rk2.OpenSubKey(s3);
                foreach (String s in rk3.GetSubKeyNames())
                {
                    if (_rx.Match(s).Success)
                    {
                        RegistryKey rk4 = rk3.OpenSubKey(s);
                        foreach (String s2 in rk4.GetSubKeyNames())
                        {
                            RegistryKey rk5 = rk4.OpenSubKey(s2);
                            RegistryKey rk6 = rk5.OpenSubKey("Device Parameters");
                            comports.Add((string)rk6.GetValue("PortName"));
                        }
                    }
                }
            }
            return comports;
        }

        //-------------------------------------------------------------------------------------------------------------
        // *** Copy Resources to ProgramData ***
        //-------------------------------------------------------------------------------------------------------------
        private void CopyResources()
        {
            var aaa = AssemblyDirectory.Length;
            string destinationFolder = ResourcePath + @"\\Resources_N42\\";
            if (!System.IO.Directory.Exists(destinationFolder))
            {
                //Get the files from the source folder.
                string[] Resource_files = System.IO.Directory.GetFiles(AssemblyDirectory);
                Directory.CreateDirectory(destinationFolder);

                // Copy the files if destination files do not already exist.
                foreach (string _file in Resource_files)
                {
                    string fileName = System.IO.Path.GetFileName(_file);
                    string destFile = System.IO.Path.Combine(destinationFolder, fileName);
                    System.IO.File.Copy(_file, destFile, true);
                }
            }
        }

        //-------------------------------------------------------------------------------------------------------------
        // *** Combobox_Pos_Vel ***
        //-------------------------------------------------------------------------------------------------------------
        private void Combobox_Pos_Vel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int SelectedSensor = ((System.Windows.Controls.ComboBox)sender).SelectedIndex;
            if (SelectedSensor == 0)
            {
                sensor = "DOF_1"; //Rotational Movement
            }
            else
            {
                sensor = "DOF_2"; //Linear Movement converted to rotation by parallelogram RCM 
            }
        }

       
        //-------------------------------------------------------------------------------------------------------------
        // *** Count NonZeros Elements ***
        //-------------------------------------------------------------------------------------------------------------
        private int CountNonZerosElements(double[] array)
        {
            var _numberofpoints = 0;
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] != 0)
                    _numberofpoints++;
            }
            return _numberofpoints;
        }

        //-------------------------------------------------------------------------------------------------------------
        // *** Properties ***
        //-------------------------------------------------------------------------------------------------------------
        public string Pot_Value 
        {
            get { return _potvalue; }
            set
            {
                _potvalue = value;
                OnPropertyChanged(nameof(Pot_Value));
            }
        }

        public string DOF1_Angle
        {
            get { return Convert.ToString(_dof1_Angle); }
            set
            {
                _dof1_Angle = Convert.ToDouble(value);
                OnPropertyChanged(nameof(DOF1_Angle));
            }
        }

        public string DOF2_Angle
        {
            get { return Convert.ToString(_dof2_Angle); }
            set
            {
                _dof2_Angle = Convert.ToDouble(value);
                OnPropertyChanged(nameof(DOF2_Angle));
            }
        }

        public double XVPointer
        {
            get { return _xvPointer; }
            set
            {
                _xvPointer = value;
                OnPropertyChanged(nameof(XVPointer));
            }
        }



        public double XFPointer
        {
            get { return _xfPointer; }
            set
            {
                _xfPointer = value;
                OnPropertyChanged(nameof(XFPointer));
            }
        }
        public double YVPointer
        {
            get { return _yvPointer; }
            set
            {
                _yvPointer = value;
                OnPropertyChanged(nameof(YVPointer));
            }
        }
        public double YFPointer
        {
            get { return _yfPointer; }
            set
            {
                _yfPointer = value;
                OnPropertyChanged(nameof(YFPointer));
            }
        }


        //-------------------------------------------------------------------------------------------------------------
        // *** Assembly Directory Path ***
        //-------------------------------------------------------------------------------------------------------------
        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri_Assembly = new UriBuilder(codeBase);         // Gives the path in URI format
                string Assembly_path = Uri.UnescapeDataString(uri_Assembly.Path);    // removes the "File://" at the beginning
                string result = System.IO.Path.GetDirectoryName(Assembly_path); // Canges the path to normal windows format

                int index = result.LastIndexOf("\\", System.StringComparison.InvariantCulture);
                return $"{result}\\Resources";
            }
        }

        //-------------------------------------------------------------------------------------------------------------
        // *** Events and Event Handlers ***
        //-------------------------------------------------------------------------------------------------------------
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(PropertyChangedEventArgs e)
        {

            TextBox_1.Text = e.ToString();

        }

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        private void Window_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            MessageBoxImage icon = MessageBoxImage.Warning;
            if (System.Windows.MessageBox.Show("Are You Sure You Want To Exit?", "", MessageBoxButton.OKCancel, icon) == MessageBoxResult.OK)
            {

                Environment.Exit(0);
            }

        }

        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            LEDStat = true;
            Auto.Foreground = Brushes.Green;
            Auto.Opacity = 100;
            Manual.Foreground = Brushes.Gray;
            Manual.Opacity = 10;

            var brush = new ImageBrush();
            brush.ImageSource = new BitmapImage(new Uri(ResourcePath + @"\\Resources_N42\\Goto_enabled.png"));
            btn_GoToPosition.IsEnabled = true;
            btn_GoToPosition.Background = brush;
            btn_GoToPosition.Cursor = System.Windows.Input.Cursors.Hand;

            //MainWindow_Visualization window_Vis = new MainWindow_Visualization();
            //window_Vis.Owner= this;
            //window_Vis.Show();
            //RobotVis_Window.Navigate("MainWindowVis.xaml");

        }

        private void ToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            LEDStat = false;
            Manual.Foreground = Brushes.Green;
            Manual.Opacity = 100;
            Auto.Foreground = Brushes.Gray;
            Auto.Opacity = 10;
            var brush = new ImageBrush();
            brush.ImageSource = new BitmapImage(new Uri(ResourcePath + @"\\Resources_N42\\Goto_disabled.png"));
            btn_GoToPosition.IsEnabled= false;
            btn_GoToPosition.Background = brush;
            btn_GoToPosition.Cursor = System.Windows.Input.Cursors.Hand;
        }

        private void GoToPosition_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}


