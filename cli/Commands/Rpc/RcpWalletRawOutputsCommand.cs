// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.
// Improved by ChatGPT

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BAMWallet.Extensions;
using BAMWallet.HD;
using BAMWallet.Model;
using Cli.Commands.Rpc;

namespace Cli.Commands.Rpc
{
    public class RpcWalletRawOutputsCommand : RpcBaseCommand
    {
        public RpcWalletRawOutputsCommand(IServiceProvider serviceProvider, ref AutoResetEvent cmdFinishedEvent, Session session)
            : base(serviceProvider, ref cmdFinishedEvent, session)
        {
        }

        public override async Task Run(Session activeSession = null)
        {
            try
            {
                await _commandReceiver.SyncWallet(_session);
                var balances = _commandReceiver.GetBalances(_session);

                var listOutputs = balances.Select(balance => new OutputText
                {
                    C = balance.Commitment.C.ByteToHex(),
                    E = balance.Commitment.E.ByteToHex(),
                    N = balance.Commitment.N.ByteToHex(),
                    T = (sbyte)balance.Commitment.T
                }).ToList();

                Result = (new
                {
                    Outputs = listOutputs
                }, string.Empty);
            }
            catch (Exception ex)
            {
                Result = (null, ex.Message);
            }
            finally
            {
                _cmdFinishedEvent.Set();
            }
        }
    }
}
