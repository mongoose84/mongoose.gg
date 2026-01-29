namespace RiotProxy.Core.ValueObjects;

public record SummonerId
{
    public string Value { get; }

    public SummonerId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentNullException(nameof(value), "Summoner ID cannot be empty");

        Value = value;
    }
}

public record AccountId
{
    public string Value { get; }

    public AccountId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentNullException(nameof(value), "Account ID cannot be empty");

        Value = value;
    }
}

public record PuuId
{
    public string Value { get; }

    public PuuId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentNullException(nameof(value), "PUUID cannot be empty");

        Value = value;
    }
}

