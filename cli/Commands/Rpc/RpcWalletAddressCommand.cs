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
    class RpcWalletAddressCommand : RpcBaseCommand
    {
        public RpcWalletAddressCommand(IServiceProvider serviceProvider, ref AutoResetEvent cmdFinishedEvent, Session session)
            : base(serviceProvider, ref cmdFinishedEvent, session)
        {
        }

        public override async Task Execute(Session activeSession = null)
        {
            try
            {
                Result = _commandReceiver.Address(_session);
            }
            catch (Exception ex)
            {
                Result = (null, ex.Message);
            }
            finally
            {
                _cmdFinishedEvent.Set();
            }

            await Task.CompletedTask;
        }
        
        public void Dispose()
        {
            _cmdFinishedEvent.Dispose();
            _session.Dispose();
        }
    }
}
