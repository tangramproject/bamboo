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
    class RpcWalletListCommand : RpcBaseCommand
    {
        public RpcWalletListCommand(IServiceProvider serviceProvider, ref AutoResetEvent cmdFinishedEvent)
            : base(serviceProvider, ref cmdFinishedEvent, null)
        {
        }

        public override async Task Execute(Session activeSession = null)
        {
            try
            {
                var request = _commandReceiver.WalletList();
                Result = new Tuple<object, string>(request.Item1, request.Item2);
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
