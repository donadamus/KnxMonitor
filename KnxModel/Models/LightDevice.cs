using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace KnxModel
{
    public class LightDevice : LightDeviceBase<LightDevice, LightAddresses>
    {
        public LightDevice(string id, string name, string subGroup, IKnxService knxService, ILogger<LightDevice> logger, TimeSpan defaultTimeout)
            : base(id, name, subGroup, KnxAddressConfiguration.CreateLightAddresses(subGroup), knxService, logger, defaultTimeout)
        {
            Initialize(this);
        }
    }
}
