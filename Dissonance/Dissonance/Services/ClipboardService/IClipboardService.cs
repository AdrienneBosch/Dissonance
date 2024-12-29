using System;

namespace Dissonance.Services.ClipboardService
{
	public interface IClipboardService
	{
		string? GetClipboardText ( );

		bool IsTextAvailable ( );
	}
}