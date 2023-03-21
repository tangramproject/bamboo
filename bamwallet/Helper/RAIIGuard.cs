using System;
using Dawn;

namespace BAMWallet.Helper
{
    /// <summary>
    /// Implements the RAII (Resource Acquisition Is Initialization) pattern.
    /// </summary>
    public sealed class RAIIGuard : IDisposable
    {
        /// <summary>
        /// Gets the action to unprotect the resource.
        /// </summary>
        private Action Unprotect { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RAIIGuard"/> class.
        /// </summary>
        /// <param name="initializeResource">The action to initialize the resource.</param>
        /// <param name="unprotect">The action to unprotect the resource.</param>
        public RAIIGuard(Action initializeResource, Action unprotect)
        {
            Guard.Argument(initializeResource, nameof(initializeResource)).NotNull();
            Guard.Argument(unprotect, nameof(unprotect)).NotNull();

            Unprotect = unprotect;
            initializeResource();
        }

        /// <summary>
        /// Disposes the object and unprotects the resource.
        /// </summary>
        public void Dispose()
        {
            Unprotect();
        }
    }
}
