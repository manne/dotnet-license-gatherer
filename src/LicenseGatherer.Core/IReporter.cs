using System;

namespace LicenseGatherer.Core
{
    /// <summary>
    /// Gathers messages with levels.
    /// </summary>
    public interface IReporter
    {
        /// <summary>
        /// Report console output.
        /// </summary>
        /// <param name="message"></param>
        void Output(string message);

        /// <summary>
        /// Report console output.
        /// </summary>
        /// <param name="message"></param>
        void OutputInvariant(FormattableString message);

        /// <summary>
        /// Report an error.
        /// </summary>
        /// <param name="message"></param>
#pragma warning disable CA1716 // Identifiers should not match keywords
        void Error(string message);
#pragma warning restore CA1716 // Identifiers should not match keywords
    }
}
