// CypherNetwork BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

namespace BAMWallet.Model
{
    public enum CoinType : sbyte
    {
        System = 0x00,
        Coin = 0x01,
        Coinbase = 0x02,
        Coinstake = 0x03,
        Fee = 0x04,
        Genesis = 0x05,
        Payment = 0x06,
        Change = 0x07,
        Timebase = 0x08,
        Burn = 0x09,
        Mint = 0x0A,
    }
}
