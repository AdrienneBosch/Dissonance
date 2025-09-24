using System;

namespace Dissonance.ViewModels
{
        public class NavigationSectionViewModel
        {
                public NavigationSectionViewModel (
                        string key,
                        string title,
                        string description,
                        string bannerTitle,
                        string bannerSubtitle,
                        object contentViewModel,
                        bool showSettingsControls = false )
                {
                        if ( string.IsNullOrWhiteSpace ( key ) )
                                throw new ArgumentException ( "Section key cannot be null or whitespace.", nameof ( key ) );

                        if ( string.IsNullOrWhiteSpace ( title ) )
                                throw new ArgumentException ( "Section title cannot be null or whitespace.", nameof ( title ) );

                        if ( contentViewModel is null )
                                throw new ArgumentNullException ( nameof ( contentViewModel ) );

                        Key = key;
                        Title = title;
                        Description = description ?? string.Empty;
                        BannerTitle = bannerTitle ?? title;
                        BannerSubtitle = bannerSubtitle ?? string.Empty;
                        ContentViewModel = contentViewModel;
                        ShowSettingsControls = showSettingsControls;
                }

                public string Key { get; }

                public string Title { get; }

                public string Description { get; }

                public string BannerTitle { get; }

                public string BannerSubtitle { get; }

                public object ContentViewModel { get; }

                public bool ShowSettingsControls { get; }
        }
}
