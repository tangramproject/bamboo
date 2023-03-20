//Improved by ChatGPT

using System;
using System.Threading;
using System.Threading.Tasks;
using BAMWallet.Extensions;
using BAMWallet.HD;
using BAMWallet.Helper;
using Cli.Commands.Rpc;

namespace Cli.Commands.Rpc
{
    public class RpcWalletRestoreCommand : RpcBaseCommand
    {
        private readonly string _walletName;
        private readonly string _seed;
        private readonly string _passphrase;
        private readonly IServiceProvider _serviceProvider;
        private readonly AutoResetEvent _cmdFinishedEvent;

        public RpcWalletRestoreCommand(string walletName, string seed, string passphrase, IServiceProvider serviceProvider, ref AutoResetEvent cmdFinishedEvent)
            : base(serviceProvider, ref cmdFinishedEvent, null)
        {
            _walletName = walletName;
            _seed = seed;
            _passphrase = passphrase;
            _serviceProvider = serviceProvider;
            _cmdFinishedEvent = cmdFinishedEvent;
        }

        public override async Task Execute(Session session = null)
        {
            try
            {
                using var commandReceiver = _serviceProvider.GetRequiredService<ICommandReceiver>();
                var id = await commandReceiver.CreateWallet(_seed.ToSecureString(), _passphrase.ToSecureString(), _walletName).ConfigureAwait(false);
                var result = new
                {
                    Path = Util.WalletPath(id),
                    Identifier = id,
                };
                Result = new Tuple<object, string>(result, string.Empty);
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
}
