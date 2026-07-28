using System;
using System.Collections.Generic;
using FSChecklist.Domain.Flight;

namespace FSChecklist.Features.FlightCallouts
{
    internal sealed class FlightCalloutEngine
    {
        private const double TakeoffArmSpeedKnots = 30D;
        private const double TakeoffPowerN1Percent = 60D;
        private const double SpoilerDeployedPercent = 20D;
        private const double ReverseDeployedPercent = 5D;
        private const double ManualBrakePosition = 3276D;

        private FlightTelemetry previous;
        private FlightPhase phase;
        private ThresholdSide tenThousandSide;
        private ThresholdSide twoThousandSide;
        private DateTimeOffset touchdownTime;
        private double lastValidV1;
        private bool takeoffArmed;
        private bool oneHundredCalled;
        private bool v1Called;
        private bool positiveClimbCalled;
        private bool spoilersCalled;
        private bool reverseCalled;
        private bool sixtyCalled;
        private bool manualBrakeCalled;

        public IReadOnlyList<FlightCallout> Process(FlightTelemetry current)
        {
            var callouts = new List<FlightCallout>();
            if (current == null) return callouts;

            if (current.V1Knots > 0D) lastValidV1 = current.V1Knots;

            if (previous == null)
            {
                Initialize(current);
                previous = current;
                return callouts;
            }

            ProcessAltitudeCrossings(current, callouts);
            ProcessPhase(current, callouts);
            previous = current;
            return callouts;
        }

        private void Initialize(FlightTelemetry current)
        {
            phase = current.IsOnGround
                ? FlightPhase.GroundIdle
                : FlightPhase.Airborne;
            positiveClimbCalled = !current.IsOnGround;
            tenThousandSide = SideOf(
                current.IndicatedAltitudeFeet,
                10000D,
                50D,
                ThresholdSide.Unknown);
            twoThousandSide = SideOf(
                current.RadioAltitudeFeet,
                2000D,
                25D,
                ThresholdSide.Unknown);
        }

        private void ProcessAltitudeCrossings(
            FlightTelemetry current,
            List<FlightCallout> callouts)
        {
            ThresholdSide newTenThousandSide = SideOf(
                current.IndicatedAltitudeFeet,
                10000D,
                50D,
                tenThousandSide);
            if (!current.IsOnGround &&
                tenThousandSide != ThresholdSide.Unknown &&
                newTenThousandSide != tenThousandSide)
            {
                callouts.Add(new FlightCallout(
                    "ten-thousand",
                    "Ten thousand"));
            }
            tenThousandSide = newTenThousandSide;

            ThresholdSide newTwoThousandSide = SideOf(
                current.RadioAltitudeFeet,
                2000D,
                25D,
                twoThousandSide);
            if (!current.IsOnGround &&
                current.VerticalSpeedFeetPerMinute < -100D &&
                twoThousandSide == ThresholdSide.Above &&
                newTwoThousandSide == ThresholdSide.Below)
            {
                callouts.Add(new FlightCallout(
                    "two-thousand",
                    "Two thousand"));
            }
            twoThousandSide = newTwoThousandSide;
        }

        private void ProcessPhase(
            FlightTelemetry current,
            List<FlightCallout> callouts)
        {
            if (phase == FlightPhase.GroundIdle)
                ProcessGroundIdle(current, callouts);
            else if (phase == FlightPhase.TakeoffRoll)
                ProcessTakeoffRoll(current, callouts);
            else if (phase == FlightPhase.Airborne)
                ProcessAirborne(current, callouts);
            else
                ProcessLandingRoll(current, callouts);
        }

        private void ProcessGroundIdle(
            FlightTelemetry current,
            List<FlightCallout> callouts)
        {
            if (!current.IsOnGround)
            {
                phase = FlightPhase.Airborne;
                return;
            }

            double averageN1 =
                (current.EngineOneN1Percent + current.EngineTwoN1Percent) / 2D;
            bool accelerating =
                current.IndicatedAirspeedKnots >
                previous.IndicatedAirspeedKnots;
            if (current.GroundSpeedKnots >= TakeoffArmSpeedKnots &&
                averageN1 >= TakeoffPowerN1Percent &&
                accelerating)
            {
                ResetTakeoffCallouts();
                takeoffArmed = true;
                phase = FlightPhase.TakeoffRoll;
                ProcessTakeoffRoll(current, callouts);
            }
        }

        private void ProcessTakeoffRoll(
            FlightTelemetry current,
            List<FlightCallout> callouts)
        {
            if (!current.IsOnGround)
            {
                phase = FlightPhase.Airborne;
                ProcessPositiveClimb(current, callouts);
                return;
            }

            if (takeoffArmed &&
                !oneHundredCalled &&
                CrossedUp(
                    previous.IndicatedAirspeedKnots,
                    current.IndicatedAirspeedKnots,
                    100D))
            {
                oneHundredCalled = true;
                callouts.Add(new FlightCallout(
                    "one-hundred-knots",
                    "One hundred knots"));
            }

            if (takeoffArmed &&
                !v1Called &&
                lastValidV1 > 0D &&
                CrossedUp(
                    previous.IndicatedAirspeedKnots,
                    current.IndicatedAirspeedKnots,
                    lastValidV1))
            {
                v1Called = true;
                callouts.Add(new FlightCallout("v1", "V one"));
            }

            bool rejectedTakeoff =
                current.GroundSpeedKnots < TakeoffArmSpeedKnots &&
                current.GroundSpeedKnots < previous.GroundSpeedKnots;
            if (rejectedTakeoff)
            {
                takeoffArmed = false;
                phase = FlightPhase.GroundIdle;
            }
        }

        private void ProcessAirborne(
            FlightTelemetry current,
            List<FlightCallout> callouts)
        {
            if (current.IsOnGround)
            {
                phase = FlightPhase.LandingRoll;
                touchdownTime = current.Timestamp;
                ResetLandingCallouts();
                ProcessLandingRoll(current, callouts);
                return;
            }

            ProcessPositiveClimb(current, callouts);
        }

        private void ProcessPositiveClimb(
            FlightTelemetry current,
            List<FlightCallout> callouts)
        {
            if (!positiveClimbCalled &&
                takeoffArmed &&
                !current.IsOnGround &&
                current.RadioAltitudeFeet >= 5D &&
                current.VerticalSpeedFeetPerMinute >= 100D)
            {
                positiveClimbCalled = true;
                callouts.Add(new FlightCallout(
                    "positive-climb",
                    "Positive climb"));
            }
        }

        private void ProcessLandingRoll(
            FlightTelemetry current,
            List<FlightCallout> callouts)
        {
            if (!current.IsOnGround)
            {
                phase = FlightPhase.Airborne;
                return;
            }

            if (!spoilersCalled &&
                Math.Max(
                    current.LeftSpoilerPercent,
                    current.RightSpoilerPercent) >= SpoilerDeployedPercent)
            {
                spoilersCalled = true;
                callouts.Add(new FlightCallout("spoilers", "Spoilers"));
            }

            if (!reverseCalled)
            {
                bool bothReversersDeployed =
                    current.EngineOneReversePercent >= ReverseDeployedPercent &&
                    current.EngineTwoReversePercent >= ReverseDeployedPercent;
                if (bothReversersDeployed)
                {
                    reverseCalled = true;
                    callouts.Add(new FlightCallout(
                        "reverse-green",
                        "Reverse green"));
                }
                else if (current.Timestamp - touchdownTime >=
                         TimeSpan.FromSeconds(3))
                {
                    reverseCalled = true;
                    callouts.Add(new FlightCallout(
                        "no-reverse",
                        "No reverse"));
                }
            }

            if (!manualBrakeCalled &&
                !current.AutobrakesActive &&
                Math.Max(
                    current.LeftBrakePosition,
                    current.RightBrakePosition) >= ManualBrakePosition)
            {
                manualBrakeCalled = true;
                callouts.Add(new FlightCallout(
                    "manual-brake",
                    "Manual brake"));
            }

            if (!sixtyCalled &&
                previous.IndicatedAirspeedKnots > 60D &&
                current.IndicatedAirspeedKnots <= 60D)
            {
                sixtyCalled = true;
                callouts.Add(new FlightCallout(
                    "sixty-knots",
                    "Sixty knots"));
            }

            if (current.GroundSpeedKnots < 5D)
            {
                takeoffArmed = false;
                phase = FlightPhase.GroundIdle;
            }
        }

        private void ResetTakeoffCallouts()
        {
            oneHundredCalled = false;
            v1Called = false;
            positiveClimbCalled = false;
            lastValidV1 = 0D;
        }

        private void ResetLandingCallouts()
        {
            spoilersCalled = false;
            reverseCalled = false;
            sixtyCalled = false;
            manualBrakeCalled = false;
        }

        private static bool CrossedUp(
            double previousValue,
            double currentValue,
            double threshold)
        {
            return previousValue < threshold && currentValue >= threshold;
        }

        private static ThresholdSide SideOf(
            double value,
            double threshold,
            double hysteresis,
            ThresholdSide currentSide)
        {
            if (value <= threshold - hysteresis) return ThresholdSide.Below;
            if (value >= threshold + hysteresis) return ThresholdSide.Above;
            return currentSide;
        }

        private enum FlightPhase
        {
            GroundIdle,
            TakeoffRoll,
            Airborne,
            LandingRoll
        }

        private enum ThresholdSide
        {
            Unknown,
            Below,
            Above
        }
    }
}
