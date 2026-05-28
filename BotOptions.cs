internal record BotOptions
{
    public const string Position = "BotOptions";

    public string[] ListendGroupIds { get; init; } = Array.Empty<string>();
}