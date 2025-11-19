
using Entities.Enums;

namespace ScriptHandler.Models.ScriptNodes
{
    internal class ScriptNodeEOLSetManufDate : ScriptNodeBase
    {
        public DeviceTypesEnum DeviceType { get; set; }

        public override string Description
        {
            get
            {
                string description = "Manufacture Date";
                return description + " - ID:" + ID;
            }
        }

        public ScriptNodeEOLSetManufDate()
        {
            Name = "EOL Set Manufacture Date";
        }
    }
}
