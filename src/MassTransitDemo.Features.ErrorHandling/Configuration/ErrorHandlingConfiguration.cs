using MassTransit;

namespace MassTransitDemo.Features.ErrorHandling.Configuration;

/// <summary>
/// Configuration helper for error handling, retry policies, and dead-letter queues.
/// </summary>
public static class ErrorHandlingConfiguration
{
    /// <summary>
    /// Configures dead-letter queue handling for Azure Service Bus.
    /// Uses reflection to check for IServiceBusReceiveEndpointConfigurator to avoid direct dependency.
    /// </summary>
    public static void ConfigureAzureServiceBusDeadLetterQueue(IReceiveEndpointConfigurator configurator, bool useNativeDlq = true)
    {
        if (!useNativeDlq)
        {
            return; // Use MassTransit default (_skipped/_error queues)
        }

        // Use reflection to configure Azure Service Bus native DLQ
        // This avoids requiring a direct reference to Azure Service Bus package in this project
        var configuratorType = configurator.GetType();
        var interfaceType = configuratorType.GetInterfaces()
            .FirstOrDefault(i => i.Name == "IServiceBusReceiveEndpointConfigurator");

        if (interfaceType != null)
        {
            // Call ConfigureDeadLetterQueueDeadLetterTransport and ConfigureDeadLetterQueueErrorTransport
            var deadLetterMethod = interfaceType.GetMethod("ConfigureDeadLetterQueueDeadLetterTransport");
            var errorMethod = interfaceType.GetMethod("ConfigureDeadLetterQueueErrorTransport");

            deadLetterMethod?.Invoke(configurator, null);
            errorMethod?.Invoke(configurator, null);
        }
    }
}
