using System;

using Xunit;

namespace Dissonance.Tests.TestInfrastructure
{
        internal sealed class WindowsFactAttribute : FactAttribute
        {
                public WindowsFactAttribute()
                {
                        if (!OperatingSystem.IsWindows())
                        {
                                Skip = "This test requires Windows.";
                        }
                }
        }
}
