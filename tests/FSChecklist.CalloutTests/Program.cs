using System;
using System.Collections.Generic;
using System.Linq;
using FSChecklist.Domain.Flight;
using FSChecklist.Features.FlightCallouts;

namespace FSChecklist.CalloutTests
{
    internal static class Program
    {
        private static readonly DateTimeOffset Start =
            new DateTimeOffset(2026, 7, 28, 12, 0, 0, TimeSpan.Zero);

        private static int Main()
        {
            try
            {
                VerifyCompleteFlight();
                VerifyNoReverse();
                VerifyNoStartupFalsePositives();
                Console.WriteLine("Flight callout tests passed.");
                return 0;
            }
            catch (Exception error)
            {
                Console.Error.WriteLine(error.Message);
                return 1;
            }
        }

        private static void VerifyCompleteFlight()
        {
            var engine = new FlightCalloutEngine();
            var heard = new List<string>();

            Process(engine, heard, Sample(0, true, 0, 0, 0, 0, 20));
            Process(engine, heard, Sample(1, true, 35, 35, 0, 0, 70, v1: 135));
            Process(engine, heard, Sample(2, true, 99, 99, 0, 0, 82, v1: 135));
            Process(engine, heard, Sample(3, true, 101, 101, 0, 0, 84, v1: 135));
            Process(engine, heard, Sample(4, true, 136, 136, 0, 0, 86, v1: 135));
            Process(engine, heard, Sample(5, false, 140, 140, 2, 500, 86, v1: 135));
            Process(engine, heard, Sample(6, false, 145, 145, 6, 700, 86, v1: 135));
            Process(engine, heard, Sample(7, false, 250, 250, 9940, 1200, 75));
            Process(engine, heard, Sample(8, false, 250, 250, 10060, 1200, 75));
            Process(engine, heard, Sample(9, false, 240, 240, 10060, -1000, 55));
            Process(engine, heard, Sample(10, false, 230, 230, 9940, -1000, 55));
            Process(engine, heard, Sample(11, false, 180, 180, 2030, -700, 50));
            Process(engine, heard, Sample(12, false, 175, 175, 1970, -700, 50));
            Process(engine, heard, Sample(13, false, 135, 135, 10, -300, 45));
            Process(engine, heard, Sample(
                14, true, 130, 130, 0, 0, 40,
                spoilers: 30, autobrakes: true));
            Process(engine, heard, Sample(
                15, true, 120, 120, 0, 0, 35,
                reverse: 10, spoilers: 30, autobrakes: true));
            Process(engine, heard, Sample(
                16, true, 65, 65, 0, 0, 30,
                reverse: 10, spoilers: 30, autobrakes: true));
            Process(engine, heard, Sample(
                17, true, 59, 59, 0, 0, 28,
                reverse: 10, spoilers: 30, autobrakes: true));
            Process(engine, heard, Sample(
                18, true, 45, 45, 0, 0, 25,
                reverse: 0, spoilers: 10, brakes: 5000));

            AssertSequence(
                heard,
                "one-hundred-knots",
                "v1",
                "positive-climb",
                "ten-thousand",
                "ten-thousand",
                "two-thousand",
                "spoilers",
                "reverse-green",
                "sixty-knots",
                "manual-brake");
        }

        private static void VerifyNoReverse()
        {
            var engine = new FlightCalloutEngine();
            var heard = new List<string>();

            Process(engine, heard, Sample(0, false, 140, 140, 20, -300, 40));
            Process(engine, heard, Sample(1, true, 130, 130, 0, 0, 35));
            Process(engine, heard, Sample(3, true, 110, 110, 0, 0, 30));
            Process(engine, heard, Sample(5, true, 95, 95, 0, 0, 25));

            AssertSequence(heard, "no-reverse");
        }

        private static void VerifyNoStartupFalsePositives()
        {
            var landingStartup = new FlightCalloutEngine();
            var landingHeard = new List<string>();
            Process(
                landingStartup,
                landingHeard,
                Sample(0, true, 70, 70, 0, 0, 25, spoilers: 30));
            Process(
                landingStartup,
                landingHeard,
                Sample(1, true, 55, 55, 0, 0, 20, brakes: 5000));
            AssertSequence(landingHeard);

            var airborneStartup = new FlightCalloutEngine();
            var airborneHeard = new List<string>();
            Process(
                airborneStartup,
                airborneHeard,
                Sample(0, false, 180, 180, 1500, -800, 45));
            Process(
                airborneStartup,
                airborneHeard,
                Sample(1, false, 175, 175, 1400, -800, 45));
            AssertSequence(airborneHeard);
        }

        private static FlightTelemetry Sample(
            int seconds,
            bool onGround,
            double airspeed,
            double groundSpeed,
            double altitude,
            double verticalSpeed,
            double n1,
            double v1 = 0,
            double reverse = 0,
            double spoilers = 0,
            double brakes = 0,
            bool autobrakes = false)
        {
            return new FlightTelemetry
            {
                Timestamp = Start.AddSeconds(seconds),
                IsOnGround = onGround,
                IndicatedAirspeedKnots = airspeed,
                GroundSpeedKnots = groundSpeed,
                IndicatedAltitudeFeet = altitude,
                RadioAltitudeFeet = altitude,
                VerticalSpeedFeetPerMinute = verticalSpeed,
                EngineOneN1Percent = n1,
                EngineTwoN1Percent = n1,
                AirlineV1Knots = v1,
                EngineOneReversePercent = reverse,
                EngineTwoReversePercent = reverse,
                LeftSpoilerPercent = spoilers,
                RightSpoilerPercent = spoilers,
                LeftBrakePosition = brakes,
                RightBrakePosition = brakes,
                AutobrakesActive = autobrakes
            };
        }

        private static void Process(
            FlightCalloutEngine engine,
            List<string> heard,
            FlightTelemetry telemetry)
        {
            heard.AddRange(engine.Process(telemetry).Select(item => item.Id));
        }

        private static void AssertSequence(
            IReadOnlyList<string> actual,
            params string[] expected)
        {
            string actualText = string.Join(", ", actual);
            string expectedText = string.Join(", ", expected);
            if (!actual.SequenceEqual(expected))
            {
                throw new InvalidOperationException(
                    "Expected [" + expectedText + "] but got [" +
                    actualText + "].");
            }
        }
    }
}
