using System.Windows;

using Dissonance.Services.ClipboardService;

using Microsoft.Extensions.Logging;

public class ClipboardService : IClipboardService
{
	private readonly ILogger<ClipboardService> _logger;

	public ClipboardService ( ILogger<ClipboardService> logger )
	{
		_logger = logger ?? throw new ArgumentNullException ( nameof ( logger ) );
	}

	public string? GetClipboardText ( )
	{
		try
		{
			if ( IsTextAvailable ( ) )
			{
				var text = Clipboard.GetText();
				_logger.LogInformation ( "Clipboard text retrieved." );
				return text;
			}
		}
		catch ( Exception ex )
		{
			_logger.LogError ( ex, "Error accessing clipboard content." );

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
			_logger.LogError ( ex, "Error checking clipboard content." );
			return false;
		}
	}
}