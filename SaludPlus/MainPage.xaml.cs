using Microsoft.Maui.Controls;
using SaludPlus.ViewModels; // Referencia a tu nueva carpeta

namespace SaludPlus
{
	public partial class MainPage : ContentPage
	{
		public MainPage()
		{
			InitializeComponent();

			// Instanciamos el modelo desde la carpeta ViewModels
			var vm = new MainPageModel();
			BindingContext = vm;
			vm.StartSimulation();
		}
	}
}