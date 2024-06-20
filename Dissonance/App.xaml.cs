using Dissonance.SettingsManagers;

using Microsoft.Extensions.DependencyInjection;

using System;
using System.Windows;

namespace Dissonance
{
	public partial class App : Application
	{
		public static IServiceProvider ServiceProvider { get; private set; }

		protected override void OnStartup ( StartupEventArgs e )
		{
			var serviceCollection = new ServiceCollection();
			ConfigureServices ( serviceCollection );
			ServiceProvider = serviceCollection.BuildServiceProvider ( );

			var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
			mainWindow.Show ( );
		}
		private void ConfigureServices ( IServiceCollection services )
		{
			services.AddSingleton<ISettingsManager, SettingsManager> ( );
			services.AddSingleton<MainWindow> ( );
		}
	}
}
