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

namespace CLi.Commands.Rpc
{
    public class RpcWalletStakeCommand : RpcBaseCommand
    {
        private readonly StakeCredentials _stakeCredentials;

        public RpcWalletStakeCommand(
            StakeCredentials stakeCredentials,
            IServiceProvider serviceProvider,
            ref AutoResetEvent cmdFinishedEvent,
            Session session)
            : base(serviceProvider, ref cmdFinishedEvent, session)
        {
            _stakeCredentials = stakeCredentials;
        }

        public override async Task Execute(Session activeSession = null)
        {
            try
            {
                await _commandReceiver.SyncWallet(_session);
                var balances = _commandReceiver.GetBalances(in _session);

                var listOutputs = balances.Select(balance => new Output
                {
                    C = balance.Commitment.C,
                    E = balance.Commitment.E,
                    N = balance.Commitment.N,
                    T = balance.Commitment.T
                }).ToArray();

                var stakeCredentialsRequest = new StakeCredentialsRequest
                {
                    Seed = _stakeCredentials.Seed,
                    Passphrase = _stakeCredentials.Passphrase,
                    RewardAddress = _stakeCredentials.RewardAddress
                };

                var messageResponse = await _commandReceiver.SendStakeCredentials(
                    stakeCredentialsRequest,
                    _stakeCredentials.NodePrivateKey.HexToByte(),
                    _stakeCredentials.NodeToken.HexToByte(),
                    in listOutputs);

                var obj = messageResponse.Value.Success ? new object() : null;
                Result = (obj, messageResponse.Value.Message);
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
