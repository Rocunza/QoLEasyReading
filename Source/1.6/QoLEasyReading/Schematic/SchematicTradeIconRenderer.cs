using UnityEngine;

namespace ZCN.QoL.EasyReading
{
    /// <summary>
    /// S3-B1: decorative paint only; no GUI input handling or tooltips.
    /// Caller owns row position and ensures texture exists BEFORE reserving
    /// label space. Always restores Unity GUI tint, including on exceptions.
    /// </summary>
    internal static class SchematicTradeIconRenderer
    {
        internal static bool Paint(Texture2D texture, Rect rect)
        {
            if (Event.current == null || Event.current.type != EventType.Repaint ||
                texture == null || rect.width <= 0f || rect.height <= 0f)
                return false;

            Color previous = GUI.color;
            try
            {
                GUI.color = Color.white;
                GUI.DrawTexture(rect, texture);
                return true;
            }
            finally
            {
                GUI.color = previous;
            }
        }
    }
}
