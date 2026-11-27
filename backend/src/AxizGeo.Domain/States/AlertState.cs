using AxizGeo.Domain.Enums;

namespace AxizGeo.Domain.States;

public sealed class InvalidAlertTransitionException(AlertStatus current, string action)
    : InvalidOperationException($"No se puede ejecutar '{action}' cuando la alerta está en estado '{current}'.");

/// <summary>
/// Patrón State: encapsula las transiciones válidas del ciclo de vida de una alerta.
/// </summary>
public abstract class AlertState
{
    public abstract AlertStatus Status { get; }

    public virtual AlertState StartReview() => throw new InvalidAlertTransitionException(Status, nameof(StartReview));
    public virtual AlertState Resolve() => throw new InvalidAlertTransitionException(Status, nameof(Resolve));
    public virtual AlertState Dismiss() => throw new InvalidAlertTransitionException(Status, nameof(Dismiss));

    public static AlertState From(AlertStatus status) => status switch
    {
        AlertStatus.New => new NewAlertState(),
        AlertStatus.InReview => new InReviewAlertState(),
        AlertStatus.Resolved => new ResolvedAlertState(),
        AlertStatus.Dismissed => new DismissedAlertState(),
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };
}

public sealed class NewAlertState : AlertState
{
    public override AlertStatus Status => AlertStatus.New;
    public override AlertState StartReview() => new InReviewAlertState();
    public override AlertState Dismiss() => new DismissedAlertState();
}

public sealed class InReviewAlertState : AlertState
{
    public override AlertStatus Status => AlertStatus.InReview;
    public override AlertState Resolve() => new ResolvedAlertState();
    public override AlertState Dismiss() => new DismissedAlertState();
}

public sealed class ResolvedAlertState : AlertState
{
    public override AlertStatus Status => AlertStatus.Resolved;
}

public sealed class DismissedAlertState : AlertState
{
    public override AlertStatus Status => AlertStatus.Dismissed;
}
