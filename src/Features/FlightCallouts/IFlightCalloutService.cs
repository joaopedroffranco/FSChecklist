using System;

namespace FSChecklist.Features.FlightCallouts
{
    internal interface IFlightCalloutService : IDisposable
    {
        void Start();
    }
}
