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
using Jurassic.Library;
using SharpJS.Encodings;
using SharpJS.Libraries;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace SharpJS
{
    public class Context
    {
        public ScriptEngine Engine;
        private Dictionary<string, object> Modules = new Dictionary<string, object>();
        public Module ExecutingModule = null;
        public bool Initialized = false;
        private Queue<FunctionInstance> callbacks = new Queue<FunctionInstance>();

        public Libraries.Assert Assert;
        public Libraries.Util Util;
        public Libraries.Path Path;
        public Libraries.Events Events;
        public Libraries.Timers Timers;
        public Libraries.Buffer Buffer;
        public Libraries.StringDecoder StringDecoder;
        public Libraries.QueryString QueryString;
        // public Libraries.Stream Stream;
        public Libraries.Fs Fs;
        public Libraries.Console Console;
        public Libraries.Process Process;

        public Context() {
            Engine = new ScriptEngine();
            Engine.Global = new ContextGlobalObject(this);

            Modules.Add("assert", Assert = new Libraries.Assert(this));
            Modules.Add("util", Util = new Libraries.Util(this));
            Modules.Add("path", Path = new Libraries.Path(this));
            Modules.Add("events", Events = new Libraries.Events(this));
            Modules.Add("timers", Timers = new Libraries.Timers(this));
            Modules.Add("buffer", Buffer = new Libraries.Buffer(this));
            Modules.Add("string_decoder", StringDecoder = new Libraries.StringDecoder(this));
            Modules.Add("querystring", QueryString = new Libraries.QueryString(this));
            // Modules.Add("stream", Stream = new Libraries.Stream(this));
            Modules.Add("fs", Fs = new Libraries.Fs(this));
            Modules.Add("console", Console = new Libraries.Console(this));

            Process = new Libraries.Process(this);
            RegisterGlobals();
        }

        public event ExitEventHandler Exit;

        public delegate void ExitEventHandler(object sender, int code);

        public object Run(Module main) {
            ExecutingModule = main;
            try {
                object exports = main.Run();
                Initialized = true;
                return exports;
            } catch (Exception ex) {
                if (!HandleUncaughtException(ex))
                    throw;
            } finally {
                ExecutingModule = null;
            }
            return null;
        }

        public void RegisterGlobals() {
            Engine.PopulateGlobals();
            GlobalObject global = Engine.Global;
            global.SetPropertyValue("global", global, true);
            global.SetPropertyValue("require", new RequireFunction(this), true);
            global.SetPropertyValue("process", Process.Instance, true);
            foreach (KeyValuePair<string,object> kv in Modules) {
                if (kv.Value is Module)
                    (kv.Value as Module).RegisterGlobals();
            }
        }

        public bool AddModule(string name, object module) {
            if (Modules.ContainsKey(name))
                return false;
            Modules.Add(name, module);
            return true;
        }

        public object GetModule(string name) {
            object module = null;
            Modules.TryGetValue(name, out module);
            return module;
        }

        public bool MoveModule(string fromName, string toName) {
            object module = GetModule(fromName);
            if (module == null)
                return false;
            if (AddModule(toName, module)) {
                Modules.Remove(fromName);
                return true;
            }
            return false;
        }

        public bool ReplaceModule(string name, object replacementModule) {
            object module = GetModule(name);
            if (module == null)
                return false;
            Modules.Remove(name);
            if (AddModule(name, replacementModule)) {
                return true;
            }
            return false;
        }

        public bool RemoveModule(string name) {
            return Modules.Remove(name);
        }

        private string TimersRef = "timers";

        public bool MayExit() {
            Timers timers = (GetModule(TimersRef) as Timers);
            if (timers == null) {
                foreach (var module in Modules) {
                    if (module.Value is Timers) {
                        TimersRef = module.Key;
                        timers = module.Value as Timers;
                    }
                }
            }
            if (timers /* still */ == null) {
                return true;
            }
            return timers.MayExit();
        }

        public void OnExit() {
            Process.Instance.Emit("exit");
        }

        public class RequireFunction : FunctionInstance
        {
            public Context Context;

            public RequireFunction(Context context)
                : base(context.Engine.Function.InstancePrototype) {
                Context = context;
            }

            public override object CallLateBound(object thisObject, params object[] argumentValues) {
                if (argumentValues.Length == 0)
                    throw new JavaScriptException(Context.Engine, "TypeError", "null");
                return Context.Require(Context.ExecutingModule, argumentValues[0] as string);
            }
        }

        public object Require(Module callingModule, string name) {
            object exports = null;
            if (Modules.TryGetValue(name, out exports))
                return exports;
            if (callingModule != null)
                return callingModule.Require(name);
            throw new JavaScriptException(Engine, "Error", "Cannot find module '" + name + "'");
        }

        public bool HandleUncaughtException(Exception ex) {
            if (Initialized && ex is JavaScriptException) {
                if (Process.Instance.HasListener("uncaughtException")) {
                    Process.Instance.Emit("uncaughtException", ex);
                    return true;
                }
            } else if (ex is SharpJS.Libraries.Process.ExitException) {
                OnExit();
                if (Exit != null)
                    Exit(this, (ex as SharpJS.Libraries.Process.ExitException).Code);
                return true;
            }
            return false;
        }

        // Ticking

        public delegate void TickEventHandler(object sender, EventArgs e);

        public event TickEventHandler Ticked;

        public void Tick() {
            try {
                ProcessCallbacks();
                if (Ticked != null)
                    Ticked(this, EventArgs.Empty);
                ProcessCallbacks();
            } catch (Exception ex) {
                if (!HandleUncaughtException(ex))
                    throw;
            }
        }

        // Callback queue

        public void NextTick(FunctionInstance callback) {
            lock (callbacks) {
                callbacks.Enqueue(callback);
            }
        }

        private void ProcessCallbacks() {
            FunctionInstance[] cbs;
            lock (callbacks) {
                cbs = callbacks.ToArray();
                callbacks.Clear();
            }
            foreach (FunctionInstance cb in cbs) {
                try {
                    cb.CallLateBound(this);
                } catch (Exception ex) {
                    if (!HandleUncaughtException(ex))
                        throw;
                }
            }
        }

        // Encodings

        private static Dictionary<string, Encoding> Encodings = null;

        public static Dictionary<string, Encoding> GetEncodings() {
            if (Encodings == null) {
                Encodings = new Dictionary<string, Encoding>();
                Encodings.Add("utf8", System.Text.Encoding.UTF8);
                Encodings.Add("hex", new Hex());
                Encodings.Add("binary", new Binary());
                // Encodings.Add("base64", new Base64());
            }
            return Encodings;
        }

        public static Encoding GetEncoding(string name) {
            if (Encodings == null)
                GetEncodings();
            System.Text.Encoding encoding = null;
            Encodings.TryGetValue(name.ToLower(), out encoding);
            return encoding;
        }
    }

    public class ContextGlobalObject : GlobalObject
    {
        public ContextGlobalObject(Context context) : base(context.Engine.Object.InstancePrototype) {
        }

        /* public override bool DefineProperty(string propertyName, PropertyDescriptor descriptor, bool throwOnError) {
            // System.Console.WriteLine("define: " + propertyName + " / " + descriptor);
            return base.DefineProperty(propertyName, descriptor, throwOnError);
        }

        public override void SetPropertyValue(uint index, object value, bool throwOnError) {
            StackTrace st = new StackTrace();
            MethodBase caller = st.GetFrame(1).GetMethod();
            if (caller.DeclaringType != typeof(Context)) {
                // System.Console.WriteLine("intercepted: " + caller.Name);
                return;
            }
            // System.Console.WriteLine("not intercepted: "+caller.Name);
            base.SetPropertyValue(index, value, throwOnError);
        } */
    }
}
