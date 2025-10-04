using System;
using Dissonance.Services.ClipboardService;
using Microsoft.Extensions.Logging;

namespace Dissonance.Managers
{
        public class ClipboardManager
        {
                private readonly IClipboardService _clipboardService;
                private readonly ILogger<ClipboardManager> _logger;

                public ClipboardManager ( IClipboardService clipboardService, ILogger<ClipboardManager> logger )
                {
                        _clipboardService = clipboardService ?? throw new ArgumentNullException ( nameof ( clipboardService ) );
                        _logger = logger ?? throw new ArgumentNullException ( nameof ( logger ) );
                }

                public string? GetValidatedClipboardText ( )
                {
                        var text = _clipboardService.GetClipboardText();
                        if ( string.IsNullOrWhiteSpace ( text ) )
                        {
                                _logger.LogWarning ( "Clipboard is empty or contains invalid text." );
                                return null;
                        }

                        return text;
                }

                public void Initialize ( )
                {
                        _logger.LogInformation ( "ClipboardManager initialized." );
                }
        }
}

