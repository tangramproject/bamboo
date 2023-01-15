using System;
using System.Threading;
using System.Threading.Tasks;
using BAMWallet.HD;
using Cli.Commands.Rpc;

namespace CLi.Commands.Rpc;

public class RcpWalletRecoverCommand : RpcBaseCommand
{
    private readonly int _start;
    private readonly bool _recoverCompletely;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="start"></param>
    /// <param name="recoverCompletely"></param>
    /// <param name="serviceProvider"></param>
    /// <param name="cmdFinishedEvent"></param>
    /// <param name="session"></param>
    public RcpWalletRecoverCommand(int start, bool recoverCompletely, IServiceProvider serviceProvider, ref AutoResetEvent cmdFinishedEvent, Session session) : base(serviceProvider, ref cmdFinishedEvent, session)
    {
        _start = start;
        _recoverCompletely = recoverCompletely;
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
            Result = _commandReceiver.RecoverTransactions(_session, _start, _recoverCompletely);
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