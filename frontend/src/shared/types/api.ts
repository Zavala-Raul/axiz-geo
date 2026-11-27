export type Uuid = string;
export type IsoDate = string; // ISO 8601 in UTC, e.g. "2026-12-01T18:30:00Z"

/**
 * GeoJSON standard (SRID 4326): coordinates are always [longitude, latitude].
 * Nuevo Laredo center: [-99.5075, 27.4763]
 */
export interface GeoJsonPoint {
  type: 'Point';
  coordinates: [number, number];
}

export interface GeoJsonLineString {
  type: 'LineString';
  coordinates: [number, number][];
}

export interface GeoJsonPolygon {
  type: 'Polygon';
  coordinates: [number, number][][];
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export type Role = 'Admin' | 'Monitor' | 'Operator';
export type TripStatus = 'Scheduled' | 'InProgress' | 'Completed' | 'Cancelled';
export type GeofenceType = 'Circle' | 'Polygon';
export type GeofenceKind = 'Allowed' | 'Restricted';
export type AlertType = 'Deviation' | 'NoSignal' | 'UnplannedStop' | 'RestrictedZone';
export type AlertStatus = 'New' | 'InReview' | 'Resolved' | 'Dismissed';
export type AlertSeverity = 'Low' | 'Medium' | 'High';
export type LiveStatus = 'OnRoute' | 'Stopped' | 'Deviated' | 'NoSignal' | 'Idle';

/**
 * RFC 9457 ProblemDetails standard error response
 */
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  errors?: Record<string, string[]>;
  traceId?: string;
}

export interface HealthResponse {
  status: string;
  database?: string;
  service?: string;
}

export interface UnitDto {
  id: Uuid;
  economicNumber: string;
  plates: string;
  brand: string;
  model: string;
  year: number;
  heightMeters?: number;
  weightTons?: number;
  isActive: boolean;
}

export type CreateUnitRequest = Omit<UnitDto, 'id' | 'isActive'>;
export type UpdateUnitRequest = Omit<UnitDto, 'id'>;

export interface OperatorDto {
  id: Uuid;
  fullName: string;
  licenseNumber: string;
  phone: string;
  email: string;
  userId: Uuid;
  isActive: boolean;
}

export interface CreateOperatorRequest {
  fullName: string;
  licenseNumber: string;
  phone: string;
  email: string;
  password: string;
}

export interface UpdateOperatorRequest {
  fullName: string;
  licenseNumber: string;
  phone: string;
  isActive: boolean;
}

export interface RouteDto {
  id: Uuid;
  name: string;
  waypoints: GeoJsonPoint[];
  path: GeoJsonLineString;
  corridorMeters: number;
  distanceMeters: number;
  durationSeconds: number;
  isActive: boolean;
  createdAt: IsoDate;
  createdBy: string;
}

export interface GeofenceDto {
  id: Uuid;
  name: string;
  type: GeofenceType;
  kind: GeofenceKind;
  geometry: GeoJsonPoint | GeoJsonPolygon;
  radiusMeters?: number;
  color?: string;
  isActive: boolean;
  createdAt: IsoDate;
}

export type UpsertGeofenceRequest = Omit<GeofenceDto, 'id' | 'createdAt'>;

export interface TripDto {
  id: Uuid;
  unit: { id: Uuid; economicNumber: string };
  operator: { id: Uuid; fullName: string };
  route: { id: Uuid; name: string };
  status: TripStatus;
  scheduledStart: IsoDate;
  startedAt?: IsoDate;
  endedAt?: IsoDate;
  lastPositionAt?: IsoDate;
  notes?: string;
}

export interface CreateTripRequest {
  unitId: Uuid;
  operatorId: Uuid;
  routeId: Uuid;
  scheduledStart: IsoDate;
  notes?: string;
}

export interface PositionInput {
  id: Uuid;
  tripId: Uuid;
  location: GeoJsonPoint;
  accuracyMeters: number;
  speedKmh?: number;
  heading?: number;
  capturedAt: IsoDate;
  source: 'Simulator' | 'BrowserGps';
}

export interface TelemetryBatchRequest {
  positions: PositionInput[];
}

export interface TelemetryBatchResponse {
  accepted: number;
  duplicates: number;
  rejected: { id: Uuid; reason: string }[];
  alertsRaised: number;
}

export interface LiveUnitDto {
  unitId: Uuid;
  economicNumber: string;
  operatorName?: string;
  tripId?: Uuid;
  routeName?: string;
  liveStatus: LiveStatus;
  location?: GeoJsonPoint;
  speedKmh?: number;
  heading?: number;
  accuracyMeters?: number;
  capturedAt?: IsoDate;
  openAlerts: number;
}

export interface DashboardSummaryDto {
  activeTrips: number;
  unitsOnRoute: number;
  unitsDeviated: number;
  unitsNoSignal: number;
  openAlerts: number;
}

export interface AlertDto {
  id: Uuid;
  type: AlertType;
  severity: AlertSeverity;
  status: AlertStatus;
  message: string;
  unit: { id: Uuid; economicNumber: string };
  trip: { id: Uuid; routeName: string };
  operatorName: string;
  location?: GeoJsonPoint;
  geofenceId?: Uuid;
  isHistorical: boolean;
  createdAt: IsoDate;
  attendedBy?: string;
  attendedAt?: IsoDate;
  resolution?: string;
}
