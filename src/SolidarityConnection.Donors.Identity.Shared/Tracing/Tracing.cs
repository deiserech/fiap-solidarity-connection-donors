using System.Diagnostics;

namespace SolidarityConnection.Donors.Identity.Shared.Tracing
{
    public static class Tracing
    {
        public static readonly ActivitySource ActivitySource = new("SolidarityConnection.Donors.Identity.Application");
    }
}

