using System;
using System.Collections;
using System.Linq;
using System.Text;

namespace WinjetApp.Droid.Net.Support
{
    public class ClientInterface
    {
        private const string PRE_AMBLE = "~~";
        private const string POST_AMBLE = "\r\n";
        private const int COMMAND_LENGTH = 2;
        private Connection m_Connection;
        private byte[] ReceiveBuffer;

        public event EventHandler<ClientInterfaceStatusChangeEventArgs> StatusChange;
        public event EventHandler<ClientInterfaceResponseStringEventArgs> ResponseLog;
        public event EventHandler<ClientInterfaceResponseStringEventArgs> ResponseTracker;
        public event EventHandler<ClientInterfaceResponseBitsEventArgs> ResponseOutputs;
        public event EventHandler<ClientInterfaceResponseBitsEventArgs> ResponseInputs;
        public event EventHandler<ClientInterfaceResponseBitsEventArgs> ResponseAlarms;

        protected virtual void OnStatusChange(ClientInterfaceStatusChangeEventArgs e)
        {
            var handler = StatusChange;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnResponseLog(ClientInterfaceResponseStringEventArgs e)
        {
            var handler = ResponseLog;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnResponseTracker(ClientInterfaceResponseStringEventArgs e)
        {
            var handler = ResponseTracker;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnResponseOutputs(ClientInterfaceResponseBitsEventArgs e)
        {
            var handler = ResponseOutputs;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnResponseInputs(ClientInterfaceResponseBitsEventArgs e)
        {
            var handler = ResponseInputs;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnResponseAlarms(ClientInterfaceResponseBitsEventArgs e)
        {
            var handler = ResponseAlarms;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public ClientInterface()
        {
            m_Connection = new Connection(0);
            m_Connection.StatusChange += new EventHandler<ConnectionStatusChangeEventArgs>(m_Connection_StatusChange);
            m_Connection.ReceiveData += new EventHandler<ConnectionReceiveDataEventArgs>(m_Connection_ReceiveData);
            m_Connection.Error += new EventHandler<ConnectionErrorEventArgs>(m_Connection_Error);
        }

        public Boolean Connect(int Port, string Address)
        {
            return m_Connection.Connect(Port, Address);
        }

        public Boolean Disconnect()
        {
            return m_Connection.Disconnect();
        }

        public Boolean Connected
        {
            get
            {
                return m_Connection.Connected;
            }
        }

        public string Address
        {
            get
            {
                return m_Connection.Address;
            }
        }

        public int Port
        {
            get
            {
                return m_Connection.Port;
            }
        }

        public override string ToString()
        {
            return Address + ":" + Port;
        }

        void m_Connection_StatusChange(object sender, ConnectionStatusChangeEventArgs e)
        {
            var StatusChangeArgs = new ClientInterfaceStatusChangeEventArgs();
            StatusChangeArgs.Status = e.State;

            OnStatusChange(StatusChangeArgs);
        }

        void m_Connection_Error(object sender, ConnectionErrorEventArgs e)
        {
            var args = new ClientInterfaceResponseStringEventArgs();
            args.Text = e.ErrorMessage;

            OnResponseLog(args);
        }

        void m_Connection_ReceiveData(object sender, ConnectionReceiveDataEventArgs e)
        {
            //Log.WriteLog(Log.LogLevelType.Debug, "ClientInterface::ReceiveData(): Received " + e.Length.ToString() + " bytes");
            //Log.HexDump(Log.LogLevelType.Debug, e.Buffer);

            // Append incoming Data to Global Recv Buffer
            if (ReceiveBuffer == null)
            {
                ReceiveBuffer = e.Buffer;
            }
            else
            {
                int OldLength = ReceiveBuffer.Length;
                Array.Resize(ref ReceiveBuffer, ReceiveBuffer.Length + e.Buffer.Length);
                Array.Copy(e.Buffer, 0, ReceiveBuffer, OldLength, e.Buffer.Length);
            }

            byte[] WorkBuffer = null;

            while (GetMessage(ref WorkBuffer) == true)
            {
                ParseMessage(WorkBuffer);
            }
        }

        private void ParseMessage(byte[] Message)
        {
            //Log.WriteLog(Log.LogLevelType.Debug, "ClientInterface::ParseMessage(): Found Message");
            //Log.HexDump(Log.LogLevelType.Debug, Message);

            ClientInterfaceResponseStringEventArgs ArgsS;
            ClientInterfaceResponseBitsEventArgs ArgsB;

            if (Message == null)
                return;

            if (Message.Count() == 0)
                return;

            String s = System.Text.Encoding.UTF8.GetString(Message, 0, COMMAND_LENGTH);
            int Command;
            byte[] BitBlock1, BitBlock2, BitBlock;

            if (int.TryParse(s, out Command) == false)
                return;

            switch (Command)
            {
                case 4: //outputs
                    ArgsB = new ClientInterfaceResponseBitsEventArgs();
                    BitBlock1 = new byte[2];
                    BitBlock2 = new byte[2];
                    BitBlock = new byte[4];

                    Array.Copy(Message, COMMAND_LENGTH, BitBlock1, 0, 2);
                    Array.Reverse(BitBlock1);
                    
                    Array.Copy(Message, COMMAND_LENGTH + 2, BitBlock2, 0, 2);
                    Array.Reverse(BitBlock2);

                    Array.Copy(BitBlock1, 0, BitBlock, 0, 2);
                    Array.Copy(BitBlock2, 0, BitBlock, 2, 2);

                    ArgsB.Bits = new BitArray(BitBlock);

                    //Log.WriteLog(Log.LogLevelType.Debug, "ClientInterface::ParseMessage(): OUTPUTS: ");
                   // Log.HexDump(Log.LogLevelType.Debug, BitBlock);

                    OnResponseOutputs(ArgsB);
                    break;

                case 5: //inputs
                    ArgsB = new ClientInterfaceResponseBitsEventArgs();
                    BitBlock1 = new byte[2];
                    BitBlock2 = new byte[2];
                    BitBlock = new byte[4];

                    Array.Copy(Message, COMMAND_LENGTH, BitBlock1, 0, 2);
                    Array.Reverse(BitBlock1);

                    Array.Copy(Message, COMMAND_LENGTH + 2, BitBlock2, 0, 2);
                    Array.Reverse(BitBlock2);

                    Array.Copy(BitBlock1, 0, BitBlock, 0, 2);
                    Array.Copy(BitBlock2, 0, BitBlock, 2, 2);

                    ArgsB.Bits = new BitArray(BitBlock);

                   // Log.WriteLog(Log.LogLevelType.Debug, "ClientInterface::ParseMessage(): INPUTS: ");
                   // Log.HexDump(Log.LogLevelType.Debug, BitBlock);

                    OnResponseInputs(ArgsB);
                    break;

                case 6: //alarms
                    ArgsB = new ClientInterfaceResponseBitsEventArgs();
                    BitBlock1 = new byte[2];
                    BitBlock2 = new byte[2];
                    BitBlock = new byte[4];

                    Array.Copy(Message, COMMAND_LENGTH, BitBlock1, 0, 2);
                    Array.Reverse(BitBlock1);

                    Array.Copy(Message, COMMAND_LENGTH + 2, BitBlock2, 0, 2);
                    Array.Reverse(BitBlock2);

                    Array.Copy(BitBlock1, 0, BitBlock, 0, 2);
                    Array.Copy(BitBlock2, 0, BitBlock, 2, 2);

                    ArgsB.Bits = new BitArray(BitBlock);

                    //Log.WriteLog(Log.LogLevelType.Debug, "ClientInterface::ParseMessage(): ALARMS: ");
                    //Log.HexDump(Log.LogLevelType.Debug, BitBlock);

                    OnResponseAlarms(ArgsB);
                    break;

                case 7: //log messages
                    ArgsS = new ClientInterfaceResponseStringEventArgs();
                    ArgsS.Text = System.Text.Encoding.UTF8.GetString(Message, COMMAND_LENGTH, Message.Length - COMMAND_LENGTH);

                   // Log.WriteLog(Log.LogLevelType.Debug, "ClientInterface::ParseMessage(): LOG: " + ArgsS.Text);
                    OnResponseLog(ArgsS);
                    break;

                case 8: //tracker
                    ArgsS = new ClientInterfaceResponseStringEventArgs();
                    ArgsS.Text = System.Text.Encoding.UTF8.GetString(Message, COMMAND_LENGTH, Message.Length - COMMAND_LENGTH);

                    //Log.WriteLog(Log.LogLevelType.Debug, "ClientInterface::ParseMessage(): TRACKER: " + ArgsS.Text);
                    OnResponseTracker(ArgsS);
                    break;
            }
        }

        private bool GetMessage(ref byte[] Message)
        {
            string Preamble = string.Empty;
            string Postamble = string.Empty;
            int index, length, postindex;

            if ((ReceiveBuffer == null) || (ReceiveBuffer.Length == 0))
            {
                Message = null;
                return false;
            }

            // find and process data: Format is [2 byte preamble]data[2 byte postamble]
            index = 0;
            while (index < ReceiveBuffer.Length - 1)
            {
                // Not enough data for preamble
                if (ReceiveBuffer.Length < index + PRE_AMBLE.Length + 1 + POST_AMBLE.Length)
                    break;

                // grab first two bytes, and test to see if they are a string?
                Preamble = System.Text.Encoding.ASCII.GetString(ReceiveBuffer, index, PRE_AMBLE.Length);

                if (Preamble != PRE_AMBLE)
                {
                    index++;

                    // still looking for the begining of the message   
                    continue;
                }

                Boolean found = false;
                postindex = index + PRE_AMBLE.Length;
                while (postindex < ReceiveBuffer.Length - 1)
                {
                    Postamble = System.Text.Encoding.ASCII.GetString(ReceiveBuffer, postindex, POST_AMBLE.Length);
                    if (Postamble != POST_AMBLE)
                    {
                        postindex++;

                        // still looking for the end of the message   
                        continue;
                    }

                    found = true;
                    break;
                }

                //found preamble, but no postamble yet
                if (found == false)
                    return false;

                // skip over pre-amble to data
                index += PRE_AMBLE.Length;
                length = postindex - index;

                // Get Message
                Message = ReceiveBuffer.Skip(index).Take(length).ToArray();

                // Add the Message Len and the Size of MessageLen
                index += (length + POST_AMBLE.Length);

                // reduce the number of bytes in the message
                ReceiveBuffer = ReceiveBuffer.Skip(index).Take(ReceiveBuffer.Length - index).ToArray();

                //Log.WriteLog(Log.LogLevelType.Debug, "<ClientInterface>{GetMessage}: Dumping remaining bytes for info purposes");
                //Log.HexDump(Log.LogLevelType.Debug, ReceiveBuffer);

                return true;
            }

            Message = null;
            return false;
        }
    }

    public class ClientInterfaceStatusChangeEventArgs : EventArgs
    {
        public ConnectionStatus Status { get; set; }
    }

    public class ClientInterfaceResponseStringEventArgs : EventArgs
    {
        public string Text { get; set; }
    }

    public class ClientInterfaceResponseBitsEventArgs : EventArgs
    {
        public BitArray Bits { get; set; }
    }
}
