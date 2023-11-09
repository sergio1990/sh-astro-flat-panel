using ASCOM.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASCOM.SHAstroFlatPanel
{
    internal class DeviceController : IDisposable
    {
        private TraceLogger traceLogger;
        private Serial serialConnection = new Serial();
        private Boolean isConnected;

        private const string DEVICE_GUID = "6C69985D-0974-4599-8367-8628E4B3F0F0";
        private const string SEPARATOR = "\n";

        internal DeviceController()
        {
            traceLogger = new TraceLogger("", "SHAstroFlatPanel DeviceController");
            traceLogger.Enabled = true;
            isConnected = false;

            serialConnection.Speed = SerialSpeed.ps57600;
            serialConnection.Parity = SerialParity.None;
            serialConnection.StopBits = SerialStopBits.One;
            serialConnection.DataBits = 8;
            serialConnection.ReceiveTimeout = 10;
        }

        internal bool Connected
        {
            get
            {
                traceLogger.LogMessage("Connected Get", isConnected.ToString());
                return this.isConnected;
            }
        }

        internal ConnectResult Connect(String comPortDefault, bool comPortAutoDetect)
        {
            traceLogger.LogMessage("Connect called", "comPortDefault=" + comPortDefault + ", comPortAutoDetect=" + comPortAutoDetect.ToString());

            if(comPortDefault.Length > 0 && TryDetect(comPortDefault))
            {
                bool isSuccess = DoConnect(comPortDefault);
                isConnected = isSuccess;
                return new ConnectResult(isSuccess, comPortDefault);
            }

            if(comPortAutoDetect)
            {
                string detectedComPort = DetectCOMPort();
                if (detectedComPort != null)
                {
                    bool isSuccess = DoConnect(detectedComPort);
                    isConnected = isSuccess;
                    return new ConnectResult(isSuccess, detectedComPort);
                }
            }

            isConnected = false;
            return new ConnectResult(false, "");
        }

        internal void Disconnect()
        {
            traceLogger.LogMessage("Disconnect called", "");
            serialConnection.Connected = false;
            isConnected = false;
        }

        internal int Brightness
        {
            get
            {
                return CommandInt(Commands.GetBrightness);
            }
            set
            {
                if(value == 0)
                {
                    CommandVoid(Commands.Off);
                }
                else
                {
                    CommandVoid(Commands.On(value));
                }
            }
        }

        public void Dispose()
        {
            traceLogger.Enabled = false;
            traceLogger.Dispose();
            traceLogger = null;
            serialConnection.Dispose();
        }

        /// <summary>
        /// Send the given command to the device.
        /// </summary>
        /// <param name="command">Command to send</param>
        /// <returns>Response returned by the device</returns>
        private int CommandInt(string command)
        {
            traceLogger.LogMessage("CommandInt", "Sending command " + command);
            serialConnection.Transmit(command + SEPARATOR);
            Response response = ReadResponse();
            if (response.StatusCode == Response.Status.NOK)
            {
                traceLogger.LogMessage("CommandInt", "Response status code is not OK - Returning 0");
                return 0;
            }
            try
            {
                int result = Int32.Parse(response.Payload);
                return result;
            }
            catch(FormatException e)
            {
                traceLogger.LogMessage("CommandInt", $"Response payload invalid format - {e.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Send the given command to the device.
        /// </summary>
        /// <param name="command">Command to send</param>
        /// <returns>Response returned by the device</returns>
        private string CommandString(string command)
        {
            traceLogger.LogMessage("CommandString", "Sending command " + command);
            serialConnection.Transmit(command + SEPARATOR);
            return ReadResponse().Payload;
        }

        /// <summary>
        /// Send the given command to the device.
        /// </summary>
        /// <param name="command">Command to send</param>
        /// <returns>Response returned by the device</returns>
        private bool CommandBool(string command)
        {
            traceLogger.LogMessage("CommandBool", "Sending command " + command);
            serialConnection.Transmit(command + SEPARATOR);
            return ReadResponse().StatusCode == Response.Status.OK;
        }

        private void CommandVoid(string command)
        {
            traceLogger.LogMessage("CommandVoid", "Sending command " + command);
            serialConnection.Transmit(command + SEPARATOR);
            ReadResponse();
        }

        private Response ReadResponse()
        {
            traceLogger.LogMessage("ReadResponse Start", "");
            String response = serialConnection.ReceiveTerminated(SEPARATOR);
            response = response.Replace("\r", "").Replace("\n", "");
            traceLogger.LogMessage("ReadResponse End", "Received response " + response);
            return new Response(response);
        }

        bool DoConnect(string comPort)
        {
            traceLogger.LogMessage("DoConnect", "Connecting to port " + comPort);
            serialConnection.PortName = comPort;
            serialConnection.Connected = true;
            Response response = ReadResponse();
            if (response.StatusCode != Response.Status.OK)
            {
                traceLogger.LogMessage("DoConnect", "Connecting to port " + comPort + " failed!");
                serialConnection.Connected = false;
                return false;
            }

            return true;
        }

        string DetectCOMPort()
        {
            foreach (string portName in System.IO.Ports.SerialPort.GetPortNames())
            {
                traceLogger.LogMessage("DetectCOMPort", $"Trying port {portName}...");

                if (TryDetect(portName))
                {
                    traceLogger.LogMessage("DetectCOMPort", $"Successfully detected the COM port: {portName}");
                    return portName;
                }
                else
                {
                    continue;
                }
            }

            traceLogger.LogMessage("DetectCOMPort", "Failed detecting the COM port!");
            return null;
        }

        bool TryDetect(string comPort)
        {
            traceLogger.LogMessage("TryConnect", $"Trying port {comPort}...");

            Serial serial = null;

            try
            {
                serial = new Serial
                {
                    Speed = SerialSpeed.ps57600,
                    PortName = comPort,
                    Connected = true,
                    ReceiveTimeout = 1,
                    Parity = SerialParity.None,
                    StopBits = SerialStopBits.One,
                    DataBits = 8
                };
            }
            catch (Exception)
            {
                // If trying to connect to a port that is already in use, an exception will be thrown.
                return false;
            }

            // Wait a second for the serial connection to establish
            System.Threading.Thread.Sleep(1000);

            serial.ClearBuffers();

            // Poll the device (with a short timeout value) until successful,
            // or until we've reached the retry count limit of 3...
            bool success = false;
            for (int retries = 3; retries >= 0; retries--)
            {
                string response = "";
                try
                {
                    // Try to handle the INITIALIZED# message
                    _ = serial.ReceiveTerminated(SEPARATOR);
                    serial.Transmit(Commands.Ping + SEPARATOR);
                    response = serial.ReceiveTerminated(SEPARATOR).Trim().Replace("\r", "").Replace("\n", "");
                }
                catch (Exception)
                {
                    traceLogger.LogMessage("TryConnect", $"Port {comPort} in use or the timeout happend!");
                    // PortInUse or Timeout exceptions may happen here!
                    // We ignore them.
                }
                traceLogger.LogMessage("TryConnect", $"Response from {comPort} is {response}");
                if (response == "OK:" + DEVICE_GUID)
                {
                    success = true;
                    break;
                }
            }

            serial.Connected = false;
            serial.Dispose();

            return success;
        }

        internal struct ConnectResult
        {
            internal bool IsConnected { get; }
            internal string ComPort { get; }

            internal ConnectResult(bool isConnected, string comPort)
            {
                IsConnected = isConnected;
                ComPort = comPort;
            }
        }

        struct Response
        {
            const string OK_RESPONSE = "OK";

            internal enum Status
            {
                OK,
                NOK
            }

            internal Status StatusCode;
            internal string Payload;

            internal Response(string rawResponse)
            {

                string[] responseParts = rawResponse.Split(':');
                if(responseParts.Length != 2)
                {
                    StatusCode = Status.NOK;
                    Payload = "";
                } else
                {
                    StatusCode = responseParts[0] == OK_RESPONSE ? Status.OK : Status.NOK;
                    Payload = responseParts[1];
                }
            }
        }

        class Commands
        {
            internal const String Ping = "PING";
            internal const String GetBrightness = "GETBRIGHTNESS";
            internal const String Off = "OFF";

            internal static String On(int Brightness)
            {
                return $"ON:{Brightness}";
            }
        }
    }
}
