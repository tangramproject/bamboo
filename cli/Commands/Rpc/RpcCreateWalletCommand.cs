// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Threading;
using System.Threading.Tasks;
using BAMWallet.Extensions;
using BAMWallet.HD;
using BAMWallet.Helper;
using NBitcoin;

namespace Cli.Commands.Rpc
{
    class RpcCreateWalletCommand : RpcBaseCommand
    {
        private readonly string _walletName;

        public RpcCreateWalletCommand(string walletName, IServiceProvider serviceProvider, ref AutoResetEvent cmdFinishedEvent)
            : base(serviceProvider, ref cmdFinishedEvent, null)
        {
            _walletName = walletName;
        }

        public override async Task Execute(Session activeSession = null)
        {
            try
            {
                var defaultSeed = _commandReceiver.CreateSeed(NBitcoin.WordCount.TwentyFour);
                var mnemonic = string.Join(" ", defaultSeed);
                var defaultPass = _commandReceiver.CreateSeed(NBitcoin.WordCount.Fifteen);
                var pass = string.Join(" ", defaultPass);
                var id = await _commandReceiver.CreateWallet(mnemonic.ToSecureString(), pass.ToSecureString(), _walletName);
                var session = new Session(id.ToSecureString(), pass.ToSecureString());

                Result = new Tuple<object, string>(new
                {
                    path = Util.WalletPath(id),
                    identifier = id,
                    seed = mnemonic,
                    passphrase = pass,
                    address = session.KeySet.StealthAddress
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
}
