using iOSBot.Service;

namespace iOSBot.Bot.Models;

public static class UpdateDtoExtensions
{
    public static Update2 ConvertUpdate(this UpdateDto updateDto) => new()
    {
        ReleaseDate = updateDto.ReleaseDate,
        Version = updateDto.Version,
        Build = updateDto.Build,
        Size = updateDto.Size,
        TrackId = updateDto.TrackId,
        TrackName = updateDto.TrackName,
        ReleaseType = updateDto.ReleaseType,
        Color = updateDto.Color
    };
}