using System.Diagnostics;
using Azure.Messaging.ServiceBus;

namespace SolidarityConnection.Donors.Identity.Shared.Tracing
{
    public static class ServiceBusTracingHelper
    {
        public static Activity? StartConsumerActivity(
            ServiceBusReceivedMessage message,
            string operationName,
            string topic,
            string subscription)
        {
            message.ApplicationProperties.TryGetValue("traceparent", out var traceParentObj);
            message.ApplicationProperties.TryGetValue("tracestate", out var traceStateObj);

            var traceParent = traceParentObj as string;
            var traceState = traceStateObj as string;

            ActivityContext parentContext = default;
            var hasParent = !string.IsNullOrWhiteSpace(traceParent) &&
                            ActivityContext.TryParse(traceParent, traceState, out parentContext);

            var activity = hasParent
                ? Tracing.ActivitySource.StartActivity(operationName, ActivityKind.Consumer, parentContext)
                : Tracing.ActivitySource.StartActivity(operationName, ActivityKind.Consumer);

            if (activity != null)
            {
                activity.SetTag("messaging.system", "azure.servicebus");
                activity.SetTag("messaging.destination", topic);
                activity.SetTag("messaging.destination_kind", "topic");
                activity.SetTag("messaging.azure_servicebus.subscription", subscription);
            }

            return activity;
        }
    }
}

