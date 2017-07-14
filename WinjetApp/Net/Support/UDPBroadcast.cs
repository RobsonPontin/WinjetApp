using System;
using System.Net;
using System.Net.Sockets;
//using System.Net.NetworkInformation;

namespace WinjetApp.Net.Support
{
    public class UDPBroadcastReceiveBroadcastEventArgs : EventArgs
    {
        public string Address { get; set; }
        public int Port { get; set; }
        public byte[] Buffer { get; set; }
    }

    public class UDPBroadcast
    {
        private UdpClient m_UDPClient = null;
        private int m_Port = 0;

        public event EventHandler<UDPBroadcastReceiveBroadcastEventArgs> ReceiveBroadcast;

        public UDPBroadcast(int Port)
        {
            m_Port = Port;
            m_UDPClient = new UdpClient(0);
            m_UDPClient.EnableBroadcast = true;

            IPEndPoint EndPoint = new IPEndPoint(IPAddress.Any, 0);

            try
            {
                m_UDPClient.BeginReceive(new AsyncCallback(ReceiveCallback), m_UDPClient);
            }
            catch (SocketException se)
            {
                //System.Diagnostics.Debug.Print("UDPBroadcast: BeginReceive: {0}", se.Message);
                return;
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            UdpClient u = (UdpClient)ar.AsyncState;
            IPEndPoint ep = new IPEndPoint(IPAddress.Any, m_Port);
            Byte[] receiveBytes;

            try
            {
                receiveBytes = u.EndReceive(ar, ref ep);
            }
            catch (SocketException se)
            {
                //System.Diagnostics.Debug.Print("UDPBroadcast: EndReceive: {0}", se.Message);
                return;
            }

            //System.Diagnostics.Debug.Print("UDPBroadcast: [{0}:{1}] {2}", ep.Address.ToString(), ep.Port.ToString(), Encoding.ASCII.GetString(receiveBytes));

            /* Generate Event */
            var args = new UDPBroadcastReceiveBroadcastEventArgs();
            args.Address = ep.Address.ToString();
            args.Port = ep.Port;
            args.Buffer = receiveBytes;

            OnReceiveBroadcast(args);

            /* Read more data */
            try
            {
                u.BeginReceive(new AsyncCallback(ReceiveCallback), u);
            }
            catch (SocketException se)
            {
                //System.Diagnostics.Debug.Print("UDPBroadcast: BeginReceive: {0}", se.Message);
            }
        }

        public void Broadcast(string BroadcastData)
        {
            Broadcast(System.Text.Encoding.UTF8.GetBytes(BroadcastData));
        }

        public void Broadcast(byte[] BroadcastData)
        {
            if (m_UDPClient == null)
                return;

            IPEndPoint IPEndPoint = new IPEndPoint(IPAddress.Broadcast, m_Port);

            try
            {
                m_UDPClient.Send(BroadcastData, BroadcastData.Length, IPEndPoint);
            }
            catch (SocketException se)
            {
                //System.Diagnostics.Debug.Print("UDPBroadcast: Send: {0}", se.Message);
                return;
            }
        }

        protected virtual void OnReceiveBroadcast(UDPBroadcastReceiveBroadcastEventArgs e)
        {
            var handler = ReceiveBroadcast;
            if (handler != null)
            {
                handler(this, e);
            }
        }


        /*
        foreach (NetworkInterface netif in NetworkInterface.GetAllNetworkInterfaces())
        {
            // if (netif.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || 
            //         netif.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
            {
                IPInterfaceProperties properties = netif.GetIPProperties();

                foreach (IPAddressInformation address in properties.UnicastAddresses)
                {
                    // We're only interested in IPv4 addresses for now 
                    if (address.Address.AddressFamily != AddressFamily.InterNetwork)
                        continue;

                    // Ignore loopback addresses (e.g., 127.0.0.1) 
                    if (IPAddress.IsLoopback(address.Address))
                        continue;


                    lvPrinters.Items.Add(address.Address.ToString());
                }
            }
        }
        */
    }
}
