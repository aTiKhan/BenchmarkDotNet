﻿using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Toolchains.CoreRt;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class BuildTimeoutTests: BenchmarkTestExecutor
    {
        public BuildTimeoutTests(ITestOutputHelper outputHelper) : base(outputHelper) { }

        [Fact]
        public void WhenBuildTakesMoreTimeThanTheTimeoutTheBuildIsCancelled()
        {
            if (!RuntimeInformation.Is64BitPlatform()) // CoreRT does not support 32bit yet
                return;

            // we use CoreRT on purpose because it takes a LOT of time to build it
            // so we can be sure that timeout = 1s should fail!
            var timeout = TimeSpan.FromSeconds(1);

            var config = ManualConfig.CreateEmpty()
                .WithBuildTimeout(timeout)
                .AddJob(Job.Dry
                    .WithRuntime(CoreRtRuntime.CoreRt50)
                    .WithToolchain(CoreRtToolchain.CreateBuilder()
                        .UseCoreRtNuGet(microsoftDotNetILCompilerVersion: "6.0.0-preview.1.21074.3") // we test against specific version to keep this test stable
                        .ToToolchain()));

            var summary = CanExecute<CoreRtBenchmark>(config, fullValidation: false);

            Assert.All(summary.Reports, report => Assert.False(report.BuildResult.IsBuildSuccess));
            Assert.All(summary.Reports, report => Assert.Contains("The configured timeout", report.BuildResult.ErrorMessage));
        }
    }

    public class Impossible
    {
        [Benchmark]
        public void Check() => Environment.FailFast("This benchmark should have been never executed because 1s is not enough to build CoreRT benchmark!");
    }
}
