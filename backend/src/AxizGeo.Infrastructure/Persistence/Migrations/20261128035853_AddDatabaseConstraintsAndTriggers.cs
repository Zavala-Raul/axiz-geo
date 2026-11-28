using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AxizGeo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDatabaseConstraintsAndTriggers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_trips_OperatorId",
                table: "trips");

            migrationBuilder.DropIndex(
                name: "IX_trips_UnitId",
                table: "trips");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "units",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "trips",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "routes",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "operators",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "geofences",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "alerts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddCheckConstraint(
                name: "CK_units_Year",
                table: "units",
                sql: "\"Year\" BETWEEN 1990 AND 2035");

            migrationBuilder.CreateIndex(
                name: "IX_trips_OperatorId_Active",
                table: "trips",
                column: "OperatorId",
                unique: true,
                filter: "\"Status\" = 'InProgress'");

            migrationBuilder.CreateIndex(
                name: "IX_trips_UnitId_Active",
                table: "trips",
                column: "UnitId",
                unique: true,
                filter: "\"Status\" = 'InProgress'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_routes_CorridorMeters",
                table: "routes",
                sql: "\"CorridorMeters\" BETWEEN 25 AND 2000");

            migrationBuilder.AddCheckConstraint(
                name: "CK_positions_AccuracyMeters",
                table: "positions",
                sql: "\"AccuracyMeters\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_geofences_RadiusMeters",
                table: "geofences",
                sql: "\"RadiusMeters\" IS NULL OR \"RadiusMeters\" > 0");

            migrationBuilder.CreateIndex(
                name: "IX_alerts_TripId_Type_Open",
                table: "alerts",
                columns: new[] { "TripId", "Type" },
                unique: true,
                filter: "\"Status\" IN ('New', 'InReview')");

            migrationBuilder.Sql("""
                UPDATE units SET "UpdatedAt" = "CreatedAt";
                UPDATE operators SET "UpdatedAt" = "CreatedAt";
                UPDATE routes SET "UpdatedAt" = "CreatedAt";
                UPDATE geofences SET "UpdatedAt" = "CreatedAt";
                UPDATE trips SET "UpdatedAt" = "CreatedAt";
                UPDATE alerts SET "UpdatedAt" = "CreatedAt";

                CREATE OR REPLACE FUNCTION fn_set_updated_at()
                RETURNS TRIGGER AS $$
                BEGIN
                    NEW."UpdatedAt" = NOW() AT TIME ZONE 'UTC';
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;

                CREATE TRIGGER trg_units_set_updated_at
                BEFORE UPDATE ON units
                FOR EACH ROW EXECUTE FUNCTION fn_set_updated_at();

                CREATE TRIGGER trg_operators_set_updated_at
                BEFORE UPDATE ON operators
                FOR EACH ROW EXECUTE FUNCTION fn_set_updated_at();

                CREATE TRIGGER trg_routes_set_updated_at
                BEFORE UPDATE ON routes
                FOR EACH ROW EXECUTE FUNCTION fn_set_updated_at();

                CREATE TRIGGER trg_geofences_set_updated_at
                BEFORE UPDATE ON geofences
                FOR EACH ROW EXECUTE FUNCTION fn_set_updated_at();

                CREATE TRIGGER trg_trips_set_updated_at
                BEFORE UPDATE ON trips
                FOR EACH ROW EXECUTE FUNCTION fn_set_updated_at();

                CREATE TRIGGER trg_alerts_set_updated_at
                BEFORE UPDATE ON alerts
                FOR EACH ROW EXECUTE FUNCTION fn_set_updated_at();

                CREATE OR REPLACE VIEW vw_trip_latest_position AS
                SELECT DISTINCT ON ("TripId")
                    "TripId",
                    "Id" AS "PositionId",
                    "Location",
                    "AccuracyMeters",
                    "SpeedKmh",
                    "Heading",
                    "CapturedAt",
                    "InsideCorridor"
                FROM positions
                ORDER BY "TripId", "CapturedAt" DESC;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP VIEW IF EXISTS vw_trip_latest_position;
                DROP TRIGGER IF EXISTS trg_alerts_set_updated_at ON alerts;
                DROP TRIGGER IF EXISTS trg_trips_set_updated_at ON trips;
                DROP TRIGGER IF EXISTS trg_geofences_set_updated_at ON geofences;
                DROP TRIGGER IF EXISTS trg_routes_set_updated_at ON routes;
                DROP TRIGGER IF EXISTS trg_operators_set_updated_at ON operators;
                DROP TRIGGER IF EXISTS trg_units_set_updated_at ON units;
                DROP FUNCTION IF EXISTS fn_set_updated_at();
                """);
            migrationBuilder.DropCheckConstraint(
                name: "CK_units_Year",
                table: "units");

            migrationBuilder.DropIndex(
                name: "IX_trips_OperatorId_Active",
                table: "trips");

            migrationBuilder.DropIndex(
                name: "IX_trips_UnitId_Active",
                table: "trips");

            migrationBuilder.DropCheckConstraint(
                name: "CK_routes_CorridorMeters",
                table: "routes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_positions_AccuracyMeters",
                table: "positions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_geofences_RadiusMeters",
                table: "geofences");

            migrationBuilder.DropIndex(
                name: "IX_alerts_TripId_Type_Open",
                table: "alerts");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "units");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "trips");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "routes");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "operators");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "geofences");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "alerts");

            migrationBuilder.CreateIndex(
                name: "IX_trips_OperatorId",
                table: "trips",
                column: "OperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_trips_UnitId",
                table: "trips",
                column: "UnitId");
        }
    }
}
