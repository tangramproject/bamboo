using System;
using System.Threading;
using System.Threading.Tasks;
using BAMWallet.Extensions;
using BAMWallet.HD;
using BAMWallet.Helper;
using Cli.Commands.Rpc;

namespace CLi.Commands.Rpc;

public class RpcWalletRestoreCommand: RpcBaseCommand
{
    private readonly string _walletName;
    private readonly string _seed;
    private readonly string _passphrase;
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="walletName"></param>
    /// <param name="seed"></param>
    /// <param name="passphrase"></param>
    /// <param name="serviceProvider"></param>
    /// <param name="cmdFinishedEvent"></param>
    public RpcWalletRestoreCommand(string walletName, string seed, string passphrase, IServiceProvider serviceProvider, ref AutoResetEvent cmdFinishedEvent)
        : base(serviceProvider, ref cmdFinishedEvent, null)
    {
        _walletName = walletName;
        _seed = seed;
        _passphrase = passphrase;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="activeSession"></param>
    public override async Task Execute(Session activeSession = null)
    {
        try
        {
            var id = await _commandReceiver.CreateWallet(_seed.ToSecureString(), _passphrase.ToSecureString(), _walletName);
            Result = new Tuple<object, string>(new
            {
                path = Util.WalletPath(id),
                identifier = id,
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