// CypherNetwork BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.Linq;
using BAMWallet.Extensions;
using BAMWallet.Helper;
using MessagePack;
using NBitcoin;

namespace BAMWallet.Model
{
    [MessagePackObject]
    public record Vout
    {
        [Key(0)] public ulong Amount { get; set; }
        [Key(1)] public byte[] C { get; set; }
        [Key(2)] public byte[] E { get; set; }
        [Key(3)] public long LockTime { get; set; }
        [Key(4)] public byte[] N { get; set; }
        [Key(5)] public byte[] P { get; set; }
        [Key(6)] public byte[] Script { get; set; }
        [Key(7)] public CoinType Type { get; set; }
        [Key(8)] public byte[] Data { get; set; }

        public bool IsLockedOrInvalid()
        {
            if (Type != CoinType.Coinbase) return false;

            var lockTime = new LockTime(Util.UnixTimeToDateTime(LockTime));
            var sc1 = new Script($"{lockTime.Value} OP_CHECKLOCKTIMEVERIFY");
            var sc2 = new Script(Script);
            if (!sc1.ToString().Equals(sc2.ToString()))
            {
                return true;
            }

            var tx = Network.Main.CreateTransaction();
            tx.Outputs.Add(new TxOut { ScriptPubKey = new Script(Script) });
            var spending = Network.Main.CreateTransaction();
            spending.LockTime = new LockTime(DateTimeOffset.UtcNow);
            spending.Inputs.Add(new TxIn(tx.Outputs.AsCoins().First().Outpoint, new Script()));
            spending.Inputs[0].Sequence = 1;
            return !spending.Inputs.AsIndexedInputs().First().VerifyScript(tx.Outputs[0]);
        }
    }
}
