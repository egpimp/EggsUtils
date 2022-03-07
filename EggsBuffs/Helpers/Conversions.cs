using System;
using System.Linq;

namespace EggsUtils.Helpers
{
    public class Conversions
    {
        //Establish all the characters we need
        public const string CHARS = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

        //Converts something to base62 (Case sensitive Alphanumeric)
        public static string ToBase62(int num)
        {
            //Start with empty string
            string str = string.Empty;
            //Tempnum is the number we are converting
            int tempNum = num;
            //While there is more to convert
            while (tempNum > 0)
            {
                //Grab the remainder
                int val = tempNum % 62;
                //Divide by 62 to prep for the next loop
                tempNum /= 62;
                //Convert the number to one of our characters, append it to beginning of the string
                str = CHARS[val] + str;
            }
            //We need the string to be 2 digits, this does that for us by prefacing it with 0's
            while (str.Length < 2) str = "0" + str;
            //Return the finished string
            return str;
        }

        //Turns a string into a number
        public static int FromBase62(string str)
        {
            //Start with 0
            int val = 0;
            //Loop through the string
            for (int i = 0; i < str.Length; i++)
            {
                //This gives us the char in question
                char indexedChar = str[i];
                //Establish this to be assigned and added to val
                int num;
                //Shouldn't be needed, but makes ure the char exists and we expect it
                if (str.Contains(indexedChar))
                {
                    //This gets us int from 0-61, lines up with what char is assigned to what number
                    int base62Val = CHARS.IndexOf(indexedChar);
                    //Num is set to the index number, times 62 to the appropriate power
                    num = Convert.ToInt32(base62Val * (System.Math.Pow(62, str.Length - 1 - i)));
                }
                //If somehow it doesn't exist, default to 0
                else num = 0;
                //Add the num we found to the end value
                val += num;
            }
            //Once finished return the value
            return val;
        }
    }
}
