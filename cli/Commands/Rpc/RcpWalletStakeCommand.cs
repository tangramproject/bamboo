// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BAMWallet.Extensions;
using BAMWallet.HD;
using BAMWallet.Model;
using Cli.Commands.Rpc;

namespace CLi.Commands.Rpc;

public class RcpWalletStakeCommand : RpcBaseCommand
{
    private readonly StakeCredentials _stakeCredentials;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="stakeCredentials"></param>
    /// <param name="serviceProvider"></param>
    /// <param name="cmdFinishedEvent"></param>
    /// <param name="session"></param>
    public RcpWalletStakeCommand(StakeCredentials stakeCredentials, IServiceProvider serviceProvider, ref AutoResetEvent cmdFinishedEvent, Session session)
        : base(serviceProvider, ref cmdFinishedEvent, session)
    {
        _stakeCredentials = stakeCredentials;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="activeSession"></param>
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
                Seed = _stakeCredentials.Seed.ToByte(),
                Passphrase = _stakeCredentials.Passphrase.ToByte(),
                RewardAddress = _stakeCredentials.RewardAddress.ToByte()
            };

            var messageResponse = await _commandReceiver.SendStakeCredentials(stakeCredentialsRequest,
                _stakeCredentials.NodePrivateKey.HexToByte(), _stakeCredentials.NodeToken.HexToByte(), in listOutputs);

            var obj = messageResponse.Value.Success == false ? null : new object();
            Result = new Tuple<object, string>(new { obj }, messageResponse.Value.Message);
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