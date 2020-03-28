namespace OceanOfCode.Helpers
{
    using OceanOfCode.Enums;

    public static class DeviceTypeHelpers
    {
        public static string ToText(this DeviceType deviceType)
        {
            switch (deviceType)
            {
                case DeviceType.Torpedo: return "TORPEDO";
                case DeviceType.Sonar: return "SONAR";
                case DeviceType.Silence: return "SILENCE";
                case DeviceType.Mine: return "MINE";
            }
            return "None";
        }
    }
}
