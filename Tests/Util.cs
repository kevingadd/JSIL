﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.IO;
using System.Diagnostics;
using System.Runtime.Serialization.Json;
using System.Threading;
using NUnit.Framework;
using System.Globalization;

namespace JSIL.Tests {
    public class JavaScriptException : Exception {
        public readonly string ErrorText;

        public JavaScriptException (int exitCode, string stdout, string stderr)
            : base(String.Format("JavaScript interpreter exited with code {0}\r\n{1}\r\n{2}", exitCode, stdout, stderr)) 
        {
            ErrorText = stderr;
        }
    }

    public static class CSharpUtil {
        public static string TempPath;

        // Attempt to clean up stray assembly files from previous test runs
        //  since the assemblies would have remained locked and undeletable 
        //  due to being loaded
        static CSharpUtil () {
            TempPath = Path.Combine(Path.GetTempPath(), "JSIL Tests");
            if (!Directory.Exists(TempPath))
                Directory.CreateDirectory(TempPath);

            foreach (var filename in Directory.GetFiles(TempPath))
                try {
                    File.Delete(filename);
                } catch {
                }
        }

        public static Assembly Compile (string sourceCode, out TempFileCollection temporaryFiles) {
            using (var csc = new CSharpCodeProvider(new Dictionary<string, string>() { 
                { "CompilerVersion", "v4.0" } 
            })) {

                var parameters = new CompilerParameters(new[] {
                    "mscorlib.dll", "System.dll", "System.Core.dll", "Microsoft.CSharp.dll",
                    typeof(JSIL.Meta.JSIgnore).Assembly.Location
                }) {
                    CompilerOptions = "/unsafe",
                    GenerateExecutable = true,
                    GenerateInMemory = false,
                    IncludeDebugInformation = true,
                    TempFiles = new TempFileCollection(TempPath, true)
                };

                var results = csc.CompileAssemblyFromSource(parameters, sourceCode);

                if (results.Errors.Count > 0) {
                    throw new Exception(
                        String.Join(Environment.NewLine, results.Errors.Cast<CompilerError>().Select((ce) => ce.ToString()).ToArray())
                    );
                }

                temporaryFiles = results.TempFiles;
                return results.CompiledAssembly;
            }
        }
    }

    public class ComparisonTest : IDisposable {
        public const float JavascriptExecutionTimeout = 30.0f;

        public static readonly Regex ElapsedRegex = new Regex(
            @"// elapsed: (?'elapsed'[0-9]*(\.[0-9]*)?)", RegexOptions.Compiled | RegexOptions.ExplicitCapture
        );

        protected TempFileCollection TemporaryFiles;

        public static readonly string TestSourceFolder;
        public static readonly string JSShellPath;
        public static readonly string CoreJSPath, BootstrapJSPath;

        public readonly TypeInfoProvider TypeInfo;
        public readonly Regex[] StubbedAssemblies;
        public readonly string Filename;
        public readonly Assembly Assembly;
        public readonly MethodInfo TestMethod;

        static string GetPathOfAssembly (Assembly assembly) {
            var uri = new Uri(assembly.CodeBase);
            var result = Uri.UnescapeDataString(uri.AbsolutePath);
            if (String.IsNullOrWhiteSpace(result))
                result = assembly.Location;

            return result;
        }

        static ComparisonTest () {
            var testAssembly = typeof(ComparisonTest).Assembly;
            var assemblyPath = Path.GetDirectoryName(GetPathOfAssembly(testAssembly));

            TestSourceFolder = Path.GetFullPath(Path.Combine(assemblyPath, @"..\"));
            JSShellPath = Path.GetFullPath(Path.Combine(assemblyPath, @"..\..\Upstream\SpiderMonkey\js.exe"));
            CoreJSPath = Path.GetFullPath(Path.Combine(TestSourceFolder, @"..\Libraries\JSIL.Core.js"));
            BootstrapJSPath = Path.GetFullPath(Path.Combine(TestSourceFolder, @"..\Libraries\JSIL.Bootstrap.js"));
        }

        public ComparisonTest (string filename, Regex[] stubbedAssemblies = null, TypeInfoProvider typeInfo = null) {
            Filename = Path.Combine(TestSourceFolder, filename);

            var sourceCode = File.ReadAllText(Filename);
            Assembly = CSharpUtil.Compile(sourceCode, out TemporaryFiles);

            TestMethod = Assembly.GetType("Program").GetMethod("Main");

            StubbedAssemblies = stubbedAssemblies;
            TypeInfo = typeInfo;
        }

        public void Dispose () {
            foreach (string filename in TemporaryFiles)
                try {
                    File.Delete(filename);
                } catch {
                }
        }

        public string RunCSharp (string[] args, out long elapsed) {
            var oldStdout = Console.Out;
            using (var sw = new StringWriter())
                try {
                    Console.SetOut(sw);
                    long startedCs = DateTime.UtcNow.Ticks;
                    TestMethod.Invoke(null, new object[] { args });
                    long endedCs = DateTime.UtcNow.Ticks;
                    elapsed = endedCs - startedCs;
                    return sw.ToString();
                } finally {
                    Console.SetOut(oldStdout);
                }
        }

        public string RunJavascript (string[] args, out string generatedJavascript, out long elapsedTranslation, out long elapsedJs) {
            var tempFilename = Path.GetTempFileName();
            var translator = new JSIL.AssemblyTranslator(TypeInfo) {
                IncludeDependencies = false
            };

            if (StubbedAssemblies != null)
                translator.StubbedAssemblies.AddRange(StubbedAssemblies);

            string translatedJs;
            var translationStarted = DateTime.UtcNow.Ticks;
            using (var ms = new MemoryStream()) {
                var assemblies = translator.Translate(
                    GetPathOfAssembly(Assembly), ms, 
                    TypeInfo == null
                );
                translatedJs = Encoding.ASCII.GetString(ms.GetBuffer(), 0, (int)ms.Length);

                // If we're using a preconstructed type information provider, we need to remove the type information
                //  from the assembly we just translated
                if (TypeInfo != null) {
                    Assert.AreEqual(1, assemblies.Length);
                    TypeInfo.Remove(assemblies);
                }
            }
            elapsedTranslation = DateTime.UtcNow.Ticks - translationStarted;

            var declaringType = JSIL.Internal.Util.EscapeIdentifier(TestMethod.DeclaringType.FullName, Internal.EscapingMode.TypeIdentifier);

            string argsJson;
            var jsonSerializer = new DataContractJsonSerializer(typeof(string[]));
            using (var ms2 = new MemoryStream()) {
                jsonSerializer.WriteObject(ms2, args);
                argsJson = Encoding.UTF8.GetString(ms2.GetBuffer(), 0, (int)ms2.Length);
            }

            var invocationJs = String.Format(
                @"timeout({0}); JSIL.Initialize(); JSIL.Host.warnedAboutRunLater = true; var started = elapsed(); {1}.Main({2}); var ended = elapsed(); print('// elapsed: ' + (ended - started));", 
                JavascriptExecutionTimeout, declaringType, argsJson
            );

            generatedJavascript = translatedJs;

            File.WriteAllText(tempFilename, translatedJs + Environment.NewLine + invocationJs);

            try {
                // throw new Exception();

                var psi = new ProcessStartInfo(JSShellPath, String.Format("-w -j -m -f \"{0}\" -f \"{1}\" -f \"{2}\"", CoreJSPath, BootstrapJSPath, tempFilename)) {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                ManualResetEventSlim stdoutSignal, stderrSignal;
                stdoutSignal = new ManualResetEventSlim(false);
                stderrSignal = new ManualResetEventSlim(false);
                var output = new string[2];

                long startedJs = DateTime.UtcNow.Ticks;
                using (var process = Process.Start(psi)) {
                    ThreadPool.QueueUserWorkItem((_) => {
                        output[0] = process.StandardOutput.ReadToEnd();
                        stdoutSignal.Set();
                    });
                    ThreadPool.QueueUserWorkItem((_) => {
                        output[1] = process.StandardError.ReadToEnd();
                        stderrSignal.Set();
                    });

                    stdoutSignal.Wait();
                    stderrSignal.Wait();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                        throw new JavaScriptException(
                            process.ExitCode,
                            (output[0] ?? "").Trim(),
                            (output[1] ?? "").Trim()
                        );
                }

                long endedJs = DateTime.UtcNow.Ticks;
                elapsedJs = endedJs - startedJs;

                if (output[0] != null) {
                    var m = ElapsedRegex.Match(output[0]);
                    if (m.Success) {
                        elapsedJs = TimeSpan.FromMilliseconds(
                            double.Parse(m.Groups["elapsed"].Value)
                        ).Ticks;
                        output[0] = output[0].Replace(m.Value, "");
                    }
                }

                return output[0] ?? "";
            } finally {
                var jsFile = Filename.Replace(".cs", ".js");
                if (File.Exists(jsFile))
                    File.Delete(jsFile);
                File.Copy(tempFilename, jsFile);

                File.Delete(tempFilename);
            }
        }

        public void Run (params string[] args) {
            var signals = new[] {
                new ManualResetEventSlim(false), new ManualResetEventSlim(false)
            };
            var generatedJs = new string[1];
            var errors = new Exception[2];
            var outputs = new string[2];
            var elapsed = new long[3];

            ThreadPool.QueueUserWorkItem((_) => {
                var oldCulture = Thread.CurrentThread.CurrentCulture;
                try {
                    Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                    outputs[0] = RunCSharp(args, out elapsed[0]).Replace("\r", "").Trim();
                } catch (Exception ex) {
                    errors[0] = ex;
                } finally {
                    Thread.CurrentThread.CurrentCulture = oldCulture;
                }
                signals[0].Set();
            });

            ThreadPool.QueueUserWorkItem((_) => {
                try {
                    outputs[1] = RunJavascript(args, out generatedJs[0], out elapsed[1], out elapsed[2]).Replace("\r", "").Trim();
                } catch (Exception ex) {
                    errors[1] = ex;
                }
                signals[1].Set();
            });

            signals[0].Wait();
            signals[1].Wait();

            try {
                if (errors[0] != null)
                    throw new Exception("C# test failed", errors[0]);
                else if (errors[1] != null)
                    throw new Exception("JS test failed", errors[1]);
                else
                    Assert.AreEqual(outputs[0], outputs[1]);

                Console.WriteLine(
                    "passed: C#:{0:00.00}s JSIL:{1:00.00}s JS:{2:00.00}s",
                    TimeSpan.FromTicks(elapsed[0]).TotalSeconds,
                    TimeSpan.FromTicks(elapsed[1]).TotalSeconds,
                    TimeSpan.FromTicks(elapsed[2]).TotalSeconds
                );
            } catch {
                Console.WriteLine("failed");
                if (outputs[0] != null) {
                    Console.WriteLine("// C# output begins //");
                    Console.WriteLine(outputs[0]);
                }
                if (outputs[1] != null) {
                    Console.WriteLine("// JavaScript output begins //");
                    Console.WriteLine(outputs[1]);
                }
                if (generatedJs[0] != null) {
                    Console.WriteLine("// Generated javascript begins here //");
                    Console.WriteLine(generatedJs[0]);
                    Console.WriteLine("// Generated javascript ends here //");
                }

                throw;
            }
        }
    }

    public class GenericTestFixture {
        protected TypeInfoProvider MakeDefaultProvider () {
            // Construct a type info provider with default proxies loaded (kind of a hack)
            return (new AssemblyTranslator(null)).TypeInfoProvider;
        }

        protected void RunComparisonTests (string[] filenames, Regex[] stubbedAssemblies = null, TypeInfoProvider typeInfo = null) {
            foreach (var filename in filenames) {
                Debug.WriteLine(String.Format("// {0}", filename));

                using (var test = new ComparisonTest(filename, stubbedAssemblies, typeInfo))
                    test.Run();
            }
        }

        protected string GetJavascript (string fileName, string expectedText = null) {
            long elapsed, temp;
            string generatedJs;

            using (var test = new ComparisonTest(fileName)) {
                var output = test.RunJavascript(new string[0], out generatedJs, out temp, out elapsed);

                if (expectedText != null)
                    Assert.AreEqual(expectedText, output.Trim());
            }

            return generatedJs;
        }

        protected string GenericTest (string fileName, string csharpOutput, string javascriptOutput, Regex[] stubbedAssemblies = null) {
            long elapsed, temp;
            string generatedJs;

            using (var test = new ComparisonTest(fileName, stubbedAssemblies)) {
                var csOutput = test.RunCSharp(new string[0], out elapsed);
                var jsOutput = test.RunJavascript(new string[0], out generatedJs, out temp, out elapsed);

                Assert.AreEqual(csharpOutput, csOutput.Trim(), "Did not get expected output from C# test");
                Assert.AreEqual(javascriptOutput, jsOutput.Trim(), "Did not get expected output from JavaScript test");
            }

            return generatedJs;
        }

        protected string GenericIgnoreTest (string fileName, string workingOutput, string jsErrorSubstring, Regex[] stubbedAssemblies = null) {
            long elapsed, temp;
            string generatedJs = null;

            using (var test = new ComparisonTest(fileName, stubbedAssemblies)) {
                var csOutput = test.RunCSharp(new string[0], out elapsed);
                Assert.AreEqual(workingOutput, csOutput.Trim());

                try {
                    test.RunJavascript(new string[0], out generatedJs, out temp, out elapsed);
                    Assert.Fail("Expected javascript to throw an exception containing the string \"" + jsErrorSubstring + "\".");
                } catch (JavaScriptException jse) {
                    if (!jse.ErrorText.Contains(jsErrorSubstring))
                        throw;
                }

            }

            return generatedJs;
        }
    }
}
