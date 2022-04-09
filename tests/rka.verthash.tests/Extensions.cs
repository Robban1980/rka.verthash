using System;

namespace RKA.Verthash.Tests;

internal static class Extensions
{
    public static string ToHex(this byte[] input)
    {
        return BitConverter.ToString(input).Replace("-", "").ToLower();
    }

    public static byte[] ToBytes(this string input)
    {
        byte[] bytes = new byte[input.Length / 2];
        for (int i = 0; i < input.Length; i += 2)
        {
            bytes[i / 2] = Convert.ToByte(input.Substring(i, 2), 16);
        }
        return bytes;
    }
}
