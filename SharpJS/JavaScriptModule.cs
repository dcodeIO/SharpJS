/*
 Copyright 2014 Daniel Wirtz <dcode@dcode.io>

 Licensed under the Apache License, Version 2.0 (the "License");
 you may not use this file except in compliance with the License.
 You may obtain a copy of the License at

 http://www.apache.org/licenses/LICENSE-2.0

 Unless required by applicable law or agreed to in writing, software
 distributed under the License is distributed on an "AS IS" BASIS,
 WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 See the License for the specific language governing permissions and
 limitations under the License.
 */
using Jurassic;
using Jurassic.Compiler;
using Jurassic.Library;
using System;
using System.Collections.Generic;
using System.IO;

namespace SharpJS
{
    class JavaScriptModule : Module
    {
        private string Code;
        private string Path;
        public ScriptSource Source = null;
        private FunctionInstance Require;

        private static string Wrap(string code) {
            // For now we have to force strict mode so that a JS module cannot set any properties
            // on the one and only global scope.
            return "(function(module, exports, require, __filename, __dirname) { \"use strict\"; " + code + "})";
        }

        public JavaScriptModule(Context context, string code, string path) : base(context) {
            Code = code;
            Path = path;
            Initialize();
        }

        public JavaScriptModule(Context context, string path) : base(context) {
            Path = context.Path.Resolve(path);
            StreamReader sr = File.OpenText(path);
            try {
                Code = sr.ReadToEnd();
            } finally {
                sr.Close();
            }
            Initialize();
        }

        private void Initialize() {
            Require = new RequireFunction(this);
        }

        private void Validate(string code) {
            // When wrapping the code for CommonJS, we have to make sure that the code we wrap
            // is valid so that it cannot inject other functions on the top level scope, like
            // if it'd contain "}); (function() {". So we have to validate that before wrapping:
            Engine.TryParse(new StringScriptSource(code, Path)); // throws SyntaxError
        }

        public class RequireFunction : FunctionInstance
        {
            public JavaScriptModule Module;
            public ObjectInstance Modules;

            public RequireFunction(JavaScriptModule module) : base(module.Context.Engine.Function.InstancePrototype) {
                Module = module;
                Modules = Module.Engine.Object.Construct();
                SetPropertyValue("cache", Modules, false);
            }

            public override object CallLateBound(object thisObject, params object[] args) {
                if (args.Length == 0)
                    throw new JavaScriptException(Module.Context.Engine, "TypeError", "null");
                if (!(args[0] is string))
                    throw new JavaScriptException(Module.Context.Engine, "TypeError", "illegal module name");
                string filename = args[0] as string;
                // Lookup global module, like "fs"
                object exports = Module.Context.TryRequire(filename);
                if (exports != null)
                    return exports;
                // Lookup local module
                if (Modules.HasProperty(filename))
                    return Modules[filename];
                // Else try to load a local JavaScript module
                string dirname = Module.Context.Path.Dirname(Module.Source.Path);
                filename = Module.Context.Fs.CheckPath(Module.Context.Path.Join(dirname, filename));
                try {
                    if (!File.Exists(filename))
                        filename = filename + ".js";
                    StreamReader sr = File.OpenText(filename);
                    try {
                        string code = sr.ReadToEnd();
                        JavaScriptModule module = new JavaScriptModule(Module.Context, code, filename);
                        Modules.SetPropertyValue(filename, exports = module.Run(), true);
                        return exports;
                    } finally {
                        sr.Close();
                    }
                } catch (Exception ex) {
                    throw new JavaScriptException(Module.Context.Engine, "Error", ex.Message);
                }
            }
        }

        public override object Run() {
            if (Source == null) {
                Validate(Code);
                Source = new StringScriptSource(Wrap(Code), Path);
                Code = null;
                Path = null;
            }
            FunctionInstance wrap = (FunctionInstance)Engine.Evaluate(Source);
            ObjectInstance module = Engine.Object.Construct();
            ObjectInstance exports = Engine.Object.Construct();
            module.SetPropertyValue("exports", exports, true);
            string filename = System.IO.Path.GetFullPath(Source.Path);
            string dirname = System.IO.Path.GetDirectoryName(filename);
            wrap.CallLateBound(this, new object[] { module, exports, Require, filename, dirname });
            return module.GetPropertyValue("exports");
        }

        /* public override object Run() {
            // Switching the global context here requires some monkey patches to
            // Jurassic to a) access the GlobalObject constructor, b) assign
            // a replacement global object to the engine and c) recreate the
            // initial global namespace on the new GlobalObject.

            GlobalObject savedGlobal = Engine.Global;
            Engine.Global = new GlobalObject(Engine.Object.InstancePrototype); // a), b)
            Engine.PopulateGlobals(); // c)

            // Register SharpJS globals
            Context.RegisterGlobals();

            // Register module global
            ObjectInstance module = Engine.Object.Construct(),
                           exports = Engine.Object.Construct();
            Engine.SetGlobalValue("module", module);
            module.SetPropertyValue("exports", exports, false);
            Engine.SetGlobalValue("exports", exports);

            // And run it
            try {
                Engine.Evaluate(Source);
                return module.GetPropertyValue("exports");
            } finally {
                Engine.Global = savedGlobal;
            }
        } */
    }
}
