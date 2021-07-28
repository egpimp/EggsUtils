using System;
using System.Linq;

namespace EggsUtils.Helpers
{
    public class Conversions
    {
        public static string chars { get; private set; } = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        public static string ToBase62(int num)
        {
            string str = "";
            int tempNum = num;
            while (tempNum > 0)
            {
                int val = tempNum % 62;
                tempNum /= 62;
                str = chars.ElementAt(val) + str;
            }
            while (str.Length < 2)
            {
                str = "0" + str;
            }
            return str;
        }
        public static int FromBase62(string str)
        {
            int val = 0;
            for (int i = 0; i < str.Length; i++)
            {
                char indexedChar = str[i];
                int num;
                if (str.Contains(indexedChar))
                {
                    int base62Val = chars.IndexOf(indexedChar);
                    num = Convert.ToInt32(base62Val * (System.Math.Pow(62, str.Length - 1 - i)));
                }
                else
                {
                    num = 0;
                }
                val += num;
            }
            return val;
        }
    }
}
