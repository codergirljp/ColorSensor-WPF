using System;
using System.Windows;
using System.Windows.Media;
using jp.Comms;

namespace ColorSensor
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        /// <summary>
        /// Serial Port Manager Instance
        /// Provide functionality for handling the send and receive of data 
        /// from a serial port.
        /// </summary>
        SerialPortManager serialPortManager;

        /// <summary>
        /// A ColorSensor message handler
        /// Translates messages received from the Color Sensor into 
        /// R, G, B  and Hex values
        /// </summary>
        ColorSensorProtocol msgHandler = new ColorSensorProtocol();

        byte[] gammaTable = new byte[256];

        /// <summary>
        /// Auto generated Window Init
        /// </summary>
        public Window1()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Delegate to allow updating window objects from external thread
        /// </summary>
        /// <param name="message">A string message from the serial port</param>
        public delegate void UpdateDisplayCallback(string message);

        /// <summary>
        /// Update function 
        /// This is the call back used when serial data is recieved 
        /// The function processes the message and takes care of UI updates
        /// </summary>
        /// <param name="message">A string containing the message from the serial port</param>
        private void Update(string message) 
        {
            // Get the data from the message using the serial port managers 
            // color sensor protocol
            msgHandler.GetData(message);

            if (message.Contains("RGB"))
            {
                LightIsOnValueLabel.Content = msgHandler.LightIsOn.ToString();

                // Update Current Color Rectangle on Screen
                Color _currentColor = new Color();
                _currentColor.A = (byte)255;
                _currentColor.R = (byte)msgHandler.R;
                _currentColor.G = (byte)msgHandler.G;
                _currentColor.B = (byte)msgHandler.B;
                CurrentColorRect.Fill = new SolidColorBrush(_currentColor);
                CurrentColorTextBox.Text = msgHandler.HexColor;

                // Calculate Gamma Corrected Color from Gamma Table
                Color _gammaColor = new Color();
                _gammaColor.A = (byte)255;
                _gammaColor.R = gammaTable[(msgHandler.R > 255 ? 255 : msgHandler.R)];
                _gammaColor.G = gammaTable[(msgHandler.G > 255 ? 255 : msgHandler.G)];
                _gammaColor.B = gammaTable[(msgHandler.B > 255 ? 255 : msgHandler.B)];
                GammColorRect.Fill = new SolidColorBrush(_gammaColor);
                GammaColorTextBox.Text = To2DigitHexString(_gammaColor.R.ToString("X")) + To2DigitHexString(_gammaColor.G.ToString("X")) + To2DigitHexString(_gammaColor.B.ToString("X"));

                // Update comparison color Rectangle on Screen
                Color _compareColor = new Color();

                if (CompareToColorTextBox.Text.Length == 6)
                {
                    _compareColor.A = (byte)(int)255;
                    _compareColor.R = (byte)int.Parse(CompareToColorTextBox.Text.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                    _compareColor.G = (byte)int.Parse(CompareToColorTextBox.Text.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                    _compareColor.B = (byte)int.Parse(CompareToColorTextBox.Text.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                    CompareToColorRect.Fill = new SolidColorBrush(_compareColor);
                }

            }
            else if (message.Length > 4) 
            {
                // send other messages to screen, but not blank lines
                richTextBox1.AppendText(message);
            }

           // Log  Incomming messages if the box is checked
           // and create a local log file of messages
            if ((bool)LogToFileCheckBox.IsChecked)
            {
                string eventMsg = String.Format("{0} - {1}", System.DateTime.Now, message);
                LogEvents(eventMsg);
            }
        }

        /// <summary>
        /// Returns the Baud Rate as an integer. 
        /// </summary>
        /// <returns>integer baud rate from the Baud Rate text box.</returns>
        private int BaudRate()
        {
            int _baudRate = 0;

            try
            {
                _baudRate = System.Convert.ToInt32(BaudRateText.Text);
            }
            catch
            {
                string msg = "ERROR: The BAUD RATE must be an integer.";
                MessageBox.Show(msg);
            }

            return _baudRate;
        }

        /// <summary>
        /// Handles the Start/Stop button click event. 
        /// Starts the Serial port if the button said Start.
        /// Closes the Serial Port if the button said Stop.
        /// </summary>
        /// <param name="sender">The start/stop button.</param>
        /// <param name="e">The button click event args.</param>
        private void OnStartButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (button1.Content.ToString() == "Stop")
                {
                    button1.Content = "Start";

                    if (serialPortManager != null && serialPortManager.IsAvailable())
                    {
                        serialPortManager.CloseSerialPort();
                    }

                    
                }
                else
                {
                    InitGammaTable();

                    int _baudRate = BaudRate();
                    serialPortManager = new SerialPortManager(ComPortComboBox.SelectionBoxItem.ToString(), _baudRate);
                    serialPortManager.UseDataQueue = false;
                    serialPortManager.OnDataReceived += this.serialPort_DataReceivedHandler;
                    button1.Content = "Stop";
                }
            }
            catch (Exception ex)
            {
                richTextBox1.AppendText(ex.Message);
                if (serialPortManager != null && serialPortManager.IsAvailable())
                {
                    serialPortManager.CloseSerialPort();
                }
             
                button1.Content = "Stop";
            }
        }

        /// <summary>
        /// Data Received Handler. 
        /// Subscribes to the Serial Port Receive event and gets called when a message is received.
        /// </summary>
        /// <param name="message">The message received.</param>
        void serialPort_DataReceivedHandler(string message)
        {
            try
            {
                richTextBox1.Dispatcher.Invoke(new UpdateDisplayCallback(this.Update), new object[] { message });
                
            }
            catch (Exception ex1)
            {
                richTextBox1.Dispatcher.Invoke(new UpdateDisplayCallback(this.Update), new object[] { ex1.Message });
            }
        }

        /// <summary>
        /// Take care of some clean up on Window Closing to shut down nicely
        /// </summary>
        /// <param name="sender">The sending object from the window.</param>
        /// <param name="e">The Cancel Event Args from the window closing event.</param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (serialPortManager != null && serialPortManager.IsAvailable())
            {
                serialPortManager.CloseSerialPort();
            }
        }

        /// <summary>
        /// Log Events 
        /// Used to log event messages to a Sensor Events Log text file if uncommented above.
        /// </summary>
        /// <param name="EventMessageText">The event message to log.</param>
        private void LogEvents(string EventMessageText)
        {
            System.IO.FileInfo fi = new System.IO.FileInfo("SensorEventsLog.txt");

            if (fi != null && !fi.Exists)
            {
                System.IO.FileStream fs = fi.Create();
                fs.Close();
                fs.Dispose();
                fs = null;
            }

            System.IO.StreamWriter sw = fi.AppendText();
            sw.WriteLine(EventMessageText);
            sw.Close();
            sw.Dispose();
            sw = null;
        }

        private string To2DigitHexString(string hexValue)
        {
            if (hexValue.Length < 2)
            {
                return "0" + hexValue;
            }
            else
            {
                return hexValue;
            }
        }

        /// <summary>
        /// Gamma Conversion Table from Adafruit example
        /// </summary>
        private void InitGammaTable()
        {
          // thanks PhilB for this gamma table!
          // it helps convert RGB colors to what humans see
          for (int i=0; i<256; i++) 
          {
            double x = i;
            x /= 255;
            x = System.Math.Pow(x, 2.5);
            x *= 255;
            gammaTable[i] = (byte)x;
          }
        }
    }
}
