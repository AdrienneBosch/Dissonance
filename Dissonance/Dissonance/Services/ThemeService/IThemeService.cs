using System.Collections.Generic;

namespace Dissonance.Services.ThemeService
{
        public interface IThemeService
        {
                IReadOnlyCollection<AppTheme> AvailableThemes { get; }

                AppTheme CurrentTheme { get; }

                void ApplyTheme ( AppTheme theme );
        }
}
