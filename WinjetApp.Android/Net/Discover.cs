using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using WinjetApp.Droid.Net.Support;

namespace WinjetApp.Droid.Net
{
    public class DiscoverData : EventArgs
    {
        public string Address { get; private set; }
        public string Product { get; private set; }
        public string Version { get; private set; }
        public string Name { get; private set; }
        public int ClientPort { get; private set; }
        public int CommandPort { get; private set; }

        private DiscoverData(string Address, string Product, string Version, string Name, int ClientPort, int CommandPort)
        {
            this.Address = Address;
            this.Product = Product;
            this.Version = Version;
            this.Name = Name;
            this.ClientPort = ClientPort;
            this.CommandPort = CommandPort;
        }

        public static DiscoverData NewDiscoverData(string Address, string Product, string Version, string Name, int ClientPort, int CommandPort)
        {
            return new DiscoverData(Address, Product, Version, Name, ClientPort, CommandPort);
        }

        // Make this similar to the  RemoteFunctionHost
        public override string ToString()
        {
            string s = "Discovered Location";

            if (Address == null)
                return s;

            s = Address + ":" + CommandPort.ToString();
            if ((Name != null) || (Name.Length != 0))
                s += " [" + Name + "]";
            
            if ((Product != null) || (Product.Length != 0))
                s += " {" + Product + "}";


            return s;
        }
    }

    public class Discover
    {
        private const int DISCOVER_PORT = 4353;
        private UDPBroadcast m_Broadcast;
        public string DiscoverText { get; set; }
        
        private List<DiscoverData> m_DiscoverData;
        public ReadOnlyCollection<DiscoverData> Data { get { return m_DiscoverData.AsReadOnly(); } }
        public event EventHandler<DiscoverData> DiscoverReceiveData;

        public Discover()
        {
            DiscoverText = "Unknown";
            m_Broadcast = new UDPBroadcast(DISCOVER_PORT);
            m_Broadcast.ReceiveBroadcast += new EventHandler<UDPBroadcastReceiveBroadcastEventArgs>(m_Broadcast_ReceiveBroadcast);
            m_DiscoverData = new List<DiscoverData>();
        }

        void m_Broadcast_ReceiveBroadcast(object sender, UDPBroadcastReceiveBroadcastEventArgs e)
        {
            string s = System.Text.Encoding.UTF8.GetString(e.Buffer);
            string[] ss = s.Split('|');

            if (ss.Length >= 4)
            {
                /*
                 * [0] = Product        "WinJetII" / "TaggerPrinter" / "QueueController"
                 * [1] = Version        "3.2"
                 * [2] = Name           "3.x-dev"
                 * [3] = ClientPort     "6000"
                 * 
                 * WinJetVersion >= svn 1518
                 * QueueController/Tagger >= svn 1623
                 * QueueController/Tagger >= TaggerTree svn 213
                 * [4] = CommandPort    "6500"
                 */

                int clientport;
                if (int.TryParse(ss[3], out clientport) == false)
                    clientport = -1;

                int commandport = 0;
                if (ss.Length >= 5)
                    if (int.TryParse(ss[4], out commandport) == false)
                        commandport = 0;

                
                DiscoverData dd = DiscoverData.NewDiscoverData(e.Address, ss[0], ss[1], ss[2], clientport, commandport);
                
                /* Add Data to local store */
                m_DiscoverData.Add(dd);

                /* Generate Event */
                OnDiscoverReceiveData(dd);
            }
        }

        public void Broadcast()
        {
            m_DiscoverData.Clear();
            m_Broadcast.Broadcast(DiscoverText);
        }

        protected virtual void OnDiscoverReceiveData(DiscoverData e)
        {
            var handler = DiscoverReceiveData;
            if (handler != null)
            {
                handler(this, e);
            }
        }
    }
}
