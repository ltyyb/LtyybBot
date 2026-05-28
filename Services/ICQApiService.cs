using Sisters.WudiLib;

namespace LtyybBot;
internal interface ICQApiService
{
    HttpApiClient? OnebotApi { get; }
    bool IsAvailable { get; }
}
