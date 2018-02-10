using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace MangleSocks.Mobile.Pages
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class About
	{
	    public About()
	    {
	        this.InitializeComponent();
	    }

	    void HandleProjectLinkTapped(object sender, EventArgs e)
	    {
            Device.OpenUri(new Uri(this.ProjectLink.Text));
	    }
	}
}