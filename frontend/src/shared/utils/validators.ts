/**
 * Validaciones operativas adaptadas de U5/Proyecto/Frontend/js/utils/validators.js
 * y alineadas con las reglas de docs/api-contract.md
 */

export const OPERATIONAL_LIMITS = {
  MIN_NAME_LENGTH: 3,
  /** Margen del prototipo v0.2 (20m-200m) y contrato de API v1 (25m-2000m) */
  CORRIDOR_MIN_METERS: 25,
  CORRIDOR_MAX_METERS: 2000,
  CORRIDOR_DEFAULT_METERS: 50,
  GEOFENCE_RADIUS_MIN_METERS: 10,
  GEOFENCE_RADIUS_MAX_METERS: 20000,
  ALERT_RESOLUTION_MIN_CHARS: 5,
  NO_SIGNAL_THRESHOLD_MINUTES: 3,
  TELEMETRY_INTERVAL_SECONDS: 30,
} as const;

export function validateEntityName(name: string): boolean {
  return name.trim().length >= OPERATIONAL_LIMITS.MIN_NAME_LENGTH;
}

export function validateCorridorMeters(meters: number): boolean {
  return (
    Number.isFinite(meters) &&
    meters >= OPERATIONAL_LIMITS.CORRIDOR_MIN_METERS &&
    meters <= OPERATIONAL_LIMITS.CORRIDOR_MAX_METERS
  );
}

export function validateGeofenceRadiusMeters(meters: number): boolean {
  return (
    Number.isFinite(meters) &&
    meters >= OPERATIONAL_LIMITS.GEOFENCE_RADIUS_MIN_METERS &&
    meters <= OPERATIONAL_LIMITS.GEOFENCE_RADIUS_MAX_METERS
  );
}

/**
 * Regla operativa: toda resolución o descarte de alerta requiere motivo obligatorio (>= 5 caracteres).
 */
export function validateAlertResolutionReason(reason: string): boolean {
  return reason.trim().length >= OPERATIONAL_LIMITS.ALERT_RESOLUTION_MIN_CHARS;
}
