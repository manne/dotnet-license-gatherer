using System;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using Xunit;

namespace LicenseGatherer.Core.Tests
{
    public class EntryPointLocatorTests
    {

        [Fact]
        public void GivenOnePathToOneDirectory_WhenInThisDirectoryIsNotAnySolution_ThenOneDirectoryNotFoundException_ShouldBeThrown()
        {
            var mockFileSystem = new MockFileSystem();
            var cut = new EntryPointLocator(mockFileSystem);

            Action action = () => cut.GetEntryPoint(@"c:\foo\bar\");
            action.Should().Throw<DirectoryNotFoundException>();
        }

        [Fact]
        public void GivenOnePathToOneProjectFile_WhenInThisFileDoesNotExist_ThenOneDirectoryNotFoundException_ShouldBeThrown()
        {
            var mockFileSystem = new MockFileSystem();
            mockFileSystem.AddDirectory(@"c:\foo\bar");
            var cut = new EntryPointLocator(mockFileSystem);

            Action action = () => cut.GetEntryPoint(@"c:\foo\bar\aa.fooproj");
            action.Should().Throw<FileNotFoundException>();
        }

        [Fact]
        public void GivenOnePathToOneDirectory_WhenInThisDirectoryDoesNotContainOneSolutionFile_ThenOneException_ShouldBeThrown()
        {
            var mockFileSystem = new MockFileSystem();
            mockFileSystem.AddDirectory(@"c:\foo\bar");
            mockFileSystem.AddFile(@"c:\foo\bar\xyz.txt", new MockFileData(""));
            var cut = new EntryPointLocator(mockFileSystem);

            Action action = () => cut.GetEntryPoint(@"c:\foo\bar\");
            action.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void GivenOnePathToOneDirectory_WhenThisDirectoryContainsTwoSolutionFiles_ThenOneException_ShouldBeThrown()
        {
            var mockFileSystem = new MockFileSystem();
            mockFileSystem.AddFile(@"c:\foo\bar\xyz.sln", new MockFileData(""));
            mockFileSystem.AddFile(@"c:\foo\bar\g.sln", new MockFileData(""));
            var cut = new EntryPointLocator(mockFileSystem);

            Action action = () => cut.GetEntryPoint(@"c:\foo\bar\");
            action.Should().Throw<InvalidOperationException>();
        }
    }
}
