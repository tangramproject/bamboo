// Improved by ChatGPT

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;

namespace BAMWallet.Extensions
{
    /// <summary>
    /// Provides extension methods for strings.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Converts the string to a secure string.
        /// </summary>
        /// <param name="value">The string to convert.</param>
        /// <returns>The secure string.</returns>
        public static SecureString ToSecureString(this string value)
        {
            var secureString = new SecureString();
            Array.ForEach(value.ToArray(), secureString.AppendChar);
            secureString.MakeReadOnly();
            return secureString;
        }

        /// <summary>
        /// Converts the hexadecimal string to a byte array.
        /// </summary>
        /// <param name="hex">The hexadecimal string to convert.</param>
        /// <returns>The byte array.</returns>
        public static byte[] HexToByte(this string hex)
        {
            return Convert.FromHexString(hex);
        }

        /// <summary>
        /// Converts the generic type to a hexadecimal string and then to a byte array.
        /// </summary>
        /// <typeparam name="T">The type to convert.</typeparam>
        /// <param name="hex">The type to convert.</param>
        /// <returns>The byte array.</returns>
        public static byte[] HexToByte<T>(this T hex)
        {
            return Convert.FromHexString(hex.ToString()!);
        }

        /// <summary>
        /// Overwrites the string with null characters.
        /// </summary>
        /// <param name="value">The string to zero.</param>
        public static void ZeroString(this string value)
        {
            var handle = GCHandle.Alloc(value, GCHandleType.Pinned);
            unsafe
            {
                var pValue = (char*)handle.AddrOfPinnedObject();
                var span = new ReadOnlySpan<char>(pValue, value.Length);
                span.Fill(char.MinValue);
            }

            handle.Free();
        }

        /// <summary>
        /// Converts the string to a decimal.
        /// </summary>
        /// <param name="value">The string to convert.</param>
        /// <returns>The decimal.</returns>
        public static decimal ToDecimal(this string value)
        {
            return Convert.ToDecimal(value);
        }

        /// <summary>
        /// Converts the string to a 64-bit integer.
        /// </summary>
        /// <param name="value">The string to convert.</param>
        /// <returns>The 64-bit integer.</returns>
        public static long ToInt64(this string value)
        {
            return Convert.ToInt64(value);
        }
    }
}
