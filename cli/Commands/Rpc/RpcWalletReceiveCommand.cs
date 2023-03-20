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
using System.Threading;
using System.Threading.Tasks;
using BAMWallet.HD;
using BAMWallet.Model;

namespace Cli.Commands.Rpc
{
    class RpcWalletReceiveCommand : RpcBaseCommand
    {
        private readonly string _paymentId;

        public RpcWalletReceiveCommand(string paymentId, IServiceProvider serviceProvider, ref AutoResetEvent cmdFinishedEvent, Session session)
            : base(serviceProvider, ref cmdFinishedEvent, session)
        {
            _paymentId = paymentId;
        }

        public override Task Execute(Session activeSession = null)
        {
            try
            {
                using var commandReceiver = new CommandReceiver();

                var receivePaymentResult = commandReceiver.ReceivePayment(_session, _paymentId);
                if (receivePaymentResult.Item1 == null)
                {
                    return Task.FromResult(new RpcWalletReceiveResult { ErrorMessage = receivePaymentResult.Item2 });
                }

                var balanceSheetResult = commandReceiver.History(_session);
                if (balanceSheetResult.Item1 == null)
                {
                    return Task.FromResult(new RpcWalletReceiveResult { ErrorMessage = balanceSheetResult.Item2 });
                }

                var lastSheet = balanceSheetResult.Item1.LastOrDefault();
                if (lastSheet == null)
                {
                    return Task.FromResult(new RpcWalletReceiveResult { ErrorMessage = "No balance sheets found" });
                }

                var result = new RpcWalletReceiveResult
                {
                    Memo = lastSheet.Memo,
                    Received = lastSheet.MoneyIn,
                    Balance = $"{lastSheet.Balance}"
                };

                return Task.FromResult(result);
            }
            catch (ArgumentException ex)
            {
                return Task.FromResult(new RpcWalletReceiveResult { ErrorMessage = ex.Message });
            }
            catch (Exception ex)
            {
                return Task.FromResult(new RpcWalletReceiveResult { ErrorMessage = "An error occurred while executing the command" });
            }
            finally
            {
                _cmdFinishedEvent.Set();
            }
        }
    }

    class RpcWalletReceiveResult
    {
        public string Memo { get; set; }
        public long Received { get; set; }
        public string Balance { get; set; }
        public string ErrorMessage { get; set; }
    }
}
