internal record ClientOptions
{
    public const string Position = "ClientOptions";

    public int WsServerPort { get; init; } = 8086;
    public string WsServerToken { get; init; } = string.Empty;
}