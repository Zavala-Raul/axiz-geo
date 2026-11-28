using AxizGeo.Application.DTOs;
using AxizGeo.Domain.Enums;
using AxizGeo.Infrastructure.Identity;
using AxizGeo.Infrastructure.Notifications;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AxizGeo.Tests;

public class RefreshTokenAndEmailTests
{
    [Fact]
    public void RefreshToken_IsActive_ChecksExpirationAndRevocation()
    {
        var now = DateTime.UtcNow;
        var token = new RefreshToken
        {
            UserId = Guid.NewGuid(),
            TokenHash = AuthService.HashToken("sample-raw-token"),
            CreatedAt = now,
            ExpiresAt = now.AddDays(7)
        };

        Assert.Equal(64, token.TokenHash.Length);
        Assert.True(token.IsActive(now));
        Assert.False(token.IsActive(now.AddDays(8)));

        token.RevokedAt = now.AddMinutes(5);
        Assert.False(token.IsActive(now.AddMinutes(6)));
    }

    [Fact]
    public async Task EmailChannel_NoOpsSafely_WhenDisabled()
    {
        var options = Options.Create(new EmailNotificationOptions { Enabled = false });
        var channel = new EmailAlertNotificationChannel(options, NullLogger<EmailAlertNotificationChannel>.Instance);

        var alert = new AlertDto(
            Guid.NewGuid(),
            AlertType.Deviation,
            AlertSeverity.High,
            AlertStatus.New,
            "Unidad T-101 fuera del corredor autorizado",
            new AlertUnitSummaryDto(Guid.NewGuid(), "T-101"),
            new AlertTripSummaryDto(Guid.NewGuid(), "Puente III -> Oradel"),
            "Carlos Méndez",
            new GeoJsonPointDto("Point", [-99.5540, 27.5550]),
            null,
            IsHistorical: true,
            DateTime.UtcNow,
            null,
            null,
            null);

        await channel.SendAlertAsync(alert);

        var subject = EmailAlertNotificationChannel.BuildSubject(alert);
        var html = EmailAlertNotificationChannel.BuildHtmlBody(alert);

        Assert.StartsWith("[HISTÓRICA]", subject);
        Assert.Contains("T-101", subject);
        Assert.Contains("openstreetmap.org", html);
    }
}
