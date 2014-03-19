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
using SharpJS.Helpers;
using System;
using System.Collections;
using System.Diagnostics;

namespace SharpJS.Libraries
{
    public class Process : Module {

        public readonly ProcessConstructor Constructor;
        public readonly ProcessInstance Instance;

        public Process(Context context) : base(context) {
            Constructor = new ProcessConstructor(context, "Process", new ProcessInstance(context.Events.EventEmitter.InstancePrototype, false));
            Instance = Constructor.Construct();
            Populate();
        }

        public class ProcessConstructor : ConstructorFunction
        {
            public ProcessConstructor(Context context, string name, ObjectInstance instancePrototype) : base(context, name, instancePrototype) {
                Populate();
            }

            // Not callable from JS
            public ProcessInstance Construct() {
                return new ProcessInstance(Context, this.InstancePrototype);
            }
        }

        public class ProcessInstance : Events.EventEmitterInstance
        {
            private long StartTime;
            private OperatingSystem OS = Environment.OSVersion;
            private string _Title = "#js";
            private ArrayInstance _Argv;
            private int _MaxTickDepth = 1000;

            // Prototype constructor
            public ProcessInstance(ObjectInstance nextPrototype, bool extended) : base(nextPrototype, true) {
                if (!extended)
                    Populate();
            }

            // Instance constructor
            public ProcessInstance(Context context, ObjectInstance thisPrototype) : base(context, thisPrototype) {
                StartTime = DateTime.Now.Ticks;
                _Argv = Engine.Array.Construct(new object[] { "sharpjs" });
            }

            [JSProperty(Name = "pid", IsEnumerable = true)]
            public int Pid {
                get {
                    return System.Diagnostics.Process.GetCurrentProcess().Id;
                }
            }

            [JSProperty(Name = "title", IsEnumerable = true)]
            public string Title {
                get {
                    return _Title;
                }
                set {
                    _Title = value;
                }
            }

            [JSProperty(Name = "arch", IsEnumerable = true)]
            public string Arch {
                get {
                    string arch = System.Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
                    if (arch == null)
                        return "null";
                    switch (arch) {
                        case "AMD64":
                        case "IA64":
                            return "x64";
                        case "X86":
                            return "ia32";
                        default:
                            return arch.ToLower();
                    }
                }
            }

            [JSProperty(Name = "platform", IsEnumerable = true)]
            public string Platform {
                get {
                    string platform = OS.Platform.ToString();
                    switch (platform) {
                        case "Win32":
                        case "Win32NT":
                            return "win32";
                        default:
                            return platform.ToLower();
                    }
                }
            }

            [JSProperty(Name = "config", IsEnumerable = true)]
            public ObjectInstance Config {
                get {
                    return Engine.Object.Construct();
                }
            }

            [JSProperty(Name = "version", IsEnumerable = true)]
            public string Version {
                get {
                    return "1.0.0";
                }
            }

            [JSProperty(Name = "versions", IsEnumerable = true)]
            public ObjectInstance Versions {
                get {
                    ObjectInstance obj = Engine.Object.Construct();
                    obj.SetPropertyValue("sharpjs", Version, false);
                    return obj;
                }
            }

            [JSProperty(Name = "argv", IsEnumerable = true)]
            public ArrayInstance Argv {
                get {
                    return _Argv;
                }
                set {
                    _Argv = value;
                }
            }

            [JSProperty(Name = "maxTickDepth", IsEnumerable = true)]
            public int MaxTickDepth {
                get {
                    return _MaxTickDepth;
                }
                set {
                    _MaxTickDepth = value;
                }
            }

            [JSFunction(Name = "cwd", IsEnumerable = true)]
            public string Cwd() {
                return Context.Path.Normalize(System.IO.Directory.GetCurrentDirectory());
            }

            [JSProperty(Name = "env", IsEnumerable = true)]
            public ObjectInstance Env {
                get {
                    ObjectInstance env = Engine.Object.Construct();
                    IDictionary vars = Environment.GetEnvironmentVariables();
                    foreach (object key in vars.Keys) {
                        if (key is string)
                            env.SetPropertyValue(key as string, vars[key], false);
                    }
                    return env;
                }
            }

            [JSFunction(Name = "uptime", IsEnumerable = true)]
            public int Uptime() {
                return (int)((DateTime.Now.Ticks - StartTime) / 10000000);
            }

            [JSFunction(Name = "hrtime", IsEnumerable = true)]
            public ArrayInstance Hrtime() {
                long nanos = DateTime.Now.Ticks * 100;
                int seconds = (int)(nanos / 1000000000);
                nanos %= 1000000000;
                return Engine.Array.Construct(new object[] { seconds, (int)nanos });
            }

            [JSFunction(Name = "nextTick", IsEnumerable = true)]
            public void NextTick(FunctionInstance callback) {
                // node.js actually performs these callbacks at the end of the current tick, which
                // is nearly as fast as calling them synchronous regarding reaction time. We could
                // also do this with a seperate queue, though for now it's just an immediate.
                (Context.GetModule("timers") as Timers).SetImmediate(callback, new object[] { });
            }

            [JSFunction(Name = "memoryUsage", IsEnumerable = true)]
            public ObjectInstance MemoryUsage() {
                ObjectInstance obj = Engine.Object.Construct();
                // rss?
                obj.SetPropertyValue("heapUsed", (double)System.Diagnostics.Process.GetCurrentProcess().PrivateMemorySize64, false);
                obj.SetPropertyValue("heapTotal", (double)System.Diagnostics.Process.GetCurrentProcess().VirtualMemorySize64, false);
                return obj;
            }

            [JSFunction(Name = "kill", IsEnumerable = true)]
            public void Kill(int pid, [DefaultParameterValue("SIGTERM")] string signal = "SIGTERM") {
                throw new JavaScriptException(Engine, "Error", "not implemented");
            }

            [JSFunction(Name = "exit", IsEnumerable = true)]
            public void Exit([DefaultParameterValue(0)] int code = 0) {
                throw new ExitException(code);
            }

            [JSFunction(Name = "abort", IsEnumerable = true)]
            public void Abort() {
                throw new JavaScriptException(Engine, "Error", "not implemented");
            }
        }

        public class ExitException : Exception
        {
            public int Code;

            public ExitException(int code) {
                Code = code;
            }
        }
    }
}
