// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.
// Improved by ChatGPT

using System;
using System.Threading;
using System.Threading.Tasks;
using BAMWallet.HD;

namespace Cli.Commands.Rpc
{
    public class RpcWalletTxHistoryCommand : RpcBaseCommand
    {
        private readonly ICommandReceiver _commandReceiver;

        public RcpWalletTxHistoryCommand(IServiceProvider serviceProvider, ref AutoResetEvent cmdFinishedEvent, Session session)
            : base(serviceProvider, ref cmdFinishedEvent, session)
        {
            _commandReceiver = serviceProvider.GetService(typeof(ICommandReceiver)) as ICommandReceiver;
        }

        public override async Task Execute(Session activeSession = null)
        {
            try
            {
                await _commandReceiver.SyncWallet(_session);
                var historyResult = await _commandReceiver.History(_session);
                Result = historyResult;
            }
            catch (TimeoutException ex)
            {
                Result = new Tuple<object, string>(null, "Timeout occurred while executing the command");
                // Handle the timeout exception appropriately
            }
            catch (Exception ex)
            {
                Result = new Tuple<object, string>(null, ex.Message);
                // Handle other exceptions appropriately
            }
            finally
            {
                _cmdFinishedEvent.Set();
            }
        }
    }
}
