using System;
using System.Threading;
using System.Threading.Tasks;
using BAMWallet.HD;
using Cli.Commands.Rpc;

namespace CLi.Commands.Rpc;

public class RcpWalletSyncCommand : RpcBaseCommand
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="cmdFinishedEvent"></param>
    /// <param name="session"></param>
    public RcpWalletSyncCommand(IServiceProvider serviceProvider, ref AutoResetEvent cmdFinishedEvent, Session session)
        : base(serviceProvider, ref cmdFinishedEvent, session)
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

            Result = new Tuple<object, string>(new
            {
                Success = true
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