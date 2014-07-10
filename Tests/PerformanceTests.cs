﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Linq;
using JSIL.Internal;
using JSIL.Translator;
using NUnit.Framework;

namespace JSIL.Tests {
    [TestFixture]
    public class PerformanceTests : GenericTestFixture {
        Configuration MakeUnsafeConfiguration () {
            var cfg = MakeConfiguration();
            cfg.CodeGenerator.EnableUnsafeCode = true;
            cfg.CodeGenerator.AggressivelyUseElementProxies = true;
            return cfg;
        }

        [Test]
        public void BinaryTrees () {
            using (var test = MakeTest(@"TestCases\BinaryTrees.cs")) {
                test.Run();
                test.Run(new[] { "8" });
            }
        }

        [Test]
        public void NBody () {
            using (var test = MakeTest(@"TestCases\NBody.cs")) {
                test.Run();
                test.Run(new[] { "100000" });
            }
        }

        [Test]
        public void FannkuchRedux () {
            using (var test = MakeTest(@"TestCases\FannkuchRedux.cs")) {
                test.Run();
                test.Run(new[] { "8" });
            }
        }

        [Test]
        public void UnsafeIntPerformanceComparison () {
            using (var test = MakeTest(
                @"PerformanceTestCases\UnsafeIntPerformanceComparison.cs"
            )) {
                Console.WriteLine(test.RunJavascript(null, MakeUnsafeConfiguration));
            }
        }

        [Test]
        public void EnumCasts () {
            using (var test = MakeTest(
                @"PerformanceTestCases\EnumCasts.cs"
            )) {
                test.Run();
            }
        }

        [Test]
        public void Sieve () {
            using (var test = MakeTest(
                @"PerformanceTestCases\Sieve.cs"
            )) {
                long elapsedcs;

                Console.WriteLine("C#:\r\n{0}", test.RunCSharp(null, out elapsedcs));
                Console.WriteLine("JS:\r\n{0}", test.RunJavascript(null, makeConfiguration: MakeUnsafeConfiguration));
            }
        }

        [Test]
        public void Vector3 () {
            using (var test = MakeTest(
                @"PerformanceTestCases\Vector3.cs"
            )) {
                long elapsedcs;

                Console.WriteLine("C#:\r\n{0}", test.RunCSharp(null, out elapsedcs));
                Console.WriteLine("JS:\r\n{0}", test.RunJavascript(null, makeConfiguration: MakeUnsafeConfiguration));
            }
        }

        [Test]
        public void FuseePackedVertices () {
            using (var test = MakeTest(
                @"PerformanceTestCases\FuseePackedVertices.cs"
            )) {
                long elapsedcs;

                Console.WriteLine("C#:\r\n{0}", test.RunCSharp(null, out elapsedcs));
                Console.WriteLine("JS:\r\n{0}", test.RunJavascript(null, makeConfiguration: MakeUnsafeConfiguration));
            }
        }

        [Test]
        public void OverloadedMethodCalls () {
            using (var test = MakeTest(
                @"PerformanceTestCases\OverloadedMethodCalls.cs"
            )) {
                long elapsedcs;

                Console.WriteLine("C#:\r\n{0}", test.RunCSharp(null, out elapsedcs));
                Console.WriteLine("JS:\r\n{0}", test.RunJavascript(null));
            }
        }

        [Test]
        public void OverloadedConstructors () {
            using (var test = MakeTest(
                @"PerformanceTestCases\OverloadedConstructors.cs"
            )) {
                long elapsedcs;

                Console.WriteLine("C#:\r\n{0}", test.RunCSharp(null, out elapsedcs));
                Console.WriteLine("JS:\r\n{0}", test.RunJavascript(null));
            }
        }

        [Test]
        public void UncachedOverloadedMethodCalls () {
            using (var test = MakeTest(
                @"PerformanceTestCases\UncachedOverloadedMethodCalls.cs"
            )) {
                long elapsedcs;

                Console.WriteLine("C#:\r\n{0}", test.RunCSharp(null, out elapsedcs));
                Console.WriteLine("JS:\r\n{0}", test.RunJavascript(null));
            }
        }

        [Test]
        public void InterfaceMethodCalls () {
            using (var test = MakeTest(
                @"PerformanceTestCases\InterfaceMethodCalls.cs"
            )) {
                long elapsedcs;

                Console.WriteLine("C#:\r\n{0}", test.RunCSharp(null, out elapsedcs));
                Console.WriteLine("JS:\r\n{0}", test.RunJavascript(null));
            }
        }

        [Test]
        public void GenericInterfaceMethodCalls () {
            using (var test = MakeTest(
                @"PerformanceTestCases\GenericInterfaceMethodCalls.cs"
            )) {
                long elapsedcs;

                Console.WriteLine("C#:\r\n{0}", test.RunCSharp(null, out elapsedcs));
                Console.WriteLine("JS:\r\n{0}", test.RunJavascript(null));
            }
        }

        [Test]
        public void VariantGenericInterfaceMethodCalls () {
            using (var test = MakeTest(
                @"PerformanceTestCases\VariantGenericInterfaceMethodCalls.cs"
            )) {
                long elapsedcs;

                Console.WriteLine("C#:\r\n{0}", test.RunCSharp(null, out elapsedcs));
                Console.WriteLine("JS:\r\n{0}", test.RunJavascript(null));
            }
        }

        [Test]
        public void RectangleIntersects () {
            using (var test = MakeTest(
                @"PerformanceTestCases\RectangleIntersects.cs"
            )) {
                long elapsedcs;

                Console.WriteLine("C#:\r\n{0}", test.RunCSharp(null, out elapsedcs));
                Console.WriteLine("JS:\r\n{0}", test.RunJavascript(null));
            }
        }

        [Test]
        public void PropertyVsField () {
            using (var test = MakeTest(
                @"PerformanceTestCases\PropertyVsField.cs"
            )) {
                long elapsedcs;

                Console.WriteLine("C#:\r\n{0}", test.RunCSharp(null, out elapsedcs));
                Console.WriteLine("JS:\r\n{0}", test.RunJavascript(
                    null,
                    makeConfiguration: () => {
                        var cfg = MakeConfiguration();
                        cfg.CodeGenerator.PreferAccessorMethods = true;
                        return cfg;
                    }
                ));
            }
        }

        [Test]
        public void BaseMethodCalls () {
            using (var test = MakeTest(
                @"PerformanceTestCases\BaseMethodCalls.cs"
            )) {
                long elapsedcs;

                Console.WriteLine("C#:\r\n{0}", test.RunCSharp(null, out elapsedcs));
                Console.WriteLine("JS:\r\n{0}", test.RunJavascript(null));
            }
        }
    }

    [TestFixture]
    public class PerformanceAnalysisTests : GenericTestFixture {
        Configuration MakeUnsafeConfiguration () {
            var cfg = MakeConfiguration();
            cfg.CodeGenerator.EnableUnsafeCode = true;
            return cfg;
        }

        protected override Dictionary<string, string> SetupEvaluatorEnvironment () {
            return new Dictionary<string, string> {
                { "INFERFLAGS", "result" }
            };
        }

        protected override string JSShellOptions {
            get {
                return "--ion-eager --thread-count=0";
            }
        }

        protected override bool UseDebugJSShell {
            get {
                return true;
            }
        }

        private void AssertIsSingleton (PerformanceAnalysisData data, string expression) {
            Assert.IsTrue(data[expression].IsSingleton, expression + " is not a singleton");
        }

        // FIXME: Latest js.exe breaks this test.
        [Ignore]
        [Test]
        public void PointerMethodsAreSingletons () {
            using (var test = MakeTest(@"PerformanceTestCases\PointerMethodsAreSingletons.cs")) {
                var data = new PerformanceAnalysisData(test, MakeUnsafeConfiguration);

                Console.WriteLine(data.Output);

                try {
                    AssertIsSingleton(data, "pBuffer.getElement");
                    AssertIsSingleton(data, "pBuffer.setElement");
                    // FIXME: Fails. Something about this function makes SpiderMonkey unhappy :-(
                    AssertIsSingleton(data, "Program.TestInlineAccess");
                } catch (Exception) {
                    data.Dump(Console.Out);
                    throw;
                }
            }
        }
    }    
}
