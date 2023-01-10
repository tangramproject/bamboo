using System;
using System.Threading;
using System.Threading.Tasks;
using BAMWallet.HD;
using Cli.Commands.Rpc;

namespace CLi.Commands.Rpc;

public class RcpVersionCommand : RpcBaseCommand
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="cmdFinishedEvent"></param>
    public RcpVersionCommand(IServiceProvider serviceProvider, ref AutoResetEvent cmdFinishedEvent) :
        base(serviceProvider, ref cmdFinishedEvent, null)
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="activeSession"></param>
    /// <returns></returns>
    public override Task Execute(Session activeSession = null)
    {
        Result = new Tuple<object, string>(new
        {
            Version = BAMWallet.Helper.Util.GetAssemblyVersion()
        }, string.Empty);

        _cmdFinishedEvent.Set();

        return Task.CompletedTask;
    }
}