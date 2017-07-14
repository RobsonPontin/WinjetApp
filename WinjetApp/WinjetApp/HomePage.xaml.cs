using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinjetApp.Net;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;


namespace WinjetApp.WinjetApp
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class HomePage : ContentPage
	{      
        /// <summary>
        /// Constructor
        /// </summary>
		public HomePage ()
		{
			InitializeComponent ();

            _clients.Add(new Client { Name = "WinjetTest", Address = "192.168.234.112", Product = "Winjet II" });
            _clients.Add(new Client { Name = "Winet II Shop", Address = "192.168.234.110", Product = "Winjet II" });

            _discover.DiscoverReceiveData += _discover_DiscoverReceiveData;
            lvLocationBrowser.ItemTapped += LvLocationBrowser_ItemTapped;

            Content = GenerateOverview();
		}

        #region 1 - PAGE LAYOUT

        ListView lvLocationBrowser = new ListView();
        ListView lvFollowers = new ListView();

        private StackLayout GenerateOverview()
        {
            StackLayout stackLayout = new StackLayout();

            #region --- Create Location Browser

            lvLocationBrowser.HorizontalOptions = LayoutOptions.Center;

            lvLocationBrowser.ItemsSource = _clients;
            lvLocationBrowser.ItemTemplate = PopulateClients();

            #endregion

            #region --- Create Action Buttons Section

            var stackLayoutActBTN = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
            };

            Button btnPrint = new Button
            {
                Text = "Print",
                HorizontalOptions = LayoutOptions.FillAndExpand,                
            };
            btnPrint.Clicked += BtnPrint_Clicked;
            
            Button btnCamera = new Button
            {
                Text = "Camera",
                HorizontalOptions = LayoutOptions.FillAndExpand,
            };

            stackLayoutActBTN.Children.Add(btnPrint);
            stackLayoutActBTN.Children.Add(btnCamera);

            #endregion

            #region --- Create Feedback Section

            lvFollowers.HorizontalOptions = LayoutOptions.Fill;

            StackLayout stackLayoutDisplay = new StackLayout();
            Label lbStatus = new Label
            {
                Text = "Print Disabled",
             };
            Label lbAlarm = new Label
            {
                Text = "No alarms",
            };

            stackLayoutDisplay.Children.Add(lbStatus);
            stackLayoutDisplay.Children.Add(lbAlarm);
            stackLayoutDisplay.Children.Add(lvFollowers);

            #endregion

            stackLayout.Children.Add(lvLocationBrowser);
            stackLayout.Children.Add(stackLayoutActBTN);
            stackLayout.Children.Add(stackLayoutDisplay);

            return stackLayout;
        }

        #endregion

        #region 2 - FIRST SECTION: List view with all current clients

        Discover _discover = new Discover();
        List<Client> _clients = new List<Client>();

        /// <summary>
        /// Discover received data from UDP Broadcast with each client connected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _discover_DiscoverReceiveData(object sender, DiscoverData e)
        {
            // Skip if address exist in the list
            var exist = _clients.Where(c => c.Address == e.Address);
            if (!exist.Any())
            {
                Client client = new Client
                {
                    Name = e.Name,
                    Product = e.Product,
                    Address = e.Address,
                    ClientPort = e.ClientPort,
                    CommandPort = e.CommandPort,
                    Version = e.Version,
                };
                _clients.Add(client);
            }
        }

        class Client
        {
            public string Name { get; set; }
            public string Product { get; set; }
            public string Address { get; set; }
            public int ClientPort { get; set; }
            public int CommandPort { get; set; }
            public string Version { get; set; }
            public Boolean Status { get; set; }
        }

        /// <summary>
        /// Bindinded to the list of Client Class objects
        /// </summary>
        /// <returns></returns>
        private DataTemplate PopulateClients()
        {
            var template = new DataTemplate(() =>
            {
                var layout = new StackLayout();

                var stackLayoutTitle = new StackLayout
                {
                    Orientation = StackOrientation.Horizontal,
                };

                Label lbName = new Label();
                lbName.SetBinding(Label.TextProperty, "Name");
                lbName.FontAttributes = FontAttributes.Bold;

                Label lbProduct = new Label();
                lbProduct.SetBinding(Label.TextProperty, "Product");

                stackLayoutTitle.Children.Add(lbName);
                stackLayoutTitle.Children.Add(lbProduct);

                Label lbAddress = new Label();
                lbAddress.SetBinding(Label.TextProperty, "Address");

                layout.Children.Add(stackLayoutTitle);
                layout.Children.Add(lbAddress);

                return new ViewCell { View = layout };
            });

            return template;
        }

        /// <summary>
        /// Event to treat when a item is selected on the listview
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LvLocationBrowser_ItemTapped(object sender, ItemTappedEventArgs e)
        {
            // Message asking if the client should connect to this host
            var lv = sender as ListView;
            if (lv == null)
                return;

            DisplayAlert("Alert!", "Not Functional", "Exit");
        }

        #endregion

        #region FEEDBACK SECTION

        /// <summary>
        /// Tied to Feedback Display
        /// </summary>
        class Follower
        {
            string Message;
            String Type;
        }

        private DataTemplate PopulateFeedBackLV()
        {
            var template = new DataTemplate(() =>
            {
                var layout = new StackLayout();



                return new ViewCell { View = layout};
            });

            return template;
        }

        #endregion

        private void BtnPrint_Clicked(object sender, EventArgs e)
        {
            DisplayAlert("Alert!", "Not Functional", "Exit");
        }
    }
}
