using System;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;

using Dissonance;

namespace Dissonance.Tests.TestInfrastructure
{
        internal static class WpfTestHelper
        {
                private static readonly object SyncLock = new();

                public static void EnsureApplication()
                {
                        lock (SyncLock)
                        {
                                if (Application.Current == null)
                                {
                                        new Application();
                                }

                                SetResourceAssembly(typeof(App).Assembly);
                        }
                }

                public static void ProcessPendingEvents()
                {
                        if (Application.Current == null)
                        {
                                throw new InvalidOperationException("An Application must exist before processing events.");
                        }

                        var frame = new DispatcherFrame();
                        Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => frame.Continue = false));
                        Dispatcher.PushFrame(frame);
                }

                private static void SetResourceAssembly(Assembly assembly)
                {
                        var resourceAssemblyProperty = typeof(Application).GetProperty("ResourceAssembly", BindingFlags.NonPublic | BindingFlags.Static);
                        resourceAssemblyProperty?.SetValue(null, assembly);
                }
        }
}
