using System.Globalization;
using System.Net;
using System.Net.Mail;
using AxizGeo.Application.Abstractions;
using AxizGeo.Application.DTOs;
using AxizGeo.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AxizGeo.Infrastructure.Notifications;

public sealed class EmailNotificationOptions
{
    public const string SectionName = "Notifications:Email";

    public bool Enabled { get; set; } = false;
    public string SmtpHost { get; set; } = "smtp.gmail.com";
    public int SmtpPort { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "Axiz Geo Monitoreo";
    public string[] Recipients { get; set; } = [];
    public bool OnlyHighSeverity { get; set; } = false;
}

/// <summary>
/// Canal de notificación por correo electrónico SMTP enchufable a través de IAlertNotificationChannel.
/// </summary>
public sealed class EmailAlertNotificationChannel(
    IOptions<EmailNotificationOptions> options,
    ILogger<EmailAlertNotificationChannel> logger) : IAlertNotificationChannel
{
    private readonly EmailNotificationOptions _options = options.Value;

    public async Task SendAlertAsync(AlertDto alert, CancellationToken ct = default)
    {
        if (!_options.Enabled)
        {
            logger.LogDebug("Canal SMTP deshabilitado; se omite envío de correo para alerta {AlertId}.", alert.Id);
            return;
        }

        if (_options.OnlyHighSeverity && alert.Severity != AlertSeverity.High)
            return;

        if (string.IsNullOrWhiteSpace(_options.SmtpHost) ||
            string.IsNullOrWhiteSpace(_options.FromEmail) ||
            _options.Recipients.Length == 0)
        {
            logger.LogWarning("Notifications:Email está habilitado pero faltan SmtpHost, FromEmail o Recipients.");
            return;
        }

        var subject = BuildSubject(alert);
        var htmlBody = BuildHtmlBody(alert);

        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromEmail, _options.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };

        foreach (var recipient in _options.Recipients.Where(r => !string.IsNullOrWhiteSpace(r)))
        {
            message.To.Add(recipient.Trim());
        }

        if (message.To.Count == 0)
            return;

        using var client = new SmtpClient(_options.SmtpHost, _options.SmtpPort)
        {
            EnableSsl = _options.UseSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        if (!string.IsNullOrWhiteSpace(_options.Username))
        {
            client.Credentials = new NetworkCredential(_options.Username, _options.Password);
        }

        await client.SendMailAsync(message, ct);
        logger.LogInformation(
            "Correo de alerta {AlertId} ({Type}) enviado a {Count} destinatario(s).",
            alert.Id,
            alert.Type,
            message.To.Count);
    }

    public static string BuildSubject(AlertDto alert)
    {
        var prefix = alert.IsHistorical ? "[HISTÓRICA] " : string.Empty;
        return $"{prefix}[Axiz Geo · {alert.Severity}] {alert.Unit.EconomicNumber} — {alert.Type}";
    }

    public static string BuildHtmlBody(AlertDto alert)
    {
        var coordsHtml = "Sin coordenadas asociadas";
        if (alert.Location?.Coordinates is { Length: >= 2 })
        {
            var lng = alert.Location.Coordinates[0].ToString("F6", CultureInfo.InvariantCulture);
            var lat = alert.Location.Coordinates[1].ToString("F6", CultureInfo.InvariantCulture);
            var osmUrl = $"https://www.openstreetmap.org/?mlat={lat}&mlon={lng}#map=15/{lat}/{lng}";
            coordsHtml = $"<a href=\"{osmUrl}\" style=\"color:#213D71;\">{lat}, {lng} (Ver en OpenStreetMap)</a>";
        }

        var localTimeLabel = FormatBorderTime(alert.CreatedAt);
        var encodedMessage = WebUtility.HtmlEncode(alert.Message);
        var encodedUnit = WebUtility.HtmlEncode(alert.Unit.EconomicNumber);
        var encodedOperator = WebUtility.HtmlEncode(alert.OperatorName);
        var encodedRoute = WebUtility.HtmlEncode(alert.Trip.RouteName);

        return $"""
            <div style="font-family:'Segoe UI',Arial,sans-serif;max-width:600px;border:1px solid #d1d5db;border-radius:8px;overflow:hidden;">
              <div style="background:#213D71;color:#ffffff;padding:16px 20px;">
                <h2 style="margin:0;font-size:18px;letter-spacing:0.5px;">AXIZ GEO · Alerta Operativa</h2>
              </div>
              <div style="padding:20px;color:#1f2937;line-height:1.5;">
                <p style="margin-top:0;font-size:15px;"><strong>{encodedMessage}</strong></p>
                <table style="width:100%;border-collapse:collapse;font-size:14px;">
                  <tr><td style="padding:6px 0;color:#6b7280;">Unidad:</td><td><strong>{encodedUnit}</strong></td></tr>
                  <tr><td style="padding:6px 0;color:#6b7280;">Operador:</td><td>{encodedOperator}</td></tr>
                  <tr><td style="padding:6px 0;color:#6b7280;">Ruta autorizada:</td><td>{encodedRoute}</td></tr>
                  <tr><td style="padding:6px 0;color:#6b7280;">Severidad / Tipo:</td><td>{alert.Severity} / {alert.Type}</td></tr>
                  <tr><td style="padding:6px 0;color:#6b7280;">Hora Nuevo Laredo:</td><td>{localTimeLabel}</td></tr>
                  <tr><td style="padding:6px 0;color:#6b7280;">Ubicación:</td><td>{coordsHtml}</td></tr>
                </table>
              </div>
              <div style="background:#f3f4f6;padding:12px 20px;font-size:12px;color:#6b7280;">
                Esta alerta requiere verificación humana en el Panel de Monitoreo de Axiz Geo.
              </div>
            </div>
            """;
    }

    private static string FormatBorderTime(DateTime utcDateTime)
    {
        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById("America/Matamoros");
            var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc), tz);
            return $"{local:yyyy-MM-dd HH:mm:ss} (America/Matamoros)";
        }
        catch
        {
            return $"{utcDateTime:yyyy-MM-dd HH:mm:ss} UTC";
        }
    }
}
