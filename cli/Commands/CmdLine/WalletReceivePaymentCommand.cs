// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.
// Improved by ChatGPT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BAMWallet.HD;
using BAMWallet.Model;
using Cli.Commands.Common;
using Kurukuru;
using McMaster.Extensions.CommandLineUtils;

namespace Cli.Commands.CmdLine
{
    [CommandDescriptor("receive", "Receive a payment")]
    public class WalletReceivePaymentCommand : Command
    {
        private readonly ILogger<WalletReceivePaymentCommand> _logger;
        private readonly ICommandReceiver _commandReceiver;

        public WalletReceivePaymentCommand(IServiceProvider serviceProvider, ICommandReceiver commandReceiver, ILogger<WalletReceivePaymentCommand> logger)
            : base(nameof(WalletReceivePaymentCommand), serviceProvider, true)
        {
            _commandReceiver = commandReceiver;
            _logger = logger;
        }

        public override async Task Execute(Session activeSession = null)
        {
            if (activeSession == null)
            {
                return;
            }

            var paymentId = Prompt.GetString("TxID:", null, ConsoleColor.Green);
            if (string.IsNullOrEmpty(paymentId))
            {
                return;
            }

            try
            {
                var (receive, errorReceive) = await _commandReceiver.ReceivePayment(activeSession, paymentId);
                if (receive == null)
                {
                    Spinner.Fail(errorReceive);
                    return;
                }

                var (balances, errorBalances) = await _commandReceiver.History(activeSession);
                if (balances == null)
                {
                    Spinner.Fail(errorBalances);
                    return;
                }

                if (balances is IList<BalanceSheet> balanceSheet)
                {
                    var transactions = balanceSheet.Where(x => x.TxId == balanceSheet.Last().TxId);
                    var received = transactions.Sum(x => x.MoneyIn);

                    Spinner.Succeed(
                        $"Memo: {transactions.First().Memo} Received: [{received:F9}] Available Balance: [{balanceSheet.Last().Balance:F9}]");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Message: {@msg}\n Stack: {@trace}", ex.Message, ex.StackTrace);
                throw;
            }
        }
    }
}
