using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Dissonance.Services.DocumentService
{
        public interface IDocumentTextExtractor
        {
                IReadOnlyCollection<string> SupportedFileExtensions { get; }

                Task<string?> ExtractTextAsync ( string filePath, CancellationToken cancellationToken = default );
        }
}
