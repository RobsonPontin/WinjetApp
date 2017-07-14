using WinjetApp.Views;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace WinjetApp
{
	public partial class App : Application
	{
        public App()
		{
			InitializeComponent();

            //SetTabbedMainPage();
            SetMainPage();
		}

        public static void SetMainPage()
        {
            Current.MainPage = new TabbedPage
            {
                Children =
                {
                    new WinjetApp.HomePage(),
                    new WinjetApp.SettingsPage(),
                }
            };
        }


		public static void SetTabbedMainPage()
		{
            Current.MainPage = new TabbedPage
            {
                Children =
                {
                    new NavigationPage(new ItemsPage())
                    {
                        Title = "Browse",
                        //Icon = Device.OnPlatform("tab_feed.png",null,null)
                    },                    
                    new NavigationPage(new SettingsPage())
                    {
                        Title = "Settings"
                    },
                    new NavigationPage(new AboutPage())
                    {
                        Title = "About",
                        //Icon = Device.OnPlatform("tab_about.png",null,null)
                    },
                }
            };
        }
	}
}
