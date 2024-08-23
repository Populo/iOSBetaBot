using System.Reflection;
using iOSBot.AdminWeb.Models;
using iOSBot.Data;
using Mapster;

namespace iOSBot.AdminWeb
{
    public static class MapsterConfig
    {
        public static void RegisterMapsterConfiguration(this IServiceCollection services)
        {
            TypeAdapterConfig<DeviceViewModel, Device>
                .NewConfig()
                .Map(dest => dest.Color, src => Convert.ToUInt32(src.Color
                    .Replace("#", "")
                    .Replace("0x", ""), 16));

            TypeAdapterConfig<Device, DeviceViewModel>
                .NewConfig()
                .Map(dest => dest.Color,
                    src => $"#{src.Color:X6}");

            TypeAdapterConfig.GlobalSettings.Scan(Assembly.GetExecutingAssembly());
        }
    }
}