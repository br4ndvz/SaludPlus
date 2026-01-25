using CommunityToolkit.Mvvm.ComponentModel;
using System.IO.Ports; // Asegúrate de haber instalado System.IO.Ports vía NuGet

namespace SaludPlus.ViewModels
{
	public partial class MainPageModel : ObservableObject
	{
		[ObservableProperty]
		private string _heartRate = "0";

		[ObservableProperty]
		private string _temperature = "0.0";

		[ObservableProperty]
		private string _stepCount = "0";

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(ConnectionText))] // Notifica que el texto debe cambiar
		private bool _isConnected = false;

		// Propiedad que devuelve el texto basado en el estado
		public string ConnectionText => IsConnected ? "Arduino Conectado" : "Arduino Desconectado";

		public void StartSimulation()
		{
			// Mantenemos la simulación por ahora para probar visualmente
			if (Application.Current?.Dispatcher != null)
			{
				var timer = Application.Current.Dispatcher.CreateTimer();
				timer.Interval = TimeSpan.FromSeconds(1);
				timer.Tick += (s, e) =>
				{
					// Simulamos que el estado cambia aleatoriamente para probar el punto
					// En la vida real, esto cambiará cuando abras el puerto Serial
					HeartRate = Random.Shared.Next(65, 85).ToString();
					Temperature = (36.5 + Random.Shared.NextDouble() * 0.5).ToString("N1");

					if (int.TryParse(StepCount, out int steps))
						StepCount = (steps + Random.Shared.Next(1, 5)).ToString();
				};
				timer.Start();
			}
		}
	}
}