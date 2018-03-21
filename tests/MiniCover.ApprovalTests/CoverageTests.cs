using System;
using System.IO;
using System.Text;
using Microsoft.DotNet.Cli.Utils;
using MiniCover.HitServices;
using Shouldly;
using Xunit;

namespace MiniCover.ApprovalTests
{
    public class CoverageTests
    {
        [Fact]
        public void Execute()
        {
            var sampleRootDirectory = Path.Combine(new SolutionDir().GetRootPath(this.GetType()), "sample");
            var sampleSolution = Path.Combine(sampleRootDirectory, "Sample.sln");
            var toolPath = Path.Combine(sampleRootDirectory, "tools");
            var tempDirectory = Guid.NewGuid().ToString();
            var workdir = $"../coverage/{tempDirectory}";
            var commandRestore = Command.CreateDotNet("restore", new[] {sampleSolution, "--no-cache"});
            var result = commandRestore.Execute();
            result.ExitCode.ShouldBe(0);

            var commandBuild = Command.CreateDotNet("build", new[] {sampleSolution, "--no-restore"});
            result = commandBuild.Execute();
            result.ExitCode.ShouldBe(0);
            result = MiniCoverRunner.ExecuteInstrumenter(toolPath,workdir);
            result.ExitCode.ShouldBe(0);
            result.StdErr.ShouldBeNullOrEmpty();

            var coverageJson = ApprovaleHelper.ApplyCleanup(File.ReadAllText(Path.Combine(toolPath, workdir, "coverage.json")), sampleRootDirectory, tempDirectory);
            ApprovaleHelper.VerifyJson(coverageJson);

            result = MiniCoverRunner.ExecuteReset(toolPath, workdir);
            result.ExitCode.ShouldBe(0);
            result.StdErr.ShouldBeNullOrEmpty();

            var testProjects = Directory.EnumerateFiles(Path.Combine(sampleRootDirectory, "test"), "*.csproj",
                SearchOption.AllDirectories);
            foreach (var testProject in testProjects)
            {
                var commandTest = Command.CreateDotNet("test", new[] {testProject, "--no-build"});
                commandTest.CaptureStdOut();
                commandTest.CaptureStdErr();
                result = commandTest.Execute();
                result.ExitCode.ShouldBe(0);
                result.StdErr.ShouldBeNullOrEmpty();
            }
            result = MiniCoverRunner.ExecuteUninstrument(toolPath, workdir);
            result.ExitCode.ShouldBe(0);
            result.StdErr.ShouldBeNullOrEmpty();

            var bytes = ReplaceTextInFile(Path.Combine(toolPath, workdir, "coverage-hits.txt"), sampleRootDirectory);
         
            ApprovaleHelper.VerifyByte(bytes);
        }

        byte[] ReplaceTextInFile(string fileName, params string[] textToRemove)
        {
            byte[] fileBytes = File.ReadAllBytes(fileName);
            
            foreach(var toRemove in textToRemove)
            fileBytes = ReplaceTextInByteArray(toRemove.Replace('/', '\\')+ '\\'
                , string.Empty, fileBytes);

            return fileBytes;
        }

        private byte[] ReplaceTextInByteArray(string oldText, string newText, byte[] fileBytes)
        {
            byte[] oldBytes = Encoding.UTF8.GetBytes(oldText);
            byte[] newBytes = Encoding.UTF8.GetBytes(newText);
            return ReplaceBytes(fileBytes, oldBytes, newBytes);
        }

        private byte[] ReplaceBytes(byte[] fileBytes, byte[] oldBytes, byte[] newBytes)
        {
            
            byte[] newFileBytes = fileBytes;
            while (true)
            {
                int index = IndexOfBytes(newFileBytes, oldBytes);
                if (index < 0) break;
                var tempileBytes =
                new byte[newFileBytes.Length + newBytes.Length - oldBytes.Length];

                Buffer.BlockCopy(newFileBytes, 0, tempileBytes, 0, index);
                Buffer.BlockCopy(newBytes, 0, tempileBytes, index, newBytes.Length);
                Buffer.BlockCopy(newFileBytes, index + oldBytes.Length,
                    tempileBytes, index + newBytes.Length,
                    newFileBytes.Length - index - oldBytes.Length);
                newFileBytes = tempileBytes;
            }
            return newFileBytes;
        }

        int IndexOfBytes(byte[] searchBuffer, byte[] bytesToFind)
        {
            for (int i = 0; i < searchBuffer.Length - bytesToFind.Length; i++)
            {
                bool success = true;

                for (int j = 0; j < bytesToFind.Length; j++)
                {
                    if (searchBuffer[i + j] != bytesToFind[j])
                    {
                        success = false;
                        break;
                    }
                }

                if (success)
                {
                    return i;
                }
            }

            return -1;
        }

    }
}