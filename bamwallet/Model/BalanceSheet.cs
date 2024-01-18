﻿// CypherNetwork BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;

namespace BAMWallet.Model
{
    public class BalanceSheet
    {
        public DateTime Date { get; set; }
        public string Memo { get; set; }
        public string MoneyOut { get; set; }
        public string Fee { get; set; }
        public string MoneyIn { get; set; }
        public string Reward { get; set; }
        public string Mint { get; set; }
        public string Balance { get; set; }
        public Vout[] Outputs { get; set; }
        public string TxId { get; set; }
        public bool IsVerified { get; set; }
        public bool? IsLocked { get; set; }
        public WalletTransactionState State { get; set; }
    }
}