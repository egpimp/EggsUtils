using UnityEngine;

namespace EggsUtils.Helpers
{
    public class Math
    {
        public static float ConvertToRange(float oldMin, float oldMax, float newMin, float newMax, float valueToConvert)
        {
            float oldRange = oldMax - oldMin;
            float newRange = newMax - newMin;
            return (((valueToConvert - oldMin) * newRange) / oldRange) + newMin;
        }
        public static Vector3 GetDirection(Vector3 startPos, Vector3 endPos)
        {
            return (endPos - startPos).normalized;
        }
    }
}
