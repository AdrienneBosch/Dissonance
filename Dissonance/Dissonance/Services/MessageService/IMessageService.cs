using System;

namespace Dissonance.Services.MessageService
{
	public interface IMessageService
	{
		void DissonanceMessageBoxShowInfo ( string title, string message );
		void DissonanceMessageBoxShowWarning ( string title, string message );
		void DissonanceMessageBoxShowError ( string title, string message, Exception ex = null );
	}
}
