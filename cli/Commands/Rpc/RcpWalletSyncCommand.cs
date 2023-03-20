// Improved by ChatGPT

using System;
using System.Threading;
using System.Threading.Tasks;
using BAMWallet.HD;

namespace Cli.Commands.Rpc
{
    public class RpcWalletSyncCommand : RpcBaseCommand
    {
        public RpcWalletSyncCommand(IServiceProvider serviceProvider, ref AutoResetEvent cmdFinishedEvent)
            : base(serviceProvider, ref cmdFinishedEvent)
        {
        }

        /// <summary>
        /// Synchronizes the wallet.
        /// </summary>
        public override async Task Execute()
        {
            try
            {
                await _commandReceiver.SyncWallet();
                Result = new { Success = true };
            }
            catch (Exception ex) when (ex is BAMWalletException || ex is OperationCanceledException)
            {
                Result = new { Success = false, ErrorMessage = ex.Message };
            }
            finally
            {
                _cmdFinishedEvent.Set();
            }
        }
    }
}
