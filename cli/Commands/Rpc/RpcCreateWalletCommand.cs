// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.
// Improved by ChatGPT

using System;
using System.Threading;
using System.Threading.Tasks;
using BAMWallet.Extensions;
using BAMWallet.HD;
using BAMWallet.Helper;
using NBitcoin;

namespace Cli.Commands.Rpc
{
    // Create a new wallet using RPC
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
                // Create a new seed and passphrase
                var defaultSeed = _commandReceiver.CreateSeed(WordCount.TwentyFour);
                var mnemonic = $"{string.Join(" ", defaultSeed)}";
                var defaultPass = _commandReceiver.CreateSeed(WordCount.Fifteen);
                var passphrase = $"{string.Join(" ", defaultPass)}";

                // Create a new wallet
                var walletId = await _commandReceiver.CreateWallet(mnemonic.ToSecureString(), passphrase.ToSecureString(), _walletName);
                var session = new Session(walletId.ToSecureString(), passphrase.ToSecureString());

                // Set the result
                Result = new Tuple<object, string>(new
                {
                    Path = Util.WalletPath(walletId),
                    Identifier = walletId,
                    Seed = mnemonic,
                    Passphrase = passphrase,
                    Address = session.KeySet.StealthAddress
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
