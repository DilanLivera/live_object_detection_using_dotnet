using Microsoft.AspNetCore.Components.Server.Circuits;

namespace UI.Infrastructure;

public class CircuitHandlerService : CircuitHandler
{
    public event EventHandler<Circuit>? CircuitClosed;
    public event EventHandler<Circuit>? CircuitOpened;

    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        CircuitOpened?.Invoke(sender: this, circuit);

        return Task.CompletedTask;
    }

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        CircuitClosed?.Invoke(sender: this, circuit);

        return Task.CompletedTask;
    }

    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        CircuitClosed?.Invoke(sender: this, circuit);

        return Task.CompletedTask;
    }

    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        CircuitOpened?.Invoke(sender: this, circuit);

        return Task.CompletedTask;
    }
}