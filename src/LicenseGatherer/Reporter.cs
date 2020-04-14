using System;
using LicenseGatherer.Core;

namespace LicenseGatherer
{
    public enum OutputType
    {
        JSON,
        CSV
    }

    public class Reporter : IReporter
    {
        private readonly McMaster.Extensions.CommandLineUtils.IReporter _reporter;

        public Reporter(McMaster.Extensions.CommandLineUtils.IReporter reporter)
        {
            _reporter = reporter;
        }

        public void Output(string message)
        {
            _reporter.Output(message);
        }

        public void OutputInvariant(FormattableString message)
        {
            Output(FormattableString.Invariant(message));
        }

        public void Error(string message)
        {
            _reporter.Error(message);
        }
    }
}
