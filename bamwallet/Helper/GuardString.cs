// CypherNetwork BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.Runtime.InteropServices;
using System.Security;
using BAMWallet.Extensions;

namespace BAMWallet.Helper
{
    public sealed class GuardString : IDisposable, IEnumerable<char>
    {
        public string Value { get; private set; }

        private readonly SecureString _secureString;
        private readonly GCHandle _gcHandle;

        public GuardString(SecureString secureString)
        {
            _secureString = secureString;

            _gcHandle = GCHandle.Alloc(Value, GCHandleType.Pinned);
            IntPtr insecurePointer = IntPtr.Zero;

            using (var secureStringPtr = SecureStringMarshal.SecureStringToGlobalAllocUnicode(secureString))
            {
                insecurePointer = secureStringPtr.DangerousGetHandle();
                Value = Marshal.PtrToStringUni(insecurePointer, secureString.Length);
            }

            _gcHandle.Target = Value;
        }

        public void Dispose()
        {
            if (_gcHandle.IsAllocated)
            {
                IntPtr insecurePointer = IntPtr.Zero;
                using (var secureStringPtr = SecureStringMarshal.SecureStringToGlobalAllocUnicode(_secureString))
                {
                    insecurePointer = secureStringPtr.DangerousGetHandle();
                    Marshal.Copy(new char[_secureString.Length], 0, insecurePointer, _secureString.Length);
                }

#if DEBUG
                Value = "¡DISPOSED¡".Substring(0, Math.Min("¡DISPOSED¡".Length, _secureString.Length));
#endif

                _gcHandle.Free();
            }
        }

        public IEnumerator<char> GetEnumerator()
        {
            return Value?.GetEnumerator() ?? new char[0].GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
