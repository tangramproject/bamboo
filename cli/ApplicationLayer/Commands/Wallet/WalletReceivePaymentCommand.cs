﻿// Bamboo (c) by Tangram 
// 
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

using Newtonsoft.Json;

using Kurukuru;

using McMaster.Extensions.CommandLineUtils;

using BAMWallet.HD;
using BAMWallet.Model;
using BAMWallet.Extentions;

namespace CLi.ApplicationLayer.Commands.Wallet
{
    [CommandDescriptor(new string[] { "receive" }, "Receive a payment")]
    public class WalletReceivePaymentCommand : Command
    {
        private readonly IWalletService _walletService;
        private readonly ILogger _logger;

        private Spinner spinner;

        public WalletReceivePaymentCommand(IServiceProvider serviceProvider)
        {
            _walletService = serviceProvider.GetService<IWalletService>();
            _logger = serviceProvider.GetService<ILogger>();
        }

        public override async Task Execute()
        {
            using var identifier = Prompt.GetPasswordAsSecureString("Identifier:", ConsoleColor.Yellow);
            using var passphrase = Prompt.GetPasswordAsSecureString("Passphrase:", ConsoleColor.Yellow);

            var paymentId = Prompt.GetString("PAYMENTID:", null, ConsoleColor.Green);

            if (!string.IsNullOrEmpty(paymentId))
            {
                await Spinner.StartAsync("Receiving payment...", async spinner =>
                {
                    this.spinner = spinner;

                    try
                    {
                        var session = _walletService.SessionAddOrUpdate(new Session(identifier, passphrase));

                        await _walletService.ReceivePayment(session.SessionId, paymentId);

                        if (session.LastError != null)
                        {
                            spinner.Fail(JsonConvert.SerializeObject(session.LastError.GetValue("message")));
                            return;
                        }

                        var transaction = _walletService.LastWalletTransaction(session.SessionId, WalletType.Receive);
                        var txnReceivedAmount = transaction == null ? 0.ToString() : transaction.Payment.DivWithNaT().ToString("F9");
                        var txnMemo = transaction == null ? "" : transaction.Memo;
                        var balance = _walletService.AvailableBalance(session.SessionId);

                        spinner.Succeed($"Memo: {txnMemo}  Received: {txnReceivedAmount}  Available Balance: {balance.Result.DivWithNaT():F9}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Message: {ex.Message}\n Stack: {ex.StackTrace}");
                        throw;
                    }
                }, Patterns.Toggle3);
            }
        }
    }
}

