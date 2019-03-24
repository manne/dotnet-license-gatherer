namespace LicenseGatherer.Core
{
    public class Environment : IEnvironment
    {
        public string CurrentDirectory
        {
            get => System.Environment.CurrentDirectory;
            set => System.Environment.CurrentDirectory = value;
        }
    }
}
