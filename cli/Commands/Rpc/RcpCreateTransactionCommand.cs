// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.
// Improved by ChatGPT

using System;
using System.Threading.Tasks;
using BAMWallet.HD;
using BAMWallet.Model;

namespace Cli.Commands.Rpc
{
    class RpcCreateTransactionCommand : RpcBaseCommand
    {
        private readonly WalletTransaction _transaction;

        public RpcCreateTransactionCommand(WalletTransaction tx)
        {
            _transaction = tx;
        }

        public override async Task Execute(Session activeSession = null)
        {
            try
            {
                var result = await _commandReceiver.CreateTransaction(_session, ref _transaction);
                Result = new Tuple<object, string>(result, null);
            }
            catch (SpecificException ex)
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
