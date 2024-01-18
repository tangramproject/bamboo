namespace BAMWallet.Model;

public record BalanceProfile(decimal Mint, decimal Payment, decimal Coinstake, decimal Coinbase, decimal Change, decimal Balance);