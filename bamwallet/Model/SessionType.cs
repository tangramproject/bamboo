﻿// CypherNetwork BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

namespace BAMWallet.Model
{
    public enum SessionType : sbyte
    {
        Coin = 0x00,
        Coinstake = 0x01,
        Burn = 0x02,
        Mint = 0x03,
    }
}
