using System.Configuration;
using System.Data;
using System.Windows;
using Dissonance.Settings.Interfaces;
using Dissonance.Settings.SettingsClasses;
using Microsoft.Extensions.DependencyInjection;


namespace Dissonance
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static ServiceProvider ServiceProvider { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            ServiceProvider = serviceCollection.BuildServiceProvider();

            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IAppSettings, AppSettings>();
            services.AddScoped<IUserSettings, UserSettings>();
            services.AddSingleton<IDefaultSettings, DefaultSettings>();

            services.AddTransient<MainWindow>();
        }
    }

}
