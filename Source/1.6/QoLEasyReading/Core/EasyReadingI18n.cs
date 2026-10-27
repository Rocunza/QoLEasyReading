using System.Globalization;
using Verse;

namespace ZCN.QoL.EasyReading
{
    /// <summary>
    /// User-visible text only. Keys live under Languages/{English,Korean}/Keyed.
    /// No gameplay behavior, settings serialization or global language changes.
    /// </summary>
    internal static class EasyReadingI18n
    {
        internal static string Get(string key)
        {
            return key.Translate().ToString();
        }

        internal static string Format(string key, params object[] args)
        {
            return string.Format(CultureInfo.CurrentCulture, Get(key), args);
        }
    }
}
