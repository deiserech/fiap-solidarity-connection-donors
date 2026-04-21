using System.Diagnostics;

namespace SolidarityConnection.Shared.Tracing
{
    public static class Tracing
    {
        public static readonly ActivitySource ActivitySource = new("SolidarityConnection.Application");
    }
}

