using AxizGeo.Domain.Enums;
using AxizGeo.Domain.States;

namespace AxizGeo.Tests;

public class AlertStateTests
{
    [Fact]
    public void NewAlert_CanTransitionTo_InReview_Then_Resolved()
    {
        var state = AlertState.From(AlertStatus.New);

        var inReview = state.StartReview();
        Assert.Equal(AlertStatus.InReview, inReview.Status);

        var resolved = inReview.Resolve();
        Assert.Equal(AlertStatus.Resolved, resolved.Status);
    }

    [Fact]
    public void ResolvedAlert_ThrowsOn_StartReview()
    {
        var state = AlertState.From(AlertStatus.Resolved);

        Assert.Throws<InvalidAlertTransitionException>(() => state.StartReview());
    }
}
