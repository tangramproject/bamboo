// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.
// Improved by ChatGPT

using BAMWallet.Extensions;
using BAMWallet.HD;
using BAMWallet.Helper;
using Cli.Commands.Common;
using Cli.Commands.Extensions;
using Cli.Commands.Models;
using Cli.Commands.Services;
using Kurukuru;
using McMaster.Extensions.CommandLineUtils;
using System;
using System.Security;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Cli.Commands.CmdLine
{
    [CommandDescriptor("create", "Create new wallet")]
    class WalletCreateCommand : Command
    {
        private readonly IWalletService _walletService;

        public WalletCreateCommand(IServiceProvider serviceProvider, IWalletService walletService)
            : base(typeof(WalletCreateCommand), serviceProvider, true)
        {
            _walletService = walletService;
        }

        public override async Task Execute(Session activeSession = null)
        {
            var passphrase = new SecureString();
            var walletName = Prompt.GetString("Specify wallet file name (e.g., MyWallet):", string.Empty);
            var generatePassphrase = Prompt.GetYesNo("Generate a secure passphrase?", false);
            if (generatePassphrase)
            {
                passphrase = string.Join(" ", _walletService.CreateSeed(NBitcoin.WordCount.Twelve)).ToSecureString();
            }
            else
            {
                var generatePin = Prompt.GetYesNo("Generate a secure pin?", true);
                if (generatePin)
                {
                    passphrase = GenerateSecurePin();
                }
                else
                {
                    passphrase = Prompt.GetPasswordAsSecureString("Specify wallet passphrase or pin:");
                }
            }

            try
            {
                await Spinner.StartAsync("Creating wallet ...", async _ =>
                {
                    var defaultSeed = _walletService.CreateSeed(NBitcoin.WordCount.TwentyFour);
                    var joinMnemonic = string.Join(" ", defaultSeed);
                    var walletId = await _walletService.CreateWallet(joinMnemonic.ToSecureString(), passphrase, walletName);
                    var walletPath = Util.WalletPath(walletId);

                    _console.WriteLine();
                    _console.WriteLine("Your wallet has been generated!");
                    _console.WriteLine();
                    _console.WriteLine("NOTE:");
                    _console.WriteLine("The following 24 seed word can be used to recover your wallet.");
                    _console.WriteLine("Please write down the 24 seed word and store it somewhere safe and secure.");
                    _console.WriteLine("Your seed and passphrase/pin will not be displayed again!");
                    _console.WriteLine();
                    _console.WriteLine("Your wallet can be found here:");
                    _console.WriteLine($"{walletPath}");
                    _console.WriteLine();
                    _console.WriteLine("Wallet Name:");
                    _console.WriteLine($"{walletId}");
                    _console.WriteLine("Seed:");
                    _console.WriteLine($"{joinMnemonic}");
                    _console.WriteLine("Passphrase/Pin:");
                    _console.WriteLine($"{passphrase.FromSecureString()}");
                    _console.WriteLine();

                    joinMnemonic.ZeroString();
                    passphrase.ZeroString();

                    await Task.Delay(100);

                    return Task.CompletedTask;
                }, Patterns.Hearts);
            }
            catch (Exception ex)
            {
                _console.WriteError(ex.Message);
            }
        }

        private static SecureString GenerateSecurePin()
        {
            var buffer = new byte[sizeof(ulong)];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(buffer);
            var num = BitConverter.ToUInt64(buffer, 0);
            var pin = num % 100000000;
            return pin.ToString("D8").ToSecureString();
        }
    }
}
