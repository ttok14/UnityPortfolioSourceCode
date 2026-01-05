using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;


public static class Helper
{
    public static string GetHash(byte[] bytes)
    {
        using (var sha256 = SHA256.Create())
        {
            byte[] hashBytes = sha256.ComputeHash(bytes);
            string hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

            return hashString;
        }
    }

    public static string GetHash(string str)
    {
        using (var sha256 = SHA256.Create())
        {
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(str));
            string hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

            return hashString;
        }
    }

    public static int GetApproximateInt(this float value)
    {
        return Mathf.FloorToInt(value + 0.4f);
    }
}
