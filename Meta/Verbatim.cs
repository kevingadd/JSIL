﻿using System;

namespace JSIL {
    public static class Verbatim {
        /// <summary>
        /// When running as C#, this method does nothing and returns null.
        /// When running as JavaScript, the passed-in script code replaces this method call.
        /// </summary>
        /// <param name="javascript">The script expression.</param>
        public static dynamic Expression (string javascript) {
            return null;
        }

        /// <summary>
        /// When running as C#, this method does nothing and returns null.
        /// When running as JavaScript, the passed-in script code replaces this method call.
        /// Variables can be referenced by index. '$0' is the first variable.
        /// </summary>
        /// <param name="javascript">The script expression.</param>
        /// <param name="variables">The variables to insert into the expression.</param>
        public static dynamic Expression (string javascript, params object[] variables) {
            return null;
        }

        /// <summary>
        /// When running as C#, this method does nothing and returns null.
        /// When running as JavaScript, the passed-in script code replaces this method call.
        /// </summary>
        /// <param name="javascript">The script expression.</param>
        /// <typeparam name="T">The return type.</typeparam>
        public static T Expression<T>(string javascript) {
          return default(T);
        }

        /// <summary>
        /// When running as C#, this method does nothing and returns null.
        /// When running as JavaScript, the passed-in script code replaces this method call.
        /// Variables can be referenced by index. '$0' is the first variable.
        /// </summary>
        /// <param name="javascript">The script expression.</param>
        /// <param name="variables">The variables to insert into the expression.</param>
        /// <typeparam name="T">The return type.</typeparam>
        public static T Expression<T>(string javascript, params object[] variables)
        {
          return default(T);
        }
    }
}
