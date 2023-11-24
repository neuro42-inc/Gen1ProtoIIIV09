using System;
using System.Collections.Generic;
using System.Windows;
using System.IO.Ports;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using SharpDX.XInput;
using System.Linq;
using NationalInstruments.DAQmx;
using System.Windows.Forms;
using System.Windows.Controls;
using OxyPlot;
using System.Threading;
using AmRoMessageDialog;
//using OxyPlot;
//using hhdvspkit;
//using NationalInstruments.Restricted;
//using DocumentFormat.OpenXml.Office2016.Drawing.Charts;

namespace n42_Robot_PROTO_III
{
    public partial class MainWindowUpdate : Window, INotifyPropertyChanged /*IDisposable*/
    {

        // Declare Visualization UserControl and x,y,z Values
        Visualization_UserControl Visualization_UserControl;

        object TbX_Val;
        object TbY_Val;
        object TbZ_Val;

        public MainWindowUpdate()
        {
            InitializeComponent();
            CopyResources();
            this.DataContext = this;


            // EventHandler aaa += new System.ComponentModel.CancelEventHandler(this.Window_Closing);

            // Access x,y,z Values from Visualization UserControl
            Visualization_UserControl = new Visualization_UserControl();

            TbX_Val = Visualization_UserControl.TbX_Val;
            TbY_Val = Visualization_UserControl.TbY_Val;
            TbZ_Val = Visualization_UserControl.TbZ_Val;
        }

        //-------------------------------------------------------------------------------------------------------------
        // *** INITIALIZE PRIVATE FIELDS ***
        //-------------------------------------------------------------------------------------------------------------

        // Robot & Connection Status

        private string _robotstatus = "DISCONNECTED";
        private string _handControllerConnection = "Disconnected";
        private string _megaControllerConnection = "Disconnected";
        private string _leonardoControllerConnection = "Disconnected";
        private string _nIDAQConnection = "Disconnected";

        private string _robotMode = "Manual";
        private string _stopButtonImagePath = "/Resources/StopBtn_Disabled.png";
        private string _startButtonImagePath = "/Resources/StartBtn_Disabled.png";
        private string _homeButtonImagePath = "/Resources/HomeBtn_Disabled.png";
        private string _toggleButtonColor = "#808285";
        private bool _toggleButtonIsEnabled = false;
        private int Btn_Connect_Count = 0;
        private bool _isHomeReached = false;

        private bool MoCo_Connected;
        private bool Leo_Connected;
        private bool HC_Connected;
        private bool NIDAQ_Connected;
        private string MoCo_PortNumber;
        private string Leo_PortNumber;
        private bool Index_Stop = false;

        // Controller & Robot Position
        private double _dof1_Angle = 00.0;
        private double _dof2_Angle = 00.0;

        private double _xvPointer;
        private double _yvPointer;
        private double _xfPointer;
        private double _yfPointer;
        private string _potvalue;

        private Controller _controller;
        private double _rightThumbstick;
        public Gamepad gamepad;
        public int LVY;
        public int RVX;
        public GamepadButtonFlags Y_Button;
        public string targetPoint = "";

        // Number of pulse needed to reach Point A and B
        //public int POINT_A_DOF1 = 3000; 
        //public int POINT_A_DOF2 = 1350;
        public int POINT_B_DOF1 = 1500;
        public int POINT_B_DOF2 = 675;

        public int POINT_A_DOF1 = 300;
        public int POINT_A_DOF2 = 300;

        public int pointA_dof1_counter = 0;
        public int pointA_dof2_counter = 0;
        public int pointB_dof1_counter = 0;
        public int pointB_dof2_counter = 0;
        

        // Data & Port
        //public event SerialDataReceivedEventHandler DataReceived;
        //NationalInstruments.DAQmx.Task DataReadTask = new NationalInstruments.DAQmx.Task();
        private Task ReadNITask;
        private static SerialPort sp_L = new SerialPort();
        private static SerialPort sp_R = new SerialPort();

        private static SerialPort virtual_COM2 = new SerialPort();
        private static SerialPort virtual_COM6 = new SerialPort();


        //public ConcurrentQueue<char> serialDataQueue = new ConcurrentQueue<char>();
        //private string sensor;

        // Pulse
        private bool current_Pulse_M1;
        private bool current_Pulse_M2;
        private bool last_Pulse_M1;
        private bool last_Pulse_M2;
        private DigitalSingleChannelReader last_Pulse;
        private DigitalSingleChannelReader current_Pulse;
        // Last pulse
        bool[] sensorL_Pulse_array;
        // Current pulse
        bool[] sensorC_Pulse_array;
        XBoxController controller = new XBoxController();

        // Timer & Others 
        //private const double Rate = 50000;
        //private const int NumberOfSamples = 20000;
        //private const bool V = true;

        private string ResourcePath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        private string virtual_input;

        public System.Windows.Forms.Timer runTimer = new System.Windows.Forms.Timer();




        // ############ Switch motor bool ################
        private int switch_confirm = 0;



        // Controller & Robot Position
        public XBox_XInputController XBox_connected { get; set; }
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


        public RelayCommand StopCommand { get; set; }
        public bool IsReading { get; set; } = false;

        public int BYTES_MAX { get; private set; }
        public Func<double, string> Formatter { get; set; }

        //--------------------------------------------------------------------------------------------------------------
        // Windows  Closing
        //--------------------------------------------------------------------------------------------------------------
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            
            e.Cancel = true;
            AmRoMessageBox _messageBox = new AmRoMessageBox
            {
                Background = "#333",
                TextColor = "#fff",
                RippleEffectColor = "#000",
                ClickEffectColor = "#1F2023",
                ShowMessageWithEffect = true,
                EffectArea = this,
                ParentWindow = this,
                IconColor = "#3399ff",
                //Direction = FlowDirection.LeftToRight,
                ButtonsText = new AmRoMessageBoxButtonsText
                {
                    Yes = "Yes",
                    No = "No",
                }
            };

            if (_messageBox.Show("Are You Sure You Want To Exit?", "", AmRoMessageBoxButton.OkCancel, AmRoMessageBoxIcon.Warring) == AmRoMessageBoxResult.Ok)
            {
                sp_R.Close();
                sp_L.Close();
                Environment.Exit(0);
            }
        }

            //e.Cancel = true;
            //MessageBoxImage icon = MessageBoxImage.Warning;
            //if (System.Windows.MessageBox.Show("Are You Sure You Want To Exit?", "", MessageBoxButton.OKCancel, icon) == MessageBoxResult.OK)
            //{
            //    try
            //    {
            //        this.Btn_Stop_Clicked = true;
            //        this.Btn_Connect_Clicked = false;
            //        this.Btn_Start_Clicked = false;

            //        StartButtonImagePath = "/Resources/StartBtn_Disabled.png";
            //        Btn_Stop.Cursor = System.Windows.Input.Cursors.Hand;

            //        RobotStatus = "STAND BY";

            //        this.Index_Stop = true;
            //        //DataReadTask.Stop();
            //        //DataReadTask.Dispose();
            //        //ReadNITask.Dispose();

            //        sp_R.Close();
            //        sp_L.Close();

            //        Environment.Exit(0);
            //    }
            //    catch
            //    {
            //        System.Windows.MessageBox.Show("Error: Btn_Stop_Click");
            //    }


        

        

        //-------------------------------------------------------------------------------------------------------------
        // *** Connect To Hardware / Stand-alone Mode  ***
        //-------------------------------------------------------------------------------------------------------------
        private void Btn_Connect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Btn_Connect_Clicked = true;
                this.Btn_Start_Clicked = false;
                this.MoCo_Connected = false;
                this.Leo_Connected = false;
                this.HC_Connected = false;
                this.NIDAQ_Connected = false;

                Connection_Establish();

                if (MoCo_Connected && HC_Connected && Leo_Connected && NIDAQ_Connected)
                {
                    // If all connections succeed, open serial ports for auduino
                    sp_R.PortName = MoCo_PortNumber;
                    sp_R.BaudRate = 57600;

                    sp_L.PortName = Leo_PortNumber;
                    sp_L.BaudRate = 57600;

                    // Enable toggle button & set robot status to "STAND BY"                   
                    ToggleButtonIsEnabled = true;
                    ToggleButtonColor = "#7FFFD4";

                    RobotStatus = "STAND BY";

                    // Enable Manual Mode
                    if (RobotMode == "Manual")
                    {
                        IsReading = true;
                        Index_Stop = false;
                        this.Btn_Start.IsEnabled = true;
                        this.Btn_Home.IsEnabled = false;

                        StartButtonImagePath = "/Resources/StartBtn.png";
                        Btn_Start.Cursor = System.Windows.Input.Cursors.Hand;
                        HomeButtonImagePath = "/Resources/HomeBtn_Disabled.png";
                        
                        XBox_Position();
                    }
                    // Enable Auto Mode
                    else
                    {
                        this.Btn_Start.IsEnabled = false;
                        this.Btn_Stop.IsEnabled = false;
                        this.Btn_Home.IsEnabled = true;

                        HomeButtonImagePath = "/Resources/HomeBtn.png";
                        Btn_Home.Cursor = System.Windows.Input.Cursors.Hand;
                    }                   
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
                HandControllerConnection = "Connected";
                Debug.WriteLine("XBox controller connected!!");
            }
            else
            {
                HandControllerConnection = "Disconnected";
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
                        MegaControllerConnection = "Connected";
                        Debug.WriteLine("Mega controller connected!!");
                    }
                }
                if (MoCo_Connected is false)
                {
                    MegaControllerConnection = "Disonnected";
                    MessageBoxImage icon = MessageBoxImage.Exclamation;
                    System.Windows.MessageBox.Show("Error! Mega Controller Not Found", "Motion Controller Error!", MessageBoxButton.OKCancel, icon);
                }

                /// <summary>
                /// Connecting to the NI DAQ
                /// </summary>
                /// 

                // Create the reader
                // Reading the last data from the created channel
                ReadNITask = new Task();
                ReadNITask.DIChannels.CreateChannel("Dev1/Port2/line0:7", "", ChannelLineGrouping.OneChannelForAllLines);
                last_Pulse = new DigitalSingleChannelReader(ReadNITask.Stream);
                sensorL_Pulse_array = last_Pulse.ReadSingleSampleMultiLine();

                string[] devs = DaqSystem.Local.Devices;
                if (devs.Contains("Dev1") || devs.Contains("Dev2") || devs.Contains("Dev3"))
                {
                    NIDAQ_Connected = true;
                    NIDAQConnection = "Connected";
                    Debug.WriteLine("NI DAQ connected!!");

                    // Create a channel for each line P2.0 - P2.7 --> here lines 0 to 7
                    //DataReadTask.DIChannels.CreateChannel("Dev1/Port2/line0:7", "", ChannelLineGrouping.OneChannelForAllLines);
                    //Debug.WriteLine("Channel created");
                }
                else
                {
                    NIDAQ_Connected = false;
                    NIDAQConnection = "Disconnected";
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
                        LeonardoControllerConnection = "Connected";
                        Debug.WriteLine("Leonardo controller connected!!");
                    }
                }
                if (Leo_Connected is false)
                {
                    LeonardoControllerConnection = "Disconnected";
                    MessageBoxImage icon = MessageBoxImage.Exclamation;
                    System.Windows.MessageBox.Show("Error! Leonardo Controller Not Found", "Motion Controller Error!", MessageBoxButton.OKCancel, icon);
                }
            }
        }

        //-------------------------------------------------------------------------------------------------------------
        // *** Read XBox Position: DOF_1 and DOF_2 ***
        //-------------------------------------------------------------------------------------------------------------
        private void XBox_Position()
        {
            try
            {
                
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

            }

            catch
            {
                if (!IsReading)
                {
                    System.Windows.MessageBox.Show("Error in Reading DOF_1 and DOF_2 Positions");
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
                this.Btn_Start_Clicked = true;
                this.Btn_Stop.IsEnabled = true;

                StartButtonImagePath = "/Resources/StartBtn.png";
                Btn_Start.Cursor = System.Windows.Input.Cursors.Hand;

                StopButtonImagePath = "/Resources/StopBtn.png";
                Btn_Stop.Cursor = System.Windows.Input.Cursors.Hand;

                RobotStatus = "RUNNING";
                // Calling the NI DAQ DI and read the values oif the optical sensors
                if (!sp_R.IsOpen)
                {
                    sp_R.Open();
                }
                if (!sp_L.IsOpen)
                {
                    sp_L.Open();
                }
                //Run_PneuStepper_Motors();

                StartTimer();

                //// Create a channel for each line P2.0 - P2.7 --> here lines 0 to 7
                //DataReadTask.DIChannels.CreateChannel("Dev1/Port2/line0:7", "", ChannelLineGrouping.OneChannelForAllLines);
                //Debug.WriteLine("Channel created");


                // Create the reader
                // Reading the last data from the created channel
                //last_Pulse = new DigitalSingleChannelReader(DataReadTask.Stream);
                //sensorL_Pulse_array = last_Pulse.ReadSingleSampleMultiLine();
                //Debug.WriteLine(" last_Pulse = " + last_Pulse);
                //Debug.WriteLine("sensorL_Pulse_array = " + sensorL_Pulse_array);



            }
            catch (Exception ex)
            {
                //System.Windows.MessageBox.Show("Error: Btn_Start_Click");
                Debug.WriteLine("Error: Btn_Start_Click " + ex.Message);
                //ReadNITask.Dispose();

            }
        }

        //--------------------------------------------------------------------------------------------------------------------------
        // ****  Toggle Button Check   ***  
        //--------------------------------------------------------------------------------------------------------------------------
        public void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            RobotMode = "Auto";

            this.Btn_Start.IsEnabled = false;
            StartButtonImagePath = "/Resources/StartBtn_Disabled.png";

            this.Btn_Stop.IsEnabled= false;
            StopButtonImagePath = "/Resources/StopBtn_Disabled.png";

            this.Btn_Home.IsEnabled = true;
            HomeButtonImagePath = "/Resources/HomeBtn.png";
            Btn_Home.Cursor = System.Windows.Input.Cursors.Hand;
 
        }

        public void ToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            RobotMode = "Manual";

            this.Btn_Start.IsEnabled = true;
            StartButtonImagePath = "/Resources/StartBtn.png";
            Btn_Start.Cursor = System.Windows.Input.Cursors.Hand;

            //this.Btn_Stop.IsEnabled = false;
            //StopButtonImagePath = "/Resources/StopBtn_Disabled.png";

            this.Btn_Home.IsEnabled = false;
            HomeButtonImagePath = "/Resources/HomeBtn_Disabled.png";
        }

        //--------------------------------------------------------------------------------------------------------------------------
        // ****  Start Timer   ***  
        //--------------------------------------------------------------------------------------------------------------------------
        public void StartTimer()
        {
            _controller = new Controller(UserIndex.One);

            sp_R.Write("<0>");
            sp_L.Write("<0>");
            if (runTimer != null)
            {
                runTimer.Stop();
                runTimer = null;
            }

            runTimer = new System.Windows.Forms.Timer();
            runTimer.Interval = 10;
            runTimer.Tick += new EventHandler(LoopTimer_Tick);
            //runTimer.Elapsed += _timer_Tick;
            runTimer.Start();

        }

        //--------------------------------------------------------------------------------------------------------------------------
        // ****  Event Timer Tick  ***  
        //--------------------------------------------------------------------------------------------------------------------------
        private void LoopTimer_Tick(object sender, EventArgs e)
        {
            ReadWrite_Data();
        }
        //--------------------------------------------------------------------------------------------------------------------------
        // ****  Timer Tick  ***  
        //--------------------------------------------------------------------------------------------------------------------------
        //private void _timer_Tick(object s, EventArgs e)
        //{
        //    DataReadWrite();
        //}

        //--------------------------------------------------------------------------------------------------------------------------
        // ****  Data read from the Joystick, data read from the NI DAQ, and data write to the serial port (motion controllers)  ***  
        //--------------------------------------------------------------------------------------------------------------------------
        private void ReadWrite_Data()
        {
            try
            {

                current_Pulse = new DigitalSingleChannelReader(ReadNITask.Stream);
                sensorC_Pulse_array = current_Pulse.ReadSingleSampleMultiLine();


                if (RobotMode == "Manual")
                {
                    _controller = new Controller(UserIndex.One);
                    var state = _controller.GetState();


                    LVY = state.Gamepad.LeftThumbY;
                    RVX = state.Gamepad.RightThumbX;

                    if (sensorC_Pulse_array[4] != sensorL_Pulse_array[4] && RVX != 0)
                    {
                        if (RVX > 0)
                        {
                            _dof1_Angle += 0.07;
                        }
                        else
                        {
                            _dof1_Angle -= 0.07;
                        }
                        DOF1_Angle = Convert.ToString(_dof1_Angle);
                    }
                    if (sensorC_Pulse_array[6] != sensorL_Pulse_array[6] && LVY != 0)
                    {
                        if (LVY > 0)
                        {
                            _dof2_Angle += 0.07;
                        }
                        else
                        {
                            _dof2_Angle -= 0.07;
                        }
                        DOF2_Angle = Convert.ToString(_dof2_Angle);
                    }

                    if (Btn_Start_Clicked) // we're in manual
                    {
                        string array_R = "<" + Convert.ToString(RVX) + ">";
                        string array_L = "<" + Convert.ToString(LVY) + ">";
                        const string zero_input = "<0>";
                        string inputVal = zero_input;
                        // right joystick
                        if (RVX > 0) // moving clockwise
                        {
                            inputVal = !sensorC_Pulse_array[1] ? array_R : zero_input;

                        }
                        else if (RVX < 0) // moving counter-clockwise
                        {
                            inputVal = !sensorC_Pulse_array[0] ? array_R : zero_input;
                        }
                        sp_R.Write(inputVal);
                        inputVal = zero_input;

                        // left joystick
                        if (LVY > 0) // moving forward
                        {
                            inputVal = !sensorC_Pulse_array[3] ? array_L : zero_input;
                        }
                        else if (LVY < 0)
                        {
                            inputVal = !sensorC_Pulse_array[2] ? array_L : zero_input;
                        }

                        sp_L.Write(inputVal);


                    }

                }
                //Y_Button = state.Gamepad.Buttons;

                // Create the reader
                // Reading the current data from the created channel
                //ReadNITask = new NationalInstruments.DAQmx.Task();
                //ReadNITask.DIChannels.CreateChannel("Dev1/Port2/line0:7", "", ChannelLineGrouping.OneChannelForAllLines);


                //Debug.WriteLine("current pulse: " + current_Pulse.ToString());
                //Debug.WriteLine("sensorC pulse array: " + (sensorC_Pulse_array != null ? string.Join(",", sensorC_Pulse_array) : "null"));
                //Debug.WriteLine("sensorL pulse array: " + (sensorL_Pulse_array != null ? string.Join(",", sensorL_Pulse_array) : "null"));




                // Write XBox position to Arduino board

                // Send the robot to Home position

                if (RobotMode == "Auto")
                { 
                    if (Btn_Home_Clicked && !Btn_GoToPosition_Clicked) // we're in auto
                    {
                    
                        bool switch_motor = false; // switching from rotation to pitch
                        Btn_GoToPosition_Clicked= false;

                        IsHomeReached = sensorC_Pulse_array[0] && sensorC_Pulse_array[2];
                        if (IsHomeReached)
                        {
                            sp_R.Write("<0>");
                            sp_L.Write("<0>");
                            //sp_R.Write("<0>");
                            //sp_L.Write("<0>");
                            //sp_R.Write("<0>");
                            //sp_L.Write("<0>");
                            sp_R.Close();
                            sp_L.Close();
                            runTimer.Stop();
                            RobotStatus = "HOME";
                            Thread.Sleep(500);
                            return;
                        }
                        else if (sensorC_Pulse_array[0] && !sensorC_Pulse_array[2])
                        {
                            sp_R.Write("<0>");
                            string max_speed = "<" + Convert.ToString(-32000) + ">";
                            sp_L.Write(max_speed);

                            //switch_motor = !switch_motor;
                            //switch_confirm += 1;
                        }

                        else if (!sensorC_Pulse_array[0] && sensorC_Pulse_array[2])
                        {
                            sp_L.Write("<0>");
                            string max_speed = "<" + Convert.ToString(-32000) + ">";
                            sp_R.Write(max_speed);

                        }

                        else
                        {
                            string max_speed = "<" + Convert.ToString(-32000) + ">";
                            sp_R.Write(max_speed);

                            switch_motor = !switch_motor;
                            switch_confirm += 1;

                            if (switch_confirm == 1)
                            {
                                sp_R.Write("<0>");
                                Thread.Sleep(3000);
                            }

                        }

                    }


                    // Send the robot to predefined target Points (A/B)
                    if (Btn_GoToPosition_Clicked)
                    {
                        Btn_Home_Clicked = false;
                        bool switch_motor = false;
                        if (pointA_dof1_counter == POINT_A_DOF1 && pointA_dof2_counter == POINT_A_DOF2)
                        {
                            sp_R.Write("<0>");
                            sp_L.Write("<0>");
                            sp_R.Close();
                            sp_L.Close();
                            runTimer.Stop();
                            RobotStatus = "Target A Reached";
                            return;
                        }

                       
                        // DOF 1 Movement to Point A
                        
                        else if (pointA_dof1_counter < POINT_A_DOF1)
                        {
                            sp_L.Write("<0>");
                            sp_R.Write("<" + Convert.ToString(32000) + ">");
                            pointA_dof1_counter++;

                        }

                        else if(pointA_dof2_counter < POINT_A_DOF2 && pointA_dof1_counter == POINT_A_DOF1)
                        {
                            
                            sp_L.Write("<" + Convert.ToString(32000) + ">");
                            pointA_dof2_counter++;

                            switch_motor = !switch_motor;
                            switch_confirm += 1;

    
                            if (switch_confirm == 1)
                            {
                                sp_R.Write("<0>");
                                Thread.Sleep(10);
                            }
                        }
                        //else
                        //{
                        //    switch_motor = !switch_motor;
                        //    switch_confirm += 1;

                        //}

                        //if (switch_confirm == 1)
                        //{
                        //    sp_R.Write("<0>");
                        //    Thread.Sleep(3000);
                        //}

                        //if (switch_motor)
                        //{
                        //    // Please add a timer delay here to switch control
                        //    // Thread.Sleep(500);

                        //    if (pointA_dof2_counter < POINT_A_DOF2)
                        //    {
                        //        sp_L.Write("<" + Convert.ToString(32500) + ">");
                        //        pointA_dof2_counter++;
                        //    }
                        //    else
                        //    {
                        //        sp_L.Write("<0>");
                        //    }
                        //}


                        ////if (pointA_dof1_counter != POINT_A_DOF1)
                        ////{
                        ////    sp_R.Write("<" + Convert.ToString(32500) + ">");
                        ////    pointA_dof1_counter++;
                        ////}
                        ////sp_R.Write("<0>");

                        ////// DOF 2 Movement to Point A
                        ////if (pointA_dof2_counter != POINT_A_DOF2)
                        ////{
                        ////    sp_L.Write("<" + Convert.ToString(32500) + ">");
                        ////    pointA_dof2_counter++;
                        ////}
                        ////sp_L.Write("<0>");

                        //}
                        //else if (targetPoint == "Target B (188.3, -18.1, -38.6)")
                        //{


                        //    bool switch_motor = false;
                        //    if (pointB_dof1_counter != POINT_B_DOF1 && !switch_motor)
                        //    {
                        //        sp_R.Write("<" + Convert.ToString(32500) + ">");
                        //        pointB_dof1_counter++;
                        //    }
                        //    else
                        //    {
                        //        switch_motor = !switch_motor;
                        //        switch_confirm += 1;

                        //    }

                        //    if (switch_confirm == 1)
                        //    {
                        //        sp_R.Write("<0>");
                        //        Thread.Sleep(3000);
                        //    }

                        //    if (switch_motor)
                        //    {

                        //        if (pointB_dof2_counter != POINT_B_DOF2)
                        //        {
                        //            sp_L.Write("<" + Convert.ToString(32500) + ">");
                        //            pointB_dof2_counter++;
                        //        }
                        //        else
                        //        {
                        //            sp_R.Write("<0>");
                        //            sp_L.Write("<0>");
                        //        }
                        //    }


                        //// DOF 1 Movement to Point B
                        //if (pointB_dof1_counter != POINT_B_DOF1)
                        //{
                        //    sp_R.Write("<" + Convert.ToString(-32500) + ">");
                        //    pointB_dof1_counter++;
                        //}
                        //sp_R.Write("<0>");
                        //// DOF 2 Movement to Point B
                        //if (pointB_dof2_counter != POINT_B_DOF2)
                        //{
                        //    sp_L.Write("<" + Convert.ToString(-32500) + ">");
                        //    pointB_dof2_counter++;
                        //}
                        //sp_L.Write("<0>");



                    }
                }
            }

            catch (Exception ex)
            {
                //DataReadTask.Dispose();
                //ReadNITask.Dispose();
                Debug.WriteLine("Error: Display Controller/Sensor Information. Exception: " + ex.Message);
            }
        }

        

        //-------------------------------------------------------------------------------------------------------------
        // *** Read Optical Encoders - NI DAQ DIs ***
        //-------------------------------------------------------------------------------------------------------------
        //private void Run_PneuStepper_Motors()
        //{
        //    try
        //    {
        //        if (IsReading)
        //        {
        //                string array_R = "<" + Convert.ToString(RVX) + ">";
        //                string array_L = "<" + Convert.ToString(LVY) + ">";
        //                Debug.WriteLine("array_R and array_L : " + RVX + LVY);
        //                string arrayStr_R = string.Join("", array_R);
        //                string arrayStr_L = string.Join("", array_L);

        //                //if (!sp_R.IsOpen)
        //                //{
        //                //    sp_R.Open();
        //                //}
        //                //if (!sp_L.IsOpen)
        //                //{
        //                //    sp_L.Open();
        //                //}

        //                sp_R.Write(array_R);
        //                sp_L.Write(array_L);

        //        }
        //    }

        //    catch
        //    {
        //        if (IsReading)
        //        {
        //            System.Windows.MessageBox.Show("Run_PneuStepper_Motors - Error");
        //        }
        //    }
        //}

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
                this.Btn_Home_Clicked = false;
                this.Btn_GoToPosition_Clicked = false;

                StartButtonImagePath = "/Resources/StartBtn_Disabled.png";
                // HomeButtonImagePath = "/Resources/HomeBtn_Disabled.png";
                Btn_Stop.Cursor = System.Windows.Input.Cursors.Hand;

                RobotStatus = "STAND BY";

                this.Index_Stop = true;
                //DataReadTask.Stop();
                //DataReadTask.Dispose();
                //ReadNITask.Dispose();

                //sp_R.Write("<0>");
                //sp_L.Write("<0>");
                sp_R.Close();
                sp_L.Close();
                runTimer.Stop();

            }
            catch
            {
                System.Windows.MessageBox.Show("Error: Btn_Stop_Click");
            }
        }

        //-------------------------------------------------------------------------------------------------------------
        // *** Btn_Home ***
        //-------------------------------------------------------------------------------------------------------------
        private void Btn_Home_Click(object sender, RoutedEventArgs e)
        {
            
            try
            {
                current_Pulse = new DigitalSingleChannelReader(ReadNITask.Stream);
                sensorC_Pulse_array = current_Pulse.ReadSingleSampleMultiLine();

                this.Btn_Home_Clicked = true;
                this.Btn_GoToPosition_Clicked = false;
                this.Btn_Stop.IsEnabled = true;
                this.switch_confirm = 0;
                this.Btn_Stop.IsEnabled = true;

                StopButtonImagePath = "/Resources/StopBtn.png";
                Btn_Stop.Cursor = System.Windows.Input.Cursors.Hand;

                pointA_dof1_counter = 0;
                pointA_dof2_counter = 0;
                RobotStatus = "RUNNING";

                this.TargetPointComboBox.IsEnabled = true;
                this.Btn_GoToPosition.IsEnabled= true;

                if (!sp_R.IsOpen || !sp_L.IsOpen)
                {
                    sp_R.Open();
                    sp_L.Open();
                }

                StartTimer();

                if (sensorC_Pulse_array[0] == true && sensorC_Pulse_array[2] == true) 
                { 
                    IsHomeReached= true;
                    RobotStatus = "HOME";
                }
            }
            catch
            {
                System.Windows.MessageBox.Show("Error: Btn_Home_Click");
            }

        }


        //-------------------------------------------------------------------------------------------------------------
        // *** Btn_GoToPosition ***
        //-------------------------------------------------------------------------------------------------------------
        private void Btn_GoToPosition_Click(object sender, RoutedEventArgs e)
        {
            // Check if robot is in AUTO mode
            if (RobotMode != "Auto")
            {
                System.Windows.MessageBox.Show("Error: Go To Position Button is only enabled under AUTO mode");
                return;
            }
            else
            {
                try
                {
                    this.Btn_GoToPosition_Clicked = true;
                    this.Btn_Stop.IsEnabled = true;
                    this.Btn_Home_Clicked = false;
                    this.switch_confirm = 0;

                    StopButtonImagePath = "/Resources/StopBtn.png";
                    Btn_Stop.Cursor = System.Windows.Input.Cursors.Hand;

                    RobotStatus = "Running";



                    if (!sp_R.IsOpen)
                    {
                        sp_R.Open();
                    }
                    if (!sp_L.IsOpen)
                    {
                        sp_L.Open();
                    }
                    // Getting the selected target point
                    ComboBoxItem selectedItem = (ComboBoxItem)TargetPointComboBox.SelectedItem;
                    targetPoint = selectedItem.Content.ToString().Trim();
                    
                    StartTimer();


                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error: Go To Target Position. Exception: " + ex.Message);
                }
            }

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

        ////-------------------------------------------------------------------------------------------------------------
        //// *** Combobox_Pos_Vel ***
        ////-------------------------------------------------------------------------------------------------------------
        //private void Combobox_Pos_Vel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    int SelectedSensor = ((System.Windows.Controls.ComboBox)sender).SelectedIndex;
        //    if (SelectedSensor == 0)
        //    {
        //        sensor = "DOF_1"; //Rotational Movement
        //    }
        //    else
        //    {
        //        sensor = "DOF_2"; //Linear Movement converted to rotation by parallelogram RCM 
        //    }
        //}


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

            //TextBox_1.Text = e.ToString();

        }

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        

        //-------------------------------------------------------------------------------------------------------------
        // *** INITIALIZE PUBLIC PROPERTIES TO SAFELY EXPOSE PRIVATE FIELDS ***
        //-------------------------------------------------------------------------------------------------------------

        // Robot & Connection Status
        public string RobotStatus
        {
            get { return _robotstatus; }
            set
            {
                _robotstatus = value;
                OnPropertyChanged(nameof(RobotStatus));
            }
        }

        public string HandControllerConnection
        {
            get { return _handControllerConnection; }
            set
            {
                _handControllerConnection = value;
                OnPropertyChanged(nameof(HandControllerConnection));
            }
        }

        public string MegaControllerConnection
        {
            get { return _megaControllerConnection; }
            set
            {
                _megaControllerConnection = value;
                OnPropertyChanged(nameof(MegaControllerConnection));
            }
        }

        public string LeonardoControllerConnection
        {
            get { return _leonardoControllerConnection; }
            set
            {
                _leonardoControllerConnection = value;
                OnPropertyChanged(nameof(LeonardoControllerConnection));
            }
        }

        public string NIDAQConnection
        {
            get { return _nIDAQConnection; }
            set
            {
                _nIDAQConnection = value;
                OnPropertyChanged(nameof(NIDAQConnection));
            }
        }
        public bool Btn_Stop_Clicked { get; set; } = false;
        public bool Btn_Connect_Clicked { get; set; } = false;
        public bool Btn_Start_Clicked { get; set; } = false;
        public bool Btn_Home_Clicked { get; set; } = false;
        public bool Btn_GoToPosition_Clicked { get; set; } = false;
        public string StartButtonImagePath
        {
            get { return _startButtonImagePath; }
            set
            {
                _startButtonImagePath = value;
                OnPropertyChanged(nameof(StartButtonImagePath));
            }
        }
        public string StopButtonImagePath
        {
            get { return _stopButtonImagePath; }
            set
            {
                _stopButtonImagePath = value;
                OnPropertyChanged(nameof(StopButtonImagePath));
            }
        }
        public string HomeButtonImagePath
        {
            get { return _homeButtonImagePath; }
            set
            {
                _homeButtonImagePath = value;
                OnPropertyChanged(nameof(HomeButtonImagePath));
            }
        }
        public bool ToggleButtonIsEnabled
        {
            get { return _toggleButtonIsEnabled; }
            set
            {
                _toggleButtonIsEnabled = value;
                OnPropertyChanged(nameof(ToggleButtonIsEnabled));
            }

        }

        public string ToggleButtonColor
        { 
            get { return _toggleButtonColor; }
            set
            {
                _toggleButtonColor = value;
                OnPropertyChanged(nameof(ToggleButtonColor));
            }

        }

        public string RobotMode
        {
            get { return _robotMode; }
            set
            {
                _robotMode = value;
                OnPropertyChanged(nameof(RobotMode));
            }
        }

        public bool IsHomeReached
        {
            get { return _isHomeReached; }
            set
            {
                _isHomeReached = value;
                OnPropertyChanged(nameof(IsHomeReached));
            }
        }

    }
}


