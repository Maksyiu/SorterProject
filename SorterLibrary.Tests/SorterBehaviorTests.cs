using Microsoft.VisualStudio.TestPlatform.Utilities;
using System.Diagnostics;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace SorterLibrary.Tests
{
    public class SorterBehaviorTests
    {
        private readonly ITestOutputHelper _output;

        public SorterBehaviorTests(ITestOutputHelper output)
        {
            _output = output;
        }        

        [Fact]
        public async Task TestSortSmallFile()
        {
            int fileMaxSize = 2 * 1024; // 2KB

            Sorter sorter = new Sorter();
            var test = Environment.CurrentDirectory;
            string source = @$"{test}\TestData\smallFile.txt";
            string output = @$"{test}\TestData\smallFile_output.txt";
            await sorter.Run(source, output, test, CancellationToken.None, fileMaxSize);

            List<string> expected = File.ReadAllLines(@$"{test}\TestData\testSmallFile_expected.txt").ToList();

            List<string> result = File.ReadAllLines(@$"{test}\TestData\smallFile_output.txt").ToList();

            Assert.True(expected.SequenceEqual(result));
        }

        [Fact]
        public async Task TestSortLargeFile()
        {
            int fileMaxSize = 2 * 1024; // 2KB

            Sorter sorter = new Sorter();
            var test = Environment.CurrentDirectory;
            string source = @$"{test}\TestData\bigFile.txt";
            string output = $"{source}_output.txt";
            await sorter.Run(source, output, test, CancellationToken.None, fileMaxSize);

            List<string> expected = File.ReadAllLines(@$"{test}\TestData\testBigFile_expected.txt").ToList();

            List<string> result = File.ReadAllLines($"{source}_output.txt").ToList();

            Assert.True(expected.SequenceEqual(result));
        }
    }
}
