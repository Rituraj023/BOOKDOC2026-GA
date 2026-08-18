using System.Net;

namespace BookDoc2026.Admin.Security;

public sealed class AdminNetworkOptions
{
    public const string SectionName = "AdminNetwork";
    public bool Enforce { get; set; } = true;
    public string[] AllowedNetworks { get; set; } = [];
    public string[] TrustedProxies { get; set; } = [];
}

public sealed record AdminNetworkDecision(bool IsAllowed, string Reason);

public sealed class AdminNetworkPolicy
{
    private readonly AdminNetworkOptions _options;
    private readonly IReadOnlyList<IpNetworkRange> _allowed;
    private readonly IReadOnlyList<IpNetworkRange> _trusted;

    public AdminNetworkPolicy(AdminNetworkOptions options)
    {
        _options = options;
        _allowed = ParseAll(options.AllowedNetworks, "allowed network");
        _trusted = ParseAll(options.TrustedProxies, "trusted proxy");
    }

    public AdminNetworkDecision Evaluate(IPAddress? remoteAddress, string? forwardedFor)
    {
        if (!_options.Enforce) return new(true, "disabled");
        if (remoteAddress is null) return new(false, "missing_remote_address");
        if (_allowed.Count == 0) return new(false, "allow_list_empty");

        var clientAddress = Normalize(remoteAddress);
        if (!string.IsNullOrWhiteSpace(forwardedFor) && IsIn(_trusted, clientAddress))
        {
            var entries = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (entries.Length is 0 or > 10) return new(false, "forwarded_chain_invalid");
            for (var index = entries.Length - 1; index >= 0 && IsIn(_trusted, clientAddress); index--)
            {
                if (!IPAddress.TryParse(entries[index], out var candidate))
                    return new(false, "forwarded_address_invalid");
                clientAddress = Normalize(candidate);
            }
        }

        return IsIn(_allowed, clientAddress)
            ? new(true, "allowed")
            : new(false, "client_not_allowed");
    }

    private static IReadOnlyList<IpNetworkRange> ParseAll(IEnumerable<string> values, string label)
    {
        var ranges = new List<IpNetworkRange>();
        foreach (var value in values ?? [])
        {
            if (!IpNetworkRange.TryParse(value, out var range))
                throw new InvalidOperationException($"Admin network configuration contains an invalid {label}.");
            ranges.Add(range);
        }
        return ranges;
    }

    private static bool IsIn(IEnumerable<IpNetworkRange> ranges, IPAddress address) =>
        ranges.Any(range => range.Contains(address));

    private static IPAddress Normalize(IPAddress address) =>
        address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;
}

public readonly record struct IpNetworkRange(IPAddress Network, int PrefixLength)
{
    public static bool TryParse(string value, out IpNetworkRange range)
    {
        range = default;
        if (string.IsNullOrWhiteSpace(value)) return false;
        var parts = value.Trim().Split('/', 2);
        if (!IPAddress.TryParse(parts[0], out var address)) return false;
        address = address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;
        var maximum = address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? 32 : 128;
        var prefix = parts.Length == 1
            ? maximum
            : int.TryParse(parts[1], out var parsed) ? parsed : -1;
        if (prefix < 0 || prefix > maximum) return false;
        range = new IpNetworkRange(Mask(address, prefix), prefix);
        return true;
    }

    public bool Contains(IPAddress address)
    {
        address = address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;
        if (address.AddressFamily != Network.AddressFamily) return false;
        return Mask(address, PrefixLength).Equals(Network);
    }

    private static IPAddress Mask(IPAddress address, int prefixLength)
    {
        var bytes = address.GetAddressBytes();
        var wholeBytes = prefixLength / 8;
        var remainingBits = prefixLength % 8;
        if (wholeBytes < bytes.Length && remainingBits > 0)
        {
            bytes[wholeBytes] &= (byte)(0xFF << (8 - remainingBits));
            wholeBytes++;
        }
        for (var index = wholeBytes; index < bytes.Length; index++) bytes[index] = 0;
        return new IPAddress(bytes);
    }
}
