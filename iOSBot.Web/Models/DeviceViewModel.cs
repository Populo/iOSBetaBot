using iOSBot.Data;

namespace iOSBot.Web.Models
{
    public class DeviceViewModel
    {
        public List<Device> Devices { get; set; }
        public Device ChangingDevice { get; set; }
    }
}
