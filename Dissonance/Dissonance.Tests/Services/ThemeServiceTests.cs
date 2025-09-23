using System;
using System.Windows;

using Dissonance.Services.ThemeService;
using Dissonance.Tests.TestInfrastructure;

namespace Dissonance.Tests.Services
{
        public class ThemeServiceTests
        {
                [Fact]
                public void AvailableThemes_ExposesLightAndDark()
                {
                        var service = new ThemeService();

                        Assert.Contains(AppTheme.Light, service.AvailableThemes);
                        Assert.Contains(AppTheme.Dark, service.AvailableThemes);
                }

                [WindowsFact]
                public void ApplyTheme_UpdatesApplicationResources()
                {
                        StaTestRunner.Run(() =>
                        {
                                if (Application.Current == null)
                                {
                                        new Application();
                                }

                                var service = new ThemeService();
                                var application = Application.Current!;
                                application.Resources.MergedDictionaries.Clear();

                                service.ApplyTheme(AppTheme.Dark);

                                Assert.Equal(AppTheme.Dark, service.CurrentTheme);
                                Assert.Contains(application.Resources.MergedDictionaries, rd => rd.Source == new Uri("Resources/Themes/BaseTheme.xaml", UriKind.Relative));
                                Assert.Contains(application.Resources.MergedDictionaries, rd => rd.Source == new Uri("Resources/Themes/DarkTheme.xaml", UriKind.Relative));
                                Assert.DoesNotContain(application.Resources.MergedDictionaries, rd => rd.Source == new Uri("Resources/Themes/LightTheme.xaml", UriKind.Relative));

                                service.ApplyTheme(AppTheme.Light);

                                Assert.Equal(AppTheme.Light, service.CurrentTheme);
                                Assert.Contains(application.Resources.MergedDictionaries, rd => rd.Source == new Uri("Resources/Themes/BaseTheme.xaml", UriKind.Relative));
                                Assert.Contains(application.Resources.MergedDictionaries, rd => rd.Source == new Uri("Resources/Themes/LightTheme.xaml", UriKind.Relative));
                                Assert.DoesNotContain(application.Resources.MergedDictionaries, rd => rd.Source == new Uri("Resources/Themes/DarkTheme.xaml", UriKind.Relative));
                        });
                }
        }
}
