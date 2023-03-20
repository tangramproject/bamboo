// Improved by ChatGPT

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;

namespace BAMWallet.Extensions
{
    /// <summary>
    /// Provides extension methods for the <see cref="SecureString"/> class.
    /// </summary>
    public static class SecureStringExtension
    {
        /// <summary>
        /// Converts a <see cref="SecureString"/> to a plain text <see cref="string"/>.
        /// </summary>
        /// <param name="secureString">The <see cref="SecureString"/> to convert.</param>
        /// <returns>The plain text <see cref="string"/> representation of the <see cref="SecureString"/>.</returns>
        public static string FromSecureString(this SecureString secureString)
        {
            IntPtr unmanagedString = IntPtr.Zero;
            try
            {
                unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(secureString);
                return Marshal.PtrToStringUni(unmanagedString);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
            }
        }

        /// <summary>
        /// Converts a <see cref="SecureString"/> to a byte array.
        /// </summary>
        /// <param name="s">The <see cref="SecureString"/> to convert.</param>
        /// <returns>The byte array representation of the <see cref="SecureString"/>.</returns>
        public static byte[] ToArray(this SecureString s)
        {
            if (s.Length == 0)
                return Array.Empty<byte>();

            var ptr = SecureStringMarshal.SecureStringToGlobalAllocAnsi(s);

            try
            {
                var i = 0;
                do
                {
                    var b = Marshal.ReadByte(ptr, i++);
                    if (b == 0)
                        break;

                    yield return b;

                } while (true);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocAnsi(ptr);
            }
        }
    }
}
