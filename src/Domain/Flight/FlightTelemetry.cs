using System;

namespace FSChecklist.Domain.Flight
{
    internal sealed class FlightTelemetry
    {
        public DateTimeOffset Timestamp { get; init; }
        public double IndicatedAirspeedKnots { get; init; }
        public double GroundSpeedKnots { get; init; }
        public double IndicatedAltitudeFeet { get; init; }
        public double RadioAltitudeFeet { get; init; }
        public double VerticalSpeedFeetPerMinute { get; init; }
        public bool IsOnGround { get; init; }
        public double EngineOneN1Percent { get; init; }
        public double EngineTwoN1Percent { get; init; }
        public double EngineOneReversePercent { get; init; }
        public double EngineTwoReversePercent { get; init; }
        public double LeftSpoilerPercent { get; init; }
        public double RightSpoilerPercent { get; init; }
        public double LeftBrakePosition { get; init; }
        public double RightBrakePosition { get; init; }
        public bool AutobrakesActive { get; init; }
        public double AirlineV1Knots { get; init; }
        public double FenixV1Knots { get; init; }

        public double V1Knots
        {
            get
            {
                if (IsValidV1(FenixV1Knots)) return FenixV1Knots;
                return IsValidV1(AirlineV1Knots) ? AirlineV1Knots : 0D;
            }
        }

        private static bool IsValidV1(double value)
        {
            return value >= 80D && value <= 220D;
        }
    }
}
