using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BAMWallet.Extensions;
using BAMWallet.HD;
using BAMWallet.Model;
using Cli.Commands.Common;
using Kurukuru;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;


namespace CLi.Commands.CmdLine;

[CommandDescriptor("newcoin", "Consolidate coins into a single coin")]

public class WalletNewCoinCommand : Command
{
    private readonly ILogger _logger;

    public WalletNewCoinCommand(IServiceProvider serviceProvider)
        : base(typeof(WalletNewCoinCommand), serviceProvider, true)
    {
    }

    public override async Task Execute(Session activeSession = null)
    {
        if (activeSession != null)
        {
            var address = Prompt.GetString("Address/Name:", null, ConsoleColor.Red);
            var amount = Prompt.GetString("Consolidated Amount:", null, ConsoleColor.Red);

            if (!_commandReceiver.IsBase58(address))
            {
                var addressBook = new AddressBook { Name = address };
                var result = _commandReceiver.FindAddressBook(activeSession, ref addressBook);
                if (result.Item1 != null)
                {
                    address = (result.Item1 as AddressBook)?.RecipientAddress;
                    var yesno = Prompt.GetYesNo($"Address for {addressBook.Name}?\n(** {address?.ToUpper()} **)",
                        false, ConsoleColor.Red);
                    if (!yesno)
                    {
                        _console.ForegroundColor = ConsoleColor.Green;
                        _console.WriteLine("Processing transaction cancelled!");
                        _console.ForegroundColor = ConsoleColor.White;
                        return;
                    }
                }
                else
                {
                    _console.ForegroundColor = ConsoleColor.Red;
                    _console.WriteLine(result.Item2);
                    _console.ForegroundColor = ConsoleColor.White;
                    return;
                }
            }

            WalletTransaction transaction = null;
            await Spinner.StartAsync("Checking confirmed balance(s) ...", spinner =>
            {
                try
                {
                    if (decimal.TryParse(amount, out var t))
                    {
                        activeSession.SessionType = SessionType.Burn;
                        transaction = new WalletTransaction
                        {
                            Memo = "Consolidate and Burn coin",
                            Payment = t.ConvertToUInt64(),
                            RecipientAddress = address,
                            WalletType = WalletType.Burn,
                            Delay = 5,
                            IsVerified = false,
                            SenderAddress = activeSession.KeySet.StealthAddress
                        };

                        var canSpend = _commandReceiver.GetSpending(activeSession, transaction);
                        if (!canSpend.Success)
                        {
                            transaction = null;
                            throw new Exception(canSpend.NonSuccessMessage);
                        }
                    }

                }
                catch (Exception ex)
                {
                    activeSession.SessionId = Guid.NewGuid();
                    _logger.LogError("{@Message}/n{@Trace}", ex.Message, ex.StackTrace);
                    throw;
                }

                Thread.Sleep(100);

                return Task.CompletedTask;
            });

            WalletTransaction walletTransaction = null;
            await Spinner.StartAsync("Processing transaction...", spinner =>
            {

                try
                {
                    if (transaction == null) return Task.CompletedTask;
                    var createTransactionResult =
                        _commandReceiver.CreateTransaction(activeSession, ref transaction);
                    if (createTransactionResult.Item1 is null)
                    {
                        spinner.Fail(createTransactionResult.Item2);
                    }
                    else
                    {
                        walletTransaction = createTransactionResult.Item1 as WalletTransaction;
                    }
                }
                catch (Exception ex)
                {
                    activeSession.SessionId = Guid.NewGuid();
                    _logger.LogError("{@Message}/n{@Trace}", ex.Message, ex.StackTrace);
                    throw;
                }

                Thread.Sleep(100);

                return Task.CompletedTask;
            });

            await Spinner.StartAsync("Sending transaction ...", spinner =>
            {
                if (walletTransaction == null) return Task.CompletedTask;
                var sendResult = _commandReceiver.SendTransaction(activeSession, ref walletTransaction);
                if (sendResult.Item1 is not true)
                {
                    spinner.Fail(sendResult.Item2);
                }
                else
                {
                    spinner.Text = "Checking balance...";
                    var balanceResult = _commandReceiver.History(activeSession);
                    if (balanceResult.Item1 is null)
                    {
                        spinner.Fail(balanceResult.Item2);
                    }
                    else
                    {
                        var message =
                            $"Available Balance: [{(balanceResult.Item1 as IList<BalanceSheet>)!.Last().Balance}] " +
                            $"TxID: ** {walletTransaction.Transaction.TxnId.ByteToHex()} ** " +
                            $"Tx size: [{walletTransaction.Transaction.GetSize() * 0.001}kB]";
                        activeSession.SessionId = Guid.NewGuid();
                        spinner.Succeed(message);
                    }
                }

                Thread.Sleep(100);

                return Task.CompletedTask;
            }, Patterns.Toggle3);
        }
    }
}