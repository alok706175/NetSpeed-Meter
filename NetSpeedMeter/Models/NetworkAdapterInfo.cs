namespace NetSpeedMeter.Models
{
    public class NetworkAdapterInfo
    {
        public string Id { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string InterfaceType { get; init; } = string.Empty;
        public bool IsUp { get; init; }
        public bool IsAutomatic { get; init; }

        public string DisplayName => IsAutomatic
            ? "Automatic (Recommended)"
            : $"{Name} ({Description})";

        public override string ToString() => DisplayName;

        public static NetworkAdapterInfo Automatic => new()
        {
            Id = "auto",
            Name = "Automatic",
            Description = "Automatically picks the active network adapter",
            InterfaceType = "Auto",
            IsUp = true,
            IsAutomatic = true
        };
    }
}
