namespace ZCN.QoL.EasyReading
{
    // R1: Shared icon size conversion. Does not change UI placement or Harmony patches.
    internal static class BookIconMetrics
    {
        internal static int NormalizeSize(int size)
        {
            return size == 100 || size == 125 || size == 150 ? size : 125;
        }

        internal static float IconSize(int percent)
        {
            switch (NormalizeSize(percent))
            {
                case 100: return 18f;
                case 150: return 27f;
                default: return 23f;
            }
        }
    }
}
