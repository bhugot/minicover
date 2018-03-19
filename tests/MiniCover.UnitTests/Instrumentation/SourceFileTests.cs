using System.Linq;
using MiniCover.Extensions;
using MiniCover.Instrumentation;
using Mono.Cecil.Pdb;
using Mono.Cecil.Tests;
using Shouldly;
using Xunit;

namespace MiniCover.UnitTests.Instrumentation
{
    public class SourceFileTests : BaseTestFixture
    {
        [Fact]
        public void FromAssembly_Should_Retrieve_all_methods()
        {
            TestModule("sample.dll", module =>
            {
                var sources = SourceFile.FromAssembly(module.Assembly);
                sources.ShouldNotBeEmpty();
            }, typeof(PdbReaderProvider));
            
        }
    }
}