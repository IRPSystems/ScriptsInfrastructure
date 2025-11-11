
namespace ScriptHandler.Models.ScriptNodes
{
    internal class ScriptNodeEOLSetManufDate : ScriptNodeBase
    {
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
