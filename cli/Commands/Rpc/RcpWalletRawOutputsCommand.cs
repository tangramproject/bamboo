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

public class RcpWalletRawOutputsCommand: RpcBaseCommand
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="cmdFinishedEvent"></param>
    /// <param name="session"></param>
    public RcpWalletRawOutputsCommand(IServiceProvider serviceProvider, ref AutoResetEvent cmdFinishedEvent, Session session) : base(serviceProvider, ref cmdFinishedEvent, session)
    {
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
            var recover = _commandReceiver.RecoverTransactions(_session, 0);
            var balances = _commandReceiver.GetBalances(_session);
            var listOutputs = balances.Select(balance => new OutputText
            {
                C = balance.Commitment.C.ByteToHex(),
                E = balance.Commitment.E.ByteToHex(),
                N = balance.Commitment.N.ByteToHex(),
                T = (sbyte)balance.Commitment.T
            }).ToList();
            
            Result = new Tuple<object, string>(new
            {
                Outputs = listOutputs
            }, string.Empty);
            
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