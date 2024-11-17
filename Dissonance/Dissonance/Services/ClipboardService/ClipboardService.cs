using System;
using System.Windows;  // For accessing clipboard

using NLog;

namespace Dissonance.Services.ClipboardService
{
	internal class ClipboardService : IClipboardService
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		/// <summary>
		/// Gets the text from the clipboard if available.
		/// </summary>
		/// <returns>Text from the clipboard, or null if unavailable or non-text content is present.</returns>
		public string GetClipboardText ( )
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
			}

			return null;  // Return null if text is unavailable or an error occurs
		}

		/// <summary>
		/// Checks whether the clipboard currently contains text data.
		/// </summary>
		/// <returns>True if text is available, false otherwise.</returns>
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
