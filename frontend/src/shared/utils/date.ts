import type { IsoDate } from '../types/api';

export const MATAMOROS_TIMEZONE = 'America/Matamoros';

/**
 * Nuevo Laredo default geographic coordinates in GeoJSON [longitude, latitude] order (SRID 4326).
 */
export const NUEVO_LAREDO_CENTER: [number, number] = [-99.5075, 27.4763];

const dateTimeFormatter = new Intl.DateTimeFormat('es-MX', {
  timeZone: MATAMOROS_TIMEZONE,
  year: 'numeric',
  month: '2-digit',
  day: '2-digit',
  hour: '2-digit',
  minute: '2-digit',
  second: '2-digit',
  hour12: false,
});

const timeOnlyFormatter = new Intl.DateTimeFormat('es-MX', {
  timeZone: MATAMOROS_TIMEZONE,
  hour: '2-digit',
  minute: '2-digit',
  second: '2-digit',
  hour12: false,
});

/**
 * Converts a Date, timestamp, or local date string to an ISO 8601 UTC string (with trailing Z)
 * to send to the API according to docs/api-contract.md.
 */
export function toUtcIsoString(input: Date | string | number = new Date()): IsoDate {
  const date = input instanceof Date ? input : new Date(input);
  if (Number.isNaN(date.getTime())) {
    throw new Error('Fecha inválida para convertir a ISO 8601 UTC.');
  }
  return date.toISOString();
}

/**
 * Formats an ISO 8601 UTC date string into America/Matamoros local date and time.
 */
export function formatMatamorosDateTime(isoUtc?: IsoDate | null): string {
  if (!isoUtc) return '—';
  const parsed = new Date(isoUtc);
  if (Number.isNaN(parsed.getTime())) return '—';
  return dateTimeFormatter.format(parsed);
}

/**
 * Formats an ISO 8601 UTC date string into America/Matamoros local time (HH:mm:ss).
 */
export function formatMatamorosTime(isoUtc?: IsoDate | null): string {
  if (!isoUtc) return '—';
  const parsed = new Date(isoUtc);
  if (Number.isNaN(parsed.getTime())) return '—';
  return timeOnlyFormatter.format(parsed);
}

/**
 * Computes human-readable elapsed time in Spanish (e.g. "hace 45 s", "hace 3 min").
 */
export function formatRelativeTime(isoUtc?: IsoDate | null, nowMs: number = Date.now()): string {
  if (!isoUtc) return 'Sin reporte';
  const parsed = new Date(isoUtc);
  const timestamp = parsed.getTime();
  if (Number.isNaN(timestamp)) return 'Sin reporte';

  const diffSeconds = Math.max(0, Math.floor((nowMs - timestamp) / 1000));
  if (diffSeconds < 60) {
    return `hace ${diffSeconds} s`;
  }
  const diffMinutes = Math.floor(diffSeconds / 60);
  if (diffMinutes < 60) {
    return `hace ${diffMinutes} min`;
  }
  const diffHours = Math.floor(diffMinutes / 60);
  if (diffHours < 24) {
    return `hace ${diffHours} h`;
  }
  const diffDays = Math.floor(diffHours / 24);
  return `hace ${diffDays} d`;
}
