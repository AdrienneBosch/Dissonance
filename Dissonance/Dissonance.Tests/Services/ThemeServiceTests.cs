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
                                WpfTestHelper.EnsureApplication();

                                var service = new ThemeService();
                                var application = Application.Current!;
                                application.Resources.MergedDictionaries.Clear();

                                var baseThemeUri = new Uri("pack://application:,,,/Dissonance;component/Resources/Themes/BaseTheme.xaml", UriKind.Absolute);
                                var darkThemeUri = new Uri("pack://application:,,,/Dissonance;component/Resources/Themes/DarkTheme.xaml", UriKind.Absolute);
                                var lightThemeUri = new Uri("pack://application:,,,/Dissonance;component/Resources/Themes/LightTheme.xaml", UriKind.Absolute);

                                static bool UriEquals(Uri? actual, Uri expected)
                                {
                                        return actual != null
                                                && Uri.Compare(actual, expected, UriComponents.AbsoluteUri, UriFormat.SafeUnescaped, StringComparison.OrdinalIgnoreCase) == 0;
                                }

                                service.ApplyTheme(AppTheme.Dark);

                                Assert.Equal(AppTheme.Dark, service.CurrentTheme);
                                Assert.Contains(application.Resources.MergedDictionaries, rd => UriEquals(rd.Source, baseThemeUri));
                                Assert.Contains(application.Resources.MergedDictionaries, rd => UriEquals(rd.Source, darkThemeUri));
                                Assert.DoesNotContain(application.Resources.MergedDictionaries, rd => UriEquals(rd.Source, lightThemeUri));

                                service.ApplyTheme(AppTheme.Light);

                                Assert.Equal(AppTheme.Light, service.CurrentTheme);
                                Assert.Contains(application.Resources.MergedDictionaries, rd => UriEquals(rd.Source, baseThemeUri));
                                Assert.Contains(application.Resources.MergedDictionaries, rd => UriEquals(rd.Source, lightThemeUri));
                                Assert.DoesNotContain(application.Resources.MergedDictionaries, rd => UriEquals(rd.Source, darkThemeUri));
                        });
                }
        }
}
