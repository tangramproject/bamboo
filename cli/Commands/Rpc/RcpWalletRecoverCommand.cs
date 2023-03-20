// Improved by ChatGPT 

using System;
using System.Threading;
using System.Threading.Tasks;
using BAMWallet.HD;
using Cli.Commands.Rpc;

namespace Cli.Commands.Rpc
{
    /// <summary>
    /// Command to recover transactions in a wallet.
    /// </summary>
    public class RpcWalletRecoverCommand : RpcBaseCommand
    {
        private readonly int _start;
        private readonly bool _recoverCompletely;

        /// <summary>
        /// Initializes a new instance of the <see cref="RpcWalletRecoverCommand"/> class.
        /// </summary>
        /// <param name="start">The index of the first transaction to recover.</param>
        /// <param name="recoverCompletely">Whether to recover all transactions.</param>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="cmdFinishedEvent">The command finished event.</param>
        /// <param name="session">The session.</param>
        public RpcWalletRecoverCommand(int start, bool recoverCompletely, IServiceProvider serviceProvider, ref AutoResetEvent cmdFinishedEvent, Session session)
            : base(serviceProvider, ref cmdFinishedEvent, session)
        {
            _start = start;
            _recoverCompletely = recoverCompletely;
        }

        /// <summary>
        /// Executes the command to recover transactions in a wallet.
        /// </summary>
        /// <param name="activeSession">The active session.</param>
        public override async Task Execute(Session activeSession = null)
        {
            try
            {
                using var wallet = await _commandReceiver.SyncWallet(_session);
                var result = wallet.RecoverTransactions(_session, _start, _recoverCompletely);
                Result = new Tuple<object, string>(result, null);
            }
            catch (Exception ex)
            {
                Result = new Tuple<object, string>(null, ex.Message);
            }
            finally
            {
                _cmdFinishedEvent.Set();
            }
        }
    }
}
