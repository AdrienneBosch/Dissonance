using System;

namespace Dissonance.Services.MessageService
{
        public interface IMessageService
        {
                void DissonanceMessageBoxShowError ( string title, string message, Exception ex = null );

                void DissonanceMessageBoxShowInfo ( string title, string message, TimeSpan? autoCloseDelay = null );

                void DissonanceMessageBoxShowWarning ( string title, string message );
        }
}
