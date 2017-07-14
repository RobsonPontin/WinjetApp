using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

//TODO: Need to send disconnect event before error event in case 
//      something is expecting disconnect event and error event
//      is a message box, which pre-empts disconnect event
//      ie: timer sends message, disconnect event turns off timer, error event shows msgbox
//          in this case, the send will continue until the message box is cleared
//          we could have timer check the 'connected' status or send the disconnect event first

namespace WinjetApp.Droid.Net.Support
{
    public enum ConnectionStatus
    {
        CONNECTED,
        DISCONNECTED,
        CONNECTING
    };

    public class ConnectionReceiveDataEventArgs : EventArgs
    {
        public int ConnectionID { get; set; }
        public int Length { get; set; }
        public byte[] Buffer { get; set; }
    }

    public class ConnectionStatusChangeEventArgs : EventArgs
    {
        public int ConnectionID { get; set; }
        public ConnectionStatus State { get; set; }
    }

    public class ConnectionErrorEventArgs : EventArgs
    {
        public int ConnectionID { get; set; }
        public int ErrorCode { get; set; }
        public String ErrorMessage { get; set; }
    }


    public class Connection
    {
    	//State object for receiving data from remote device.
        private class StateObject
        {
			//Client socket.
            public Socket WorkSocket = null;
        	//Size of receive buffer.
            public const int BufferSize = 512;
        	//Receive buffer.
            public Byte[] buffer = new Byte[BufferSize];
        }

        private Socket ClientSocket = null;
        private Boolean m_Connecting = false;
        private int m_ID = -1;
        private readonly Object countLock = new Object();

        public event EventHandler<ConnectionReceiveDataEventArgs> ReceiveData;
        public event EventHandler<ConnectionStatusChangeEventArgs> StatusChange;
        public event EventHandler<ConnectionErrorEventArgs> Error;

        protected virtual void OnReceiveData(ConnectionReceiveDataEventArgs e)
        {
            var handler = ReceiveData;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnStatusChange(ConnectionStatusChangeEventArgs e)
        {
            var handler = StatusChange;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnError(ConnectionErrorEventArgs e)
        {
            var handler = Error;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public string Address { get; private set; }
        public int Port { get; private set; }


        public Connection(int ID)
        {
            m_ID = ID;
            Address = "";
            Port = 0;
        }

        public Boolean Connect(int Port, string Address)
        {
            if (Address == null)
                return false;

            if ((Port == 0) || (Address.Trim() == ""))
                return false;

            //already connected
            if (Connected)
                return false;

            //already in async connect
            if (m_Connecting == true)
                return true;

            //trying to connect when already connected, return true
            //if (ClientSocket != null)
            //    if (ClientSocket.Connected == true)
            //        return true;

            this.Address = Address;
            this.Port = Port;

            //create new connection
            ClientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            StateObject state = new StateObject();
            state.WorkSocket = ClientSocket;

            m_Connecting = true;

            ConnectionStatusChangeEventArgs StatusArgs = new ConnectionStatusChangeEventArgs();
            StatusArgs.ConnectionID = m_ID;
            StatusArgs.State = ConnectionStatus.CONNECTING;
            OnStatusChange(StatusArgs);

            try
            {
                ClientSocket.BeginConnect(Address, Port, new AsyncCallback(ConnectCallback), state);
            }
            catch (SocketException e)
            {
                /* Generate Event */
                ConnectionErrorEventArgs args = new ConnectionErrorEventArgs();
                args.ConnectionID = m_ID;
                args.ErrorCode = e.ErrorCode;
                args.ErrorMessage = e.Message;
                OnError(args);
                
                Disconnect();
                return false;
            }
            catch (Exception)
            {
                Disconnect();
                return false;
            }

            return true;
        }


        public Boolean Disconnect()
        {
            m_Connecting = false;

            //if socket is created
            if (ClientSocket != null)
            {
                //if socket is connected, disconnect
                if (ClientSocket.Connected == true)
                {
                    try
                    {
                        //Turn off Sending
                        ClientSocket.Shutdown(SocketShutdown.Send);
                        //ClientSocket.Disconnect(True)

                        //wait for socket to be empty
                        Byte[] b = new Byte[256];
                        while (ClientSocket.Receive(b, 256, SocketFlags.None) > 0)
                            System.Threading.Thread.Sleep(1);

                        //Shutdown the Recieve Socket!
                        if (ClientSocket != null)
                            ClientSocket.Shutdown(SocketShutdown.Receive);
                    }
                    catch (SocketException e)
                    {
                        ConnectionErrorEventArgs args = new ConnectionErrorEventArgs();
                        args.ConnectionID = m_ID;
                        args.ErrorCode = e.ErrorCode;
                        args.ErrorMessage = e.Message;
                        OnError(args);
                    }
                    catch (Exception)
                    {
                        /* We are trying to catch the NullReferenceException 
                         * but we still want to trigger the ConnectionStatusChangeEventArgs
                         * so allow the error to fall through
                         */
                    }

                    if (ClientSocket != null)
                    {
                        try
                        {
                            ClientSocket.Disconnect(false);
                        }
                        catch (SocketException e)
                        {
                            ConnectionErrorEventArgs args = new ConnectionErrorEventArgs();
                            args.ConnectionID = m_ID;
                            args.ErrorCode = e.ErrorCode;
                            args.ErrorMessage = e.Message;
                            OnError(args);
                        }
                        catch (Exception)
                        {
                            /* We are trying to catch the NullReferenceException 
                             * but we still want to trigger the ConnectionStatusChangeEventArgs
                             * so allow the error to fall through
                             */
                        }
                    }
                }

                //close socket object
                if (ClientSocket != null)
                {
                    ClientSocket.Close(1);
                    ClientSocket = null;
                }

                ConnectionStatusChangeEventArgs StatusArgs = new ConnectionStatusChangeEventArgs();
                StatusArgs.ConnectionID = m_ID;
                StatusArgs.State = ConnectionStatus.DISCONNECTED;
                OnStatusChange(StatusArgs);
            }

            Address = "";
            Port = 0;

            return true;
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            StateObject state = (StateObject)ar.AsyncState;
            Socket client = state.WorkSocket;

            m_Connecting = false;

            //ensure current client connection is the same as
            //the one contained in the callback object
            if (client != ClientSocket)
                return;

            try
            {
                client.EndConnect(ar);
            }
            catch (SocketException e)
            {
                ConnectionErrorEventArgs args = new ConnectionErrorEventArgs();
                args.ConnectionID = m_ID;
                args.ErrorCode = e.ErrorCode;
                args.ErrorMessage = e.Message;
                OnError(args);

                Disconnect();
                return;
            }
            catch (Exception) 
            {
                Disconnect();
                return;
            }

            if (client != ClientSocket)
                return;

            if (client.Connected == true)
            {
                ConnectionStatusChangeEventArgs StatusArgs = new ConnectionStatusChangeEventArgs();
                StatusArgs.ConnectionID = m_ID;
                StatusArgs.State = ConnectionStatus.CONNECTED;
                OnStatusChange(StatusArgs);

                if (client != ClientSocket)
                {
                    Disconnect();
                    return;
                }

                if (ClientSocket == null)
                    return;

                if (client.Connected == false)
                {
                    Disconnect();
                    return;
                }

                try
                {
                    client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
                }
                catch (SocketException e)
                {
                    ConnectionErrorEventArgs args = new ConnectionErrorEventArgs();
                    args.ConnectionID = m_ID;
                    args.ErrorCode = e.ErrorCode;
                    args.ErrorMessage = e.Message;
                    OnError(args);
                    return;
                }
                catch (Exception)
                {
                    return;
                }
            }
        }


        private void ReceiveCallback(IAsyncResult ar)
        {
            // Retrieve the state object and the client socket 
            // from the asynchronous state object.
            StateObject state = (StateObject)ar.AsyncState;
            Socket client = state.WorkSocket;
            SocketError error;
            int bytesRead;

            //ensure current client connection is the same as
            //the one contained in the callback object
            if (client != ClientSocket)
                return;

            if (ClientSocket == null)
                return;

            if (Connected == false)
                return;


            System.Threading.Monitor.Enter(countLock);

            //Read data from the remote device.
            try
            {
                bytesRead = client.EndReceive(ar, out error);
            }
            catch (SocketException e)
            {
                System.Threading.Monitor.Exit(countLock);

                ConnectionErrorEventArgs args = new ConnectionErrorEventArgs();
                args.ConnectionID = m_ID;
                args.ErrorCode = e.ErrorCode;
                args.ErrorMessage = e.Message;
                OnError(args);

                Disconnect();
                return;
            }
            catch (Exception)
            {
                Disconnect();
                return;
            }

            if ((bytesRead == 0) || (error != SocketError.Success))
            {
                System.Threading.Monitor.Exit(countLock);

                if (error != SocketError.Success)
                {
                    ConnectionErrorEventArgs args = new ConnectionErrorEventArgs();
                    args.ConnectionID = m_ID;
                    args.ErrorCode = (int)error;
                    args.ErrorMessage = "Disconnect: Bytes = " + bytesRead.ToString() + ", Error = " + error.ToString();
                    OnError(args);
                }

                Disconnect();
                return;
            }

            Byte[] buffer = null;

            if (bytesRead > 0)
            {
                buffer = new Byte[bytesRead];
                Array.Copy(state.buffer, buffer, bytesRead);
            }

            System.Threading.Monitor.Exit(countLock);

            if (bytesRead > 0)
            {
                ConnectionReceiveDataEventArgs RecvArgs = new ConnectionReceiveDataEventArgs();
                RecvArgs.ConnectionID = m_ID;
                RecvArgs.Buffer = buffer;
                RecvArgs.Length = bytesRead;
                OnReceiveData(RecvArgs);
            }


            //invoke could trigger disconnect (or disconnect via send()),  
            //which means our socket could be invalid 
            if (client != ClientSocket)
                return;

            if (ClientSocket == null)
                return;

            if (Connected == false)
                return;


            //Get the rest of the data.
            try
            {
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
            }
            catch (SocketException e)
            {
                ConnectionErrorEventArgs args = new ConnectionErrorEventArgs();
                args.ConnectionID = m_ID;
                args.ErrorCode = e.ErrorCode;
                args.ErrorMessage = e.Message;
                OnError(args);

                Disconnect();
                return;
            }
            catch (Exception)
            {
                Disconnect();
                return;
            }
        }

        public Boolean SendRequest(Byte[] Buffer)
        {
            if (ClientSocket == null)
                return false;

            if (Connected == false)
                return false;

            try
            {
                //A first chance exception of type 'System.InvalidOperationException' occurred in System.dll
                //{"Cannot block a call on this socket while an earlier asynchronous call is in progress."}
                ClientSocket.Send(Buffer, SocketFlags.None);
            }
            catch (SocketException e)
            {
                ConnectionErrorEventArgs args = new ConnectionErrorEventArgs();
                args.ConnectionID = m_ID;
                args.ErrorCode = e.ErrorCode;
                args.ErrorMessage = e.Message;
                OnError(args);

                Disconnect();
                return false;
            }
            catch (Exception)
            {
                Disconnect();
                return false;
            }

            return true;
        }


        public Boolean SendRequest(string Buffer)
        {
            if (String.IsNullOrEmpty(Buffer))
                return false;

            return SendRequest(System.Text.Encoding.UTF8.GetBytes(Buffer));
        }

        public Boolean Connected
        {
            get
            {
                if (ClientSocket == null)
                    return false;

                return ClientSocket.Connected;
            }
        }
    }
}
