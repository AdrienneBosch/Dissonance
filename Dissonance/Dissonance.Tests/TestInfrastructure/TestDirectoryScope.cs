using System;
using System.IO;

namespace Dissonance.Tests.TestInfrastructure
{
        internal sealed class TestDirectoryScope : IDisposable
        {
                private readonly string _originalDirectory;

                public string DirectoryPath { get; }

                public TestDirectoryScope()
                {
                        DirectoryPath = Path.Combine(Path.GetTempPath(), "DissonanceTests", Guid.NewGuid().ToString("N"));
                        Directory.CreateDirectory(DirectoryPath);

                        _originalDirectory = Directory.GetCurrentDirectory();
                        Directory.SetCurrentDirectory(DirectoryPath);
                }

                public void Dispose()
                {
                        Directory.SetCurrentDirectory(_originalDirectory);

                        try
                        {
                                if (Directory.Exists(DirectoryPath))
                                        Directory.Delete(DirectoryPath, recursive: true);
                        }
                        catch
                        {
                                // Ignore cleanup errors to avoid hiding test results.
                        }
                }
        }
}
