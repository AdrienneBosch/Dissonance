using System;

namespace Dissonance.Services.ClipboardService
{
	internal interface IClipboardService
	{
		string? GetClipboardText ( );

		bool IsTextAvailable ( );
	}
}