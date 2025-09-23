using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Dissonance.Services.ThemeService
{
        internal class ThemeService : IThemeService
        {
                private static readonly IReadOnlyCollection<AppTheme> ThemeValues = Array.AsReadOnly(new[]
                {
                        AppTheme.Light,
                        AppTheme.Dark
                });

                private readonly object _syncLock = new object ( );
                private readonly Uri _baseThemeUri = new Uri ( "pack://application:,,,/Dissonance;component/Resources/Themes/BaseTheme.xaml", UriKind.Absolute );
                private readonly Uri _darkThemeUri = new Uri ( "pack://application:,,,/Dissonance;component/Resources/Themes/DarkTheme.xaml", UriKind.Absolute );
                private readonly Uri _lightThemeUri = new Uri ( "pack://application:,,,/Dissonance;component/Resources/Themes/LightTheme.xaml", UriKind.Absolute );

                public IReadOnlyCollection<AppTheme> AvailableThemes => ThemeValues;

                public AppTheme CurrentTheme { get; private set; } = AppTheme.Light;

                public void ApplyTheme ( AppTheme theme )
                {
                        var application = Application.Current ?? throw new InvalidOperationException ( "Application resources are unavailable." );

                        if ( application.Dispatcher.CheckAccess ( ) )
                        {
                                ApplyThemeInternal ( application.Resources, theme );
                        }
                        else
                        {
                                application.Dispatcher.Invoke ( ( ) => ApplyThemeInternal ( application.Resources, theme ) );
                        }
                }

                private void ApplyThemeInternal ( ResourceDictionary resources, AppTheme theme )
                {
                        lock ( _syncLock )
                        {
                                EnsureDictionary ( resources, _baseThemeUri );

                                var themeUri = theme == AppTheme.Dark ? _darkThemeUri : _lightThemeUri;

                                RemoveDictionary ( resources, _lightThemeUri );
                                RemoveDictionary ( resources, _darkThemeUri );

                                resources.MergedDictionaries.Add ( new ResourceDictionary { Source = themeUri } );

                                CurrentTheme = theme;
                        }
                }

                private static void EnsureDictionary ( ResourceDictionary resources, Uri source )
                {
                        if ( resources.MergedDictionaries.Any ( dictionary => dictionary.Source == source ) )
                                return;

                        resources.MergedDictionaries.Add ( new ResourceDictionary { Source = source } );
                }

                private static void RemoveDictionary ( ResourceDictionary resources, Uri source )
                {
                        var dictionary = resources.MergedDictionaries.FirstOrDefault ( rd => rd.Source == source );
                        if ( dictionary != null )
                        {
                                resources.MergedDictionaries.Remove ( dictionary );
                        }
                }
        }
}
