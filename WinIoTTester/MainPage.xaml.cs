using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace WinIoTTester
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private string _uartBridgeName = "CP2102 USB to UART Bridge Controller";
        private SerialDevice _rfidReader = null;
    
        public MainPage()
        {
            this.InitializeComponent();
            SerialConnection();
        }

        public async void SerialConnection()
        {
            SetStatus("Retrieving RFID Reader through UART Bridge ...");
            string deviceQuery = SerialDevice.GetDeviceSelector();
            var discovered = await DeviceInformation.FindAllAsync(deviceQuery);
            var readerInfo = discovered.Where(x => x.Name == _uartBridgeName).First();
            _rfidReader = await SerialDevice.FromIdAsync(readerInfo.Id);
            _rfidReader.WriteTimeout = TimeSpan.FromMilliseconds(1000);
            _rfidReader.ReadTimeout = TimeSpan.FromMilliseconds(1000);
            _rfidReader.BaudRate = 9600;
            _rfidReader.Parity = SerialParity.None;
            _rfidReader.StopBits = SerialStopBitCount.One;
            _rfidReader.DataBits = 8;
            SetStatus("Reader ready and configured");
        }

        private void SetStatus(string text)
        {
            this.txtStatus.Text = text;
        }

        private async Task<RfidReaderResult> Read()
        {
            RfidReaderResult retvalue = new RfidReaderResult();
            var dataReader = new DataReader(_rfidReader.InputStream);
            try
            {
                SetStatus("Awaiting Data from RFID Reader");
                var numBytesRecvd = await dataReader.LoadAsync(1024);
                retvalue.Result = new byte[numBytesRecvd];
                if (numBytesRecvd > 0)
                {
                    SetStatus("Data successfully read from RFID Reader");
                    dataReader.ReadBytes(retvalue.Result);
                    retvalue.IsSuccessful = true;
                    retvalue.Message = "Data successfully read from RFID Reader";
                }
            }
            catch (Exception ex)
            {
                retvalue.IsSuccessful = false;
                retvalue.Message = ex.Message;
            }
            finally
            {
                if (dataReader != null)
                {
                    dataReader.DetachStream();
                    dataReader = null;
                }
            }
            return retvalue;
        }

        private async Task<RfidReaderResult> Write(byte[] writeBytes)
        {
            var dataWriter = new DataWriter(_rfidReader.OutputStream);
            RfidReaderResult retvalue = new RfidReaderResult();
            try
            {
                //send the message
                SetStatus("Writing command to RFID Reader");
                dataWriter.WriteBytes(writeBytes);
                await dataWriter.StoreAsync();
                retvalue.IsSuccessful = true;
                retvalue.Result = writeBytes;
                retvalue.Message = "Text has been successfully sent";
                SetStatus("Writing of command has been successful");
            }
            catch (Exception ex)
            {
                retvalue.IsSuccessful = false;
                retvalue.Message = ex.Message;
            }
            finally
            {
                if (dataWriter != null)
                {
                    dataWriter.DetachStream();
                    dataWriter = null;
                }
            }
            return retvalue;
        }

        internal class RfidReaderResult
        {
            public bool IsSuccessful { get; set; }
            public byte[] Result { get; set; }
            public string Message { get; set; }

        }

        private async void btnSendCommand_Click(object sender, RoutedEventArgs e)
        {
            btnSendCommand.IsEnabled = false;

                      
            //split input into hex bytes, separated by spaces
            string[] tokens = txtCommand.Text.Trim().Split(' ');
            if (tokens.Length > 0)
            {
                byte[] writeArray = new byte[tokens.Length];
                for (int i = 0; i < tokens.Length; i++)
                {
                    int value = Convert.ToInt32(tokens[i], 16);
                    writeArray[i] = (byte)value;
                }
                var writeResult = await Write(writeArray);
                if (writeResult.IsSuccessful)
                {
                    var readResult = await Read();
                    if (readResult.IsSuccessful)
                    {
                        txtReceived.Text = BitConverter.ToString(readResult.Result);
                    }
                    else
                    {
                        SetStatus(readResult.Message);
                    }
                }
                else
                {
                    SetStatus(writeResult.Message);
                }
            } 

            btnSendCommand.IsEnabled = true;

        }
    }
}
