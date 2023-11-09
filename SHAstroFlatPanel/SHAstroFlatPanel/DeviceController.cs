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

        private const string SEPARATOR = "\n";

        internal DeviceController()
        {
            traceLogger = new TraceLogger("", "SHAstroFlatPanel DeviceController");
            traceLogger.Enabled = true;
            isConnected = false;
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
