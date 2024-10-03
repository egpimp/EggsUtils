using UnityEngine;

namespace EggsUtils.Helpers
{
    public class Math
    {
        //Basically lets us translate a value in a given range to a new value
        public static float ConvertToRange(float oldMin, float oldMax, float newMin, float newMax, float valueToConvert)
        {
            //Get the oldrange
            float oldRange = oldMax - oldMin;
            //Get the newrange
            float newRange = newMax - newMin;
            //I wish I knew what this math was doing so I could explain it, but there's too much going on in one line, just google it lol
            return (((valueToConvert - oldMin) * newRange) / oldRange) + newMin;
        }

        //Just simplified way for us to get a direction, nothing special.  Also might already exist idk
        public static Vector3 GetDirection(Vector3 startPos, Vector3 endPos)
        {
            return (endPos - startPos).normalized;
        }
    }
}
