﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JSIL.Compiler.Extensibility.DeadCodeAnalyzer {
    public class Configuration {
        private readonly bool _DeadCodeElimination;
        private readonly bool _NonAggressiveVirtualMethodElimination;
        private readonly IList<string> _WhiteList;
        private readonly IList<string> _AssembliesWhiteList;

        public Configuration(IDictionary<string, object> configuration) {
            _DeadCodeElimination = configuration.ContainsKey("DeadCodeElimination") &&
                                  configuration["DeadCodeElimination"] is bool &&
                                  ((bool) configuration["DeadCodeElimination"]);

            _NonAggressiveVirtualMethodElimination = configuration.ContainsKey("NonAggressiveVirtualMethodElimination") &&
                                  configuration["NonAggressiveVirtualMethodElimination"] is bool &&
                                  ((bool)configuration["NonAggressiveVirtualMethodElimination"]);
    
            if (configuration.ContainsKey("WhiteList") &&
                configuration["WhiteList"] is IList) {
                _WhiteList = ((IList) configuration["WhiteList"]).Cast<string>().ToList();
            }

            if (configuration.ContainsKey("AssembliesWhiteList") && configuration["AssembliesWhiteList"] is IList)
            {
                _AssembliesWhiteList = ((IList)configuration["AssembliesWhiteList"]).Cast<string>().ToList();
            }
        }

        public bool DeadCodeElimination
        {
            get { return _DeadCodeElimination; }
        }

        public bool NonAggressiveVirtualMethodElimination
        {
            get { return _NonAggressiveVirtualMethodElimination; }
        }

        public IList<string> WhiteList
        {
            get { return _WhiteList; }
        }

        public IList<string> AssembliesWhiteList
        {
            get { return _AssembliesWhiteList; }
        }
    }
}
