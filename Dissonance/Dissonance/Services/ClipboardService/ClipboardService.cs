using System.Media;
using System.Windows;

using NLog;

namespace Dissonance.Services.ClipboardService
{
	internal class ClipboardService : IClipboardService
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );

		public string? GetClipboardText ( )
		{
			try
			{
				if ( IsTextAvailable ( ) )
				{
					return Clipboard.GetText ( );
				}
			}
			catch ( Exception ex )
			{
				Logger.Error ( ex, "Error accessing clipboard content." );
				//TODO: Play error sound and show error message in UI
			}

			return null;
		}

		public bool IsTextAvailable ( )
		{
			try
			{
				return Clipboard.ContainsText ( );
			}
			catch ( Exception ex )
			{
				Logger.Error ( ex, "Error checking clipboard content." );
				return false;
			}
		}
	}
}