namespace PipelineStabilityExample;

/// <summary>
/// Generates an infinite stream of simulated sensor messages using an <see cref="IEnumerable{SensorMessage}"/>.
/// </summary>
public static class InfiniteDataGenerator
{
    private static readonly string[] DevicePool =
    [
        "sensor-us-east-01",
        "sensor-us-west-02",
        "sensor-eu-central-01",
        "sensor-ap-southeast-01",
        "sensor-edge-gateway-99"
    ];

    /// <summary>
    /// Produces an infinite sequence of <see cref="SensorMessage"/> items until cancellation is requested.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to signal when the generator should stop producing items.</param>
    /// <returns>An infinite enumerable sequence of sensor telemetry messages.</returns>
    public static IEnumerable<SensorMessage> Generate(CancellationToken cancellationToken)
    {
        long sequenceId = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            sequenceId++;
            var deviceId = DevicePool[sequenceId % DevicePool.Length];
            var temperature = 15.0 + (Random.Shared.NextDouble() * 30.0);
            var humidity = 20.0 + (Random.Shared.NextDouble() * 70.0);
            var payload = new byte[256];

            yield return new SensorMessage(
                SequenceId: sequenceId,
                DeviceId: deviceId,
                Temperature: temperature,
                Humidity: humidity,
                Timestamp: DateTime.UtcNow,
                Payload: payload
            );
        }
    }
}
