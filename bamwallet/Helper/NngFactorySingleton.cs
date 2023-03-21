// CypherNetwork BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.IO;
using nng;

namespace BAMWallet.Helper
{
    public sealed class NngFactorySingleton
    {
        private static readonly Lazy<NngFactorySingleton> Lazy = new(() => new NngFactorySingleton());
        public static NngFactorySingleton Instance => Lazy.Value;

        public IAPIFactory<INngMsg> Factory { get; }

        private NngFactorySingleton()
        {
            try
            {
                // Get the path of the managed assembly
                var managedAssemblyPath = Path.GetDirectoryName(GetType().Assembly.Location);

                // Initialize the NNG load context
                var alc = new NngLoadContext(managedAssemblyPath);
                Factory = NngLoadContext.Init(alc);
            }
            catch (Exception ex)
            {
                // Handle any exceptions thrown during initialization
                Console.WriteLine($"Failed to initialize NNG factory: {ex.Message}");
                throw;
            }
        }
    }
}
