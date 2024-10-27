using System;

namespace Dissonance.Services.ClipboardService
{
	internal interface IClipboardService
	{
		/// <summary>
		/// Reads and returns the current text content from the clipboard.
		/// </summary>
		/// <returns>Text from clipboard, or null if unavailable.</returns>
		string GetClipboardText ( );

		/// <summary>
		/// Checks if the clipboard currently contains text.
		/// </summary>
		/// <returns>True if text is available, false otherwise.</returns>
		bool IsTextAvailable ( );
	}
}
