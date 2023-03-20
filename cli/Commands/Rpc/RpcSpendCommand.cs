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
using System.Threading;
using System.Threading.Tasks;
using BAMWallet.Extensions;
using BAMWallet.HD;
using BAMWallet.Model;

namespace Cli.Commands.Rpc
{
    class RpcSpendCommand : RpcBaseCommand
    {
        private readonly WalletTransaction _transaction;

        public RpcSpendCommand(ref WalletTransaction transaction, IServiceProvider serviceProvider, ref AutoResetEvent cmdFinishedEvent, Session session)
            : base(serviceProvider, ref cmdFinishedEvent, session)
        {
            _transaction = transaction;
        }

        public override async Task Execute(Session activeSession = null)
        {
            try
            {
                using var canSpendResult = _commandReceiver.GetSpending(_session, _transaction);
                if (!canSpendResult.Success)
                {
                    throw new Exception(canSpendResult.NonSuccessMessage);
                }

                using var createTransactionResult = _commandReceiver.CreateTransaction(_session, ref _transaction);
                if (createTransactionResult.Item1 is null)
                {
                    Result = new Tuple<object, string>(null, createTransactionResult.Item2);
                    return;
                }

                using var sendTransactionResult = _commandReceiver.SendTransaction(_session, ref _transaction);
                if (sendTransactionResult.Item1 is null)
                {
                    Result = new Tuple<object, string>(null, sendTransactionResult.Item2);
                    return;
                }

                using var historyResult = _commandReceiver.History(_session);
                if (historyResult.Item1 is null)
                {
                    Result = new Tuple<object, string>(null, historyResult.Item2);
                    return;
                }

                var balance = (historyResult.Item1 as IList<BalanceSheet>).Last().Balance;
                var paymentId = _transaction.Transaction.TxnId.ByteToHex();
                Result = new Tuple<object, string>(new
                {
                    balance = $"{balance}",
                    paymentId
                }, String.Empty);
            }
            catch (Exception ex)
            {
                Result = new Tuple<object, string>(null, ex.Message);
            }
            finally
            {
                _cmdFinishedEvent.Set();
            }

            await Task.CompletedTask;
        }
    }
}
