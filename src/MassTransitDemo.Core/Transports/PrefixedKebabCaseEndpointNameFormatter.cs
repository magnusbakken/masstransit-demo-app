using MassTransit;

namespace MassTransitDemo.Core.Transports;

/// <summary>
/// Wraps <see cref="KebabCaseEndpointNameFormatter"/> and prepends a dot-separated
/// prefix to every receive-endpoint (queue) name while leaving topic/exchange names
/// untouched.  This keeps queue names globally unique per user while still routing
/// through shared exchanges/topics.
/// </summary>
public sealed class PrefixedKebabCaseEndpointNameFormatter : IEndpointNameFormatter
{
    private readonly IEndpointNameFormatter _inner;
    private readonly string _prefix;

    public PrefixedKebabCaseEndpointNameFormatter(string prefix)
        : this(prefix, KebabCaseEndpointNameFormatter.Instance)
    {
    }

    public PrefixedKebabCaseEndpointNameFormatter(string prefix, IEndpointNameFormatter inner)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);
        ArgumentNullException.ThrowIfNull(inner);

        _prefix = prefix;
        _inner = inner;
    }

    public string Separator => _inner.Separator;

    public string Consumer<T>()
        where T : class, IConsumer =>
        Prefix(_inner.Consumer<T>());

    public string Saga<T>()
        where T : class, ISaga =>
        Prefix(_inner.Saga<T>());

    public string ExecuteActivity<T, TArguments>()
        where T : class, IExecuteActivity<TArguments>
        where TArguments : class =>
        Prefix(_inner.ExecuteActivity<T, TArguments>());

    public string CompensateActivity<T, TLog>()
        where T : class, ICompensateActivity<TLog>
        where TLog : class =>
        Prefix(_inner.CompensateActivity<T, TLog>());

    public string Message<T>()
        where T : class =>
        _inner.Message<T>();

    public string TemporaryEndpoint(string tag) =>
        Prefix(_inner.TemporaryEndpoint(tag));

    public string SanitizeName(string name) =>
        _inner.SanitizeName(name);

    private string Prefix(string name) => $"{_prefix}.{name}";
}
