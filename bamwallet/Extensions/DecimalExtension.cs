using BAMWallet.HD;
using Dawn;

namespace BAMWallet.Extensions
{
    public static class DecimalExtension
    {
        public static ulong ConvertToUInt64(this decimal value)
        {
            Guard.Argument(value, nameof(value)).NotZero().NotNegative();
            var amount = (ulong)(value * Constant.GYin);
            return amount;
        }

        public static uint ConvertToUInt32(this decimal value)
        {
            Guard.Argument(value, nameof(value)).NotZero().NotNegative();
            var amount = (uint)(value * Constant.MYin);
            return amount;
        }
    }
}