using LicenseGatherer.Core;

namespace LicenseGatherer
{
    public class Reporter : IReporter
    {
        private readonly McMaster.Extensions.CommandLineUtils.IReporter _reporter;

        public Reporter(McMaster.Extensions.CommandLineUtils.IReporter reporter)
        {
            _reporter = reporter;
        }

        public void Verbose(string message)
        {
            _reporter.Verbose(message);
        }

        public void Output(string message)
        {
            _reporter.Output(message);
        }

        public void Error(string message)
        {
            _reporter.Error(message);
        }
    }
}
