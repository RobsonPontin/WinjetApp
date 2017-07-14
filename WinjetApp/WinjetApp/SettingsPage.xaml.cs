using System;
using System.Collections.Generic;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace WinjetApp.WinjetApp
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class SettingsPage : ContentPage
	{

        public class Function : BindableObject
        {
            private string _name;
            private string _value;

            public string Name
            {
                get { return _name; }
                set { _name = value; }
            }

            public string Value
            {
                get { return _value; }
                set { _value = value; }
            }
        }

        List<Function> Functions = new List<Function>();

		public SettingsPage ()
		{
			InitializeComponent ();

            Functions.Add(new Function { Name = "Offset PH1", Value = "3423", });
            Functions.Add(new Function { Name = "Offset PH2", Value = "5434", });
        }


        private void btnEnableEncoder_Clicked(object sender, EventArgs e)
        {
            lvOffset.ItemsSource = Functions;
            lvOffset.ItemTemplate = PopulateListView();
            lvOffset.RowHeight = 100;
        }

        private DataTemplate PopulateListView()
        {

            var template = new DataTemplate(() =>
            {
                var stackLayout = new StackLayout();
                stackLayout.Padding = 10;
                stackLayout.Spacing = 10;
                stackLayout.Orientation = StackOrientation.Horizontal;
                
                var btnOffset = new Button();
                btnOffset.SetBinding(Button.TextProperty, "Name");

                var lbDisplay = new Label();
                lbDisplay.SetBinding(Label.TextProperty, "Value");
                lbDisplay.FontSize = 30;

                stackLayout.Children.Add(btnOffset);
                stackLayout.Children.Add(lbDisplay);

                // Child of a DataTemplate must be a ViewCell
                // and the layout used is a stacklayout
                return new ViewCell { View = stackLayout};
            });
            return template;
        }
    }
}
