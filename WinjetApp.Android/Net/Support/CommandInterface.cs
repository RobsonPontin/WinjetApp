using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WinjetApp.Droid.Net;

namespace WinjetApp.Droid.Net.Support
{
    public class CommandInterface
    {
        public enum CommandCodes
        {                       /* Packet format                             Result Format                    */
            COMMAND_FAILURE,	/*                                        -> [??+Msg]                         */
            GET_ID,				/* [??+Name]                              -> [??+Name][2=ID][2=Fmt]           */
            RUN_GET,			/* [2=ID][2=Param][2=ReqLen]              -> [2=ID][2=Param][2=VLen][Value]   */
            RUN_SET,			/* [2=ID][2=Param][2=VLen][Value]         -> [2=ID][2=Param][2=VLen]          */
            BUILD_MAP,			/* [2=Num]([2=ID][2=Param][2=ReqLen])...  -> [2=MapID][4=MapLen]              */
            GET_MAP,			/* [2=MapID]                              -> [2=MapID][4=MapLen][Values]      */
            SET_MAP,			/* [2=MapID][4=MapLen][Values]            -> [2=MapID]                        */

            /* Version 2 commands */
            V2_GET_ID,  		/* [??+Name]                                      -> [??+Name][2=ID][2=Fmt][2=Use][2=Cnt]    - add Use,Cnt*/
            V2_RUN_GET,			/* [2=TxID][2=ID][2=Param][2=ReqLen]              -> [2=TxID][2=ID][2=Param][2=VLen][Value]  - Add TxID */
            V2_RUN_SET,			/* [2=TxID][2=ID][2=Param][2=VLen][Value]         -> [2=TxID][2=ID][2=Param][2=VLen]         - Add TxID */
            V2_BUILD_MAP,		/* [2=TxID][2=Num]([2=ID][2=Param][2=ReqLen])...  -> [2=TxID][2=MapID][4=MapLen]             - Add TxID */
            V2_GET_MAP,			/* [2=TxID][2=MapID]                              -> [2=TxID][2=MapID][4=MapLen][Values]     - Add TxID */
            V2_SET_MAP,         /* [2=TxID][2=MapID][4=MapLen][Values]            -> [2=TxID][2=MapID]                       - Add TxID */

            MAX_COMMAND_CODES
        }

        private const string PRE_AMBLE = "~~";
        private const string POST_AMBLE = "\r\n";
        private Connection m_Connection;
        private byte[] ReceiveBuffer;

        public event EventHandler<CommandInterfaceStatusChangeEventArgs> StatusChange;
        public event EventHandler<CommandInterfaceResponseErrorEventArgs> ResponseError;
        public event EventHandler<CommandInterfaceResponseGetIDEventArgs> ResponseGetID;
        public event EventHandler<CommandInterfaceResponseRunGetEventArgs> ResponseRunGet;
        public event EventHandler<CommandInterfaceResponseRunSetEventArgs> ResponseRunSet;
        public Object Tag { get; set; }

        protected virtual void OnStatusChange(CommandInterfaceStatusChangeEventArgs e)
        {
            var handler = StatusChange;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnResponseError(CommandInterfaceResponseErrorEventArgs e)
        {
            var handler = ResponseError;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnResponseGetID(CommandInterfaceResponseGetIDEventArgs e)
        {
            var handler = ResponseGetID;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnResponseRunGet(CommandInterfaceResponseRunGetEventArgs e)
        {
            var handler = ResponseRunGet;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnResponseRunSet(CommandInterfaceResponseRunSetEventArgs e)
        {
            var handler = ResponseRunSet;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public CommandInterface()
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
            if (m_Connection.Connected == false)
                return true;

            return m_Connection.Disconnect();
        }

        public Boolean Connected
        {
            get
            {
                return m_Connection.Connected;
            }
        }

        void m_Connection_StatusChange(object sender, ConnectionStatusChangeEventArgs e)
        {
           // Log.WriteLog(Log.LogLevelType.Comm, "CommandInterface::StatusChange(): " +
           //     "[" + m_Connection.Address + ":" + m_Connection.Port + "] " +
          //      "State Change to " + e.State.ToString().ToUpper());

            var StatusChangeArgs = new CommandInterfaceStatusChangeEventArgs();
            StatusChangeArgs.Status = e.State;

            OnStatusChange(StatusChangeArgs);
        }

        void m_Connection_Error(object sender, ConnectionErrorEventArgs e)
        {
            //Log.WriteLog(Log.LogLevelType.Comm, "CommandInterface::Error(): " +
            //    "[" + m_Connection.Address + ":" + m_Connection.Port + "] " +
            //    e.ErrorMessage);

            var ErrorEventArgs = new CommandInterfaceResponseErrorEventArgs();
            ErrorEventArgs.ErrorText = e.ErrorMessage;

            OnResponseError(ErrorEventArgs);
        }

        public Boolean SendMessage(byte[] CommandBytes)
        {
            List<byte> SendBuf = new List<byte>();


            // get the length of the message (Add 2 for the CRC to be added later)
            Int32 MessageLen = CommandBytes.Length + 2;

            // Add in message length including CRC to empty buffer
            SendBuf.AddRange(BitConverter.GetBytes(MessageLen));

            // Append Buffer
            SendBuf.AddRange(CommandBytes);

            // Append CRC to end
            SendBuf.AddRange(zCrc16.ComputeChecksumBytes(SendBuf.ToArray<byte>()));

            // Insert the Pre-Amble to the front of the buffer
            //   (do this at the end so it is not included in the CRC calculation)
            SendBuf.InsertRange(0, Encoding.ASCII.GetBytes(PRE_AMBLE));

            // Add the Post-Amble to the end of the buffer
            //   (do this at the end so it is not included in the CRC calculation)
            SendBuf.AddRange(Encoding.ASCII.GetBytes(POST_AMBLE));

            Boolean rc = m_Connection.SendRequest(SendBuf.ToArray<byte>());

            //Log.WriteLog(Log.LogLevelType.Comm, "CommandInterface::SendMessage(): " +
            //    "[" + m_Connection.Address + ":" + m_Connection.Port + "] " +
            //    "Sent " + SendBuf.Count().ToString() + " bytes");
           // Log.HexDump(Log.LogLevelType.Comm, SendBuf.ToArray());

            return rc;
        }

        void m_Connection_ReceiveData(object sender, ConnectionReceiveDataEventArgs e)
        {
            //Log.WriteLog(Log.LogLevelType.Comm, "CommandInterface::ReceiveData(): " +
            //    "[" + m_Connection.Address + ":" + m_Connection.Port + "] " +
            //    "Received " + e.Length.ToString() + " bytes");
            //Log.HexDump(Log.LogLevelType.Comm, e.Buffer);

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
            int offset, strLen;

            if (Message == null)
                return;

            if (Message.Count() == 0)
                return;

            //Log.WriteLog(Log.LogLevelType.Comm, "CommandInterface::ParseMessage(): " +
            //    "[" + m_Connection.Address + ":" + m_Connection.Port + "] " +
           //     "Found Message of " + Message.Length.ToString() + " bytes");
           // Log.HexDump(Log.LogLevelType.Comm, Message);

            offset = 0;
            while (offset < Message.Length)
            {
                CommandCodes Command = (CommandCodes)Message[0];
                offset++;   //skip past Command

                if (offset == Message.Length)
                    break;

                switch (Command)
                {
                    case CommandCodes.COMMAND_FAILURE:
                        var ResponseErrorArgs = new CommandInterfaceResponseErrorEventArgs();
                        strLen = BitConverter.ToInt16(Message, offset);
                        offset += sizeof(Int16);

                        ResponseErrorArgs.ErrorText = System.Text.Encoding.ASCII.GetString(Message, offset, strLen);
                        offset += strLen;

                        //Log.WriteLog(Log.LogLevelType.Error, "CommandInterface::ParseMessage(): " +
                        //    "[" + m_Connection.Address + ":" + m_Connection.Port + "] " +
                        //    "COMMAND_FAILURE: " + ResponseErrorArgs.ErrorText);

                        OnResponseError(ResponseErrorArgs);
                        break;

                    case CommandCodes.GET_ID:
                    case CommandCodes.V2_GET_ID:
                        var ResponseGetIDArgs = new CommandInterfaceResponseGetIDEventArgs();

                        strLen = BitConverter.ToInt16(Message, offset);
                        offset += sizeof(Int16);

                        ResponseGetIDArgs.Name = System.Text.Encoding.ASCII.GetString(Message, offset, strLen);
                        offset += strLen;

                        ResponseGetIDArgs.ID = BitConverter.ToInt16(Message, offset);
                        offset += sizeof(Int16);

                        ResponseGetIDArgs.Format = BitConverter.ToInt16(Message, offset);
                        offset += sizeof(Int16);
						
						ResponseGetIDArgs.Use = 0;
                        ResponseGetIDArgs.ParamCount = 0;

                        if (Command == CommandCodes.V2_GET_ID)
                        {
                            ResponseGetIDArgs.Use = BitConverter.ToInt16(Message, offset);
                            offset += sizeof(Int16);

                            ResponseGetIDArgs.ParamCount = BitConverter.ToInt16(Message, offset);
                            offset += sizeof(Int16);
                        }

                        //Log.WriteLog(Log.LogLevelType.Comm, "CommandInterface::ParseMessage(): " +
                        //    "[" + m_Connection.Address + ":" + m_Connection.Port + "] " +
                        //    "GET_ID: " + ResponseGetIDArgs.Name +
                        //    " = " + ResponseGetIDArgs.ID + " (0x" + ResponseGetIDArgs.ID.ToString("X2") + ")" +
                         //   " Format: " + ResponseGetIDArgs.Format +
                        //    " Use: " + ResponseGetIDArgs.Use +
                         //   " ParamCount: " + ResponseGetIDArgs.ParamCount);

                        OnResponseGetID(ResponseGetIDArgs);
                        break;

                    case CommandCodes.RUN_GET:
                    case CommandCodes.V2_RUN_GET:
                        var ResponseRunGetArgs = new CommandInterfaceResponseRunGetEventArgs();

                        ResponseRunGetArgs.TransactionID = 0;

                        if (Command == CommandCodes.V2_RUN_GET)
                        {
                            ResponseRunGetArgs.TransactionID = BitConverter.ToInt16(Message, offset);
                            offset += sizeof(Int16);
                        }

                        ResponseRunGetArgs.ID = BitConverter.ToInt16(Message, offset);
                        offset += sizeof(Int16);

                        ResponseRunGetArgs.Param = BitConverter.ToInt16(Message, offset);
                        offset += sizeof(Int16);

                        ResponseRunGetArgs.Length = BitConverter.ToInt16(Message, offset);
                        offset += sizeof(Int16);

                        ResponseRunGetArgs.Value = Message.Skip(offset).Take(ResponseRunGetArgs.Length).ToArray();
                        offset += ResponseRunGetArgs.Length;

                        //Log.WriteLog(Log.LogLevelType.Comm, "CommandInterface::ParseMessage(): " +
                        //    "[" + m_Connection.Address + ":" + m_Connection.Port + "] " +
                        //    "RUN_GET: ID: " + ResponseRunGetArgs.ID + " (0x" + ResponseRunGetArgs.ID.ToString("X2") + ")" +
                        //    " Param: " + ResponseRunGetArgs.Param + 
                        //    " Length: " + ResponseRunGetArgs.Length +
                        //    " Transaction: " + ResponseRunGetArgs.TransactionID);

                        OnResponseRunGet(ResponseRunGetArgs);
                        break;

                    case CommandCodes.RUN_SET:
                    case CommandCodes.V2_RUN_SET:
                        var ResponseRunSetArgs = new CommandInterfaceResponseRunSetEventArgs();

                        ResponseRunSetArgs.TransactionID = 0;

                        if (Command == CommandCodes.V2_RUN_SET)
                        {
                            ResponseRunSetArgs.TransactionID = BitConverter.ToInt16(Message, offset);
                            offset += sizeof(Int16);
                        }

                        ResponseRunSetArgs.ID = BitConverter.ToInt16(Message, offset);
                        offset += sizeof(Int16);

                        ResponseRunSetArgs.Param = BitConverter.ToInt16(Message, offset);
                        offset += sizeof(Int16);

                        ResponseRunSetArgs.Length = BitConverter.ToInt16(Message, offset);
                        offset += sizeof(Int16);

                        //Log.WriteLog(Log.LogLevelType.Comm, "CommandInterface::ParseMessage(): " +
                       //     "[" + m_Connection.Address + ":" + m_Connection.Port + "] " +
                        //    "RUN_SET: ID: " + ResponseRunSetArgs.ID + " (0x" + ResponseRunSetArgs.ID.ToString("X2") + ")" +
                        //    " Param: " + ResponseRunSetArgs.Param +
                       //     " Length: " + ResponseRunSetArgs.Length +
                        //    " Transaction: " + ResponseRunSetArgs.TransactionID);

                        OnResponseRunSet(ResponseRunSetArgs);
                        break;

                    default:
                        var InvalidCommandErrorArgs = new CommandInterfaceResponseErrorEventArgs();

                        InvalidCommandErrorArgs.ErrorText = "Unimplemented Command: " + Command.ToString("X2");

                        //Log.WriteLog(Log.LogLevelType.Error, "CommandInterface::ParseMessage(): " +
                        //    "[" + m_Connection.Address + ":" + m_Connection.Port + "] " +
                        //    "INVALID_COMMAND: " + InvalidCommandErrorArgs.ErrorText);

                        OnResponseError(InvalidCommandErrorArgs);
                        break;
                }
            }
        }

        private bool GetMessage(ref byte[] Message)
        {
            string Preamble = string.Empty;
            int index, offset, length;
            Int32 DataLen;
            UInt16 MessageCRC, CalculatedCRC;


            if ((ReceiveBuffer == null) || (ReceiveBuffer.Length == 0))
            {
                Message = null;
                return false;
            }

            // find and process data: Format is [2 byte preamble][4 byte length]data[2 byte crc][2 byte postamble]
            index = 0;
            while (index < ReceiveBuffer.Length - 1)
            {
                // Not enough data for preamble
                if (ReceiveBuffer.Length < index + PRE_AMBLE.Length)
                    break;

                // grab first two bytes, and test to see if they are a string?
                Preamble = System.Text.Encoding.ASCII.GetString(ReceiveBuffer, index, PRE_AMBLE.Length);

                if (Preamble == PRE_AMBLE)
                {
                    // skip over pre-amble to data
                    index += PRE_AMBLE.Length;

                    // Not enough data (offset + sizeof(Datalen)
                    if (ReceiveBuffer.Length < index + sizeof(Int32))
                        break;

                    // Get data length (does not include preamble or data length, but includes crc)
                    DataLen = BitConverter.ToInt32(ReceiveBuffer, index);

                    // Not enough data (offset + sizeof(Datalen) + DataLen
                    if (ReceiveBuffer.Length < index + sizeof(Int32) + DataLen + POST_AMBLE.Length)
                        break;

                    // Get data up to CRC
                    length = sizeof(Int32) + DataLen - sizeof(UInt16);
                    byte[] CRCData = ReceiveBuffer.Skip(index).Take(length).ToArray();

                    // Get CRC from end of Data
                    offset = index + length;
                    MessageCRC = BitConverter.ToUInt16(ReceiveBuffer, offset);
                    CalculatedCRC = zCrc16.ComputeChecksum(CRCData.ToArray());
 
                    // Check CRC
                    if (CalculatedCRC != MessageCRC)
                    {
                       // Log.WriteLog(Log.LogLevelType.Warning, "CommandInterface::GetMessage(): " +
                       //     "[" + m_Connection.Address + ":" + m_Connection.Port + "] " +
                       //     "CRCs (" + MessageCRC + "/" + CalculatedCRC + ") do not match");

                        // do not increment index here, we have bypassed the preamble already
                        continue;
                    }
                    else
                    {
                        // Get Message out of CRC data - offset by message size
                        Message = CRCData.Skip(sizeof(Int32)).ToArray();

                        // Add the Message Len and the Size of MessageLen
                        index += (sizeof(Int32) + DataLen + POST_AMBLE.Length);

                        // reduce the number of bytes in the message
                        ReceiveBuffer = ReceiveBuffer.Skip(index).Take(ReceiveBuffer.Length - index).ToArray();

                        //Log.WriteLog(Log.LogLevelType.Debug, "<CommandInterface>{GetMessage}: Dumping remaining bytes for info purposes");
                        //Log.HexDump(Log.LogLevelType.Debug, ReceiveBuffer);

                        return true;
                    }
                }

                // still looking for the begining of the message   
                index++;
            }

            Message = null;
            return false;
        }

        public Boolean SendGetIDFromName(string Name)
        {
            if (String.IsNullOrEmpty(Name))
                return false;

            List<byte> commandBuf = new List<byte>();
            Int16 Length = (Int16)Name.Length;

            // Add Command
            commandBuf.Add((byte)CommandCodes.GET_ID);
            commandBuf.AddRange(BitConverter.GetBytes(Length));
            commandBuf.AddRange(Encoding.ASCII.GetBytes(Name));

            return SendMessage(commandBuf.ToArray<byte>());
        }

        public Boolean SendRunGet(Int16 FunctionID, Int16 Param, Int16 Length)
        {
            List<byte> commandBuf = new List<byte>();

            // Add Command
            commandBuf.Add((byte)CommandCodes.RUN_GET);
            commandBuf.AddRange(BitConverter.GetBytes(FunctionID));
            commandBuf.AddRange(BitConverter.GetBytes(Param));
            commandBuf.AddRange(BitConverter.GetBytes(Length));

            return SendMessage(commandBuf.ToArray<byte>());
        }

        public Boolean SendRunSet(Int16 FunctionID, Int16 Param, byte[] Value)
        {
			if ((Value == null) || (Value.Length <= 0))
				return false;

            List<byte> commandBuf = new List<byte>();

            // Add Command
            commandBuf.Add((byte)CommandCodes.RUN_SET);
            commandBuf.AddRange(BitConverter.GetBytes(FunctionID));
            commandBuf.AddRange(BitConverter.GetBytes(Param));

            Int16 Length = (Int16)Value.Length;
            commandBuf.AddRange(BitConverter.GetBytes(Length));
            commandBuf.AddRange(Value);

            return SendMessage(commandBuf.ToArray<byte>());
        }

        public Boolean SendV2GetIDFromName(string Name)
        {
            if (String.IsNullOrEmpty(Name))
                return false;

            List<byte> commandBuf = new List<byte>();
            Int16 Length = (Int16)Name.Length;

            // Add Command
            commandBuf.Add((byte)CommandCodes.V2_GET_ID);
            commandBuf.AddRange(BitConverter.GetBytes(Length));
            commandBuf.AddRange(Encoding.ASCII.GetBytes(Name));

            return SendMessage(commandBuf.ToArray<byte>());
        }

        public Boolean SendV2RunGet(Int16 TransactionID, Int16 FunctionID, Int16 Param, Int16 Length)
        {
            List<byte> commandBuf = new List<byte>();

            // Add Command
            commandBuf.Add((byte)CommandCodes.V2_RUN_GET);
            commandBuf.AddRange(BitConverter.GetBytes(TransactionID));
            commandBuf.AddRange(BitConverter.GetBytes(FunctionID));
            commandBuf.AddRange(BitConverter.GetBytes(Param));
            commandBuf.AddRange(BitConverter.GetBytes(Length));

            return SendMessage(commandBuf.ToArray<byte>());
        }

        public Boolean SendV2RunSet(Int16 TransactionID, Int16 FunctionID, Int16 Param, byte[] Value)
        {
            if ((Value == null) || (Value.Length <= 0))
                return false;

            List<byte> commandBuf = new List<byte>();

            // Add Command
            commandBuf.Add((byte)CommandCodes.V2_RUN_SET);
            commandBuf.AddRange(BitConverter.GetBytes(TransactionID));
            commandBuf.AddRange(BitConverter.GetBytes(FunctionID));
            commandBuf.AddRange(BitConverter.GetBytes(Param));

            Int16 Length = (Int16)Value.Length;
            commandBuf.AddRange(BitConverter.GetBytes(Length));
            commandBuf.AddRange(Value);

            return SendMessage(commandBuf.ToArray<byte>());
        }
    }

    public class CommandInterfaceStatusChangeEventArgs : EventArgs
    {
        public ConnectionStatus Status { get; set; }
    }

    public class CommandInterfaceResponseErrorEventArgs : EventArgs
    {
        public string ErrorText { get; set; }
    }

    public class CommandInterfaceResponseGetIDEventArgs : EventArgs
    {
        public string Name { get; set; }
        public Int16 ID { get; set; }
        public Int16 Format { get; set; }
        public Int16 Use { get; set; }
        public Int16 ParamCount { get; set; }
    }

    public class CommandInterfaceResponseRunGetEventArgs : EventArgs
    {
        public Int16 TransactionID { get; set; }
        public Int16 ID { get; set; }
        public Int16 Param { get; set; }
        public Int16 Length { get; set; }
        public byte[] Value { get; set; }
    }

    public class CommandInterfaceResponseRunSetEventArgs : EventArgs
    {
        public Int16 TransactionID { get; set; }
        public Int16 ID { get; set; }
        public Int16 Param { get; set; }
        public Int16 Length { get; set; }
    }
}
