using System.Reflection;
using iOSBot.Web.Models;
using Mapster;

namespace iOSBot.Web
{
    public static class MapsterConfig
    {
        public static void RegisterMapsterConfiguration(this IServiceCollection services)
        {
            TypeAdapterConfig<DeviceViewModel, Data.Device>
                .NewConfig()
                .Map(dest => dest.Color, src => Convert.ToUInt32(src.Color
                    .Replace("#", "")
                    .Replace("0x", ""), 16));

            TypeAdapterConfig<Data.Device, DeviceViewModel>
                .NewConfig()
                .Map(dest => dest.Color,
                    src => $"#{src.Color:X6}");
            
            TypeAdapterConfig.GlobalSettings.Scan(Assembly.GetExecutingAssembly());
        }
    }
}