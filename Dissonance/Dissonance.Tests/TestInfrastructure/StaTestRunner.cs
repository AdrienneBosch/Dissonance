using System;
using System.Threading;
using System.Runtime.ExceptionServices;

namespace Dissonance.Tests.TestInfrastructure
{
        internal static class StaTestRunner
        {
                public static void Run(Action action)
                {
                        if (action == null)
                                throw new ArgumentNullException(nameof(action));

                        ExceptionDispatchInfo? capturedException = null;

                        var thread = new Thread(() =>
                        {
                                try
                                {
                                        action();
                                }
                                catch (Exception ex)
                                {
                                        capturedException = ExceptionDispatchInfo.Capture(ex);
                                }
                        })
                        {
                                IsBackground = true
                        };

                        thread.SetApartmentState(ApartmentState.STA);
                        thread.Start();
                        thread.Join();

                        capturedException?.Throw();
                }
        }
}
