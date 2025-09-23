using Dissonance.Infrastructure.Commands;

namespace Dissonance.Tests.Infrastructure
{
        public class RelayCommandTests
        {
                [Fact]
                public void RelayCommand_Execute_InvokesDelegate()
                {
                        bool executed = false;
                        var command = new RelayCommand(_ => executed = true);

                        command.Execute(null);

                        Assert.True(executed);
                }

                [Fact]
                public void RelayCommand_CanExecute_UsesPredicate()
                {
                        var command = new RelayCommand(_ => { }, _ => false);

                        Assert.False(command.CanExecute(null));

                        command = new RelayCommand(_ => { }, _ => true);
                        Assert.True(command.CanExecute(null));
                }

                [Fact]
                public void RelayCommand_RaiseCanExecuteChanged_RaisesEvent()
                {
                        var command = new RelayCommand(_ => { });
                        bool eventRaised = false;

                        command.CanExecuteChanged += (_, _) => eventRaised = true;
                        command.RaiseCanExecuteChanged();

                        Assert.True(eventRaised);
                }

                [Fact]
                public void RelayCommandNoParam_Execute_InvokesDelegate()
                {
                        bool executed = false;
                        var command = new RelayCommandNoParam(() => executed = true);

                        command.Execute(null);

                        Assert.True(executed);
                }

                [Fact]
                public void RelayCommandNoParam_CanExecute_UsesPredicate()
                {
                        var command = new RelayCommandNoParam(() => { }, () => false);

                        Assert.False(command.CanExecute(null));

                        command = new RelayCommandNoParam(() => { }, () => true);
                        Assert.True(command.CanExecute(null));
                }

                [Fact]
                public void RelayCommandNoParam_RaiseCanExecuteChanged_RaisesEvent()
                {
                        var command = new RelayCommandNoParam(() => { });
                        bool eventRaised = false;

                        command.CanExecuteChanged += (_, _) => eventRaised = true;
                        command.RaiseCanExecuteChanged();

                        Assert.True(eventRaised);
                }
        }
}
