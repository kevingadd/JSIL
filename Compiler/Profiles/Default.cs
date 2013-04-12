﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using JSIL.Compiler.Extensibility;
using JSIL.Utilities;

namespace JSIL.Compiler.Profiles {
    public class Default : BaseProfile {
#if !__MonoCS__
        public override bool IsAppropriateForSolution (SolutionBuilder.BuildResult buildResult) {
            // Normally we'd return true so that this profile is always selected, but this is our fallback profile.
            return false;
        }
#endif
        public virtual TranslationResult Translate (AssemblyTranslator translator, Configuration configuration, string assemblyPath, bool scanForProxies) {
            var result = translator.Translate(assemblyPath, scanForProxies);

            ResourceConverter.ConvertResources(configuration, assemblyPath, result);

            AssemblyTranslator.GenerateManifest(translator.Manifest, assemblyPath, result);

            return result;
        }

#if !__MonoCS__
        public override SolutionBuilder.BuildResult ProcessBuildResult (VariableSet variables, Configuration configuration, SolutionBuilder.BuildResult buildResult) {
            CopiedOutputGatherer.GatherFromProjectFiles(
                variables, configuration, buildResult
            );

            return base.ProcessBuildResult(variables, configuration, buildResult);
        }
#endif
    }
}
