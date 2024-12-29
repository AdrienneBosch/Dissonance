using Dissonance.ViewModels;

using Microsoft.Extensions.Logging;

namespace Dissonance.Services.MessageService
{
	public class MessageService : IMessageService
	{
		private readonly ILogger<MessageService> _logger;

		public MessageService ( ILogger<MessageService> logger )
		{
			_logger = logger ?? throw new ArgumentNullException ( nameof ( logger ) );
		}

		public void DissonanceMessageBoxShowError ( string title, string message, Exception ex = null )
		{
			if ( ex != null )
				_logger.LogError ( ex, message );
			else
				_logger.LogError ( message );

			DissonanceMessageBoxViewModel.Show ( title, message, showCancelButton: false );
		}

		public void DissonanceMessageBoxShowInfo ( string title, string message )
		{
			_logger.LogInformation ( message );
			DissonanceMessageBoxViewModel.Show ( title, message, showCancelButton: false );
		}

		public void DissonanceMessageBoxShowWarning ( string title, string message )
		{
			_logger.LogWarning ( message );
			DissonanceMessageBoxViewModel.Show ( title, message, showCancelButton: false );
		}
	}
}