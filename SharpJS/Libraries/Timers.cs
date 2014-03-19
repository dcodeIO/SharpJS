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
using System;
using System.Collections.Generic;

namespace SharpJS.Libraries
{
    public class Timers : /* Constructor-less */ Module
    {
        private List<TimerHandle> RegisteredTimers = new List<TimerHandle>();
        private long Now = (int)(DateTime.Now.Ticks / 10000);

        public Timers(Context context) : base(context) {
            context.Ticked += (sender, e) => Tick();
        }

        public override Module RegisterGlobals() {
            Engine.SetGlobalFunction("setTimeout", new SetTimerDelegate(SetTimeout));
            Engine.SetGlobalFunction("clearTimeout", new ClearTimerDelegate(ClearTimeout));
            Engine.SetGlobalFunction("setInterval", new SetTimerDelegate(SetInterval));
            Engine.SetGlobalFunction("clearInterval", new ClearTimerDelegate(ClearInterval));
            Engine.SetGlobalFunction("setImmediate", new SetImmediateDelegate(SetImmediate));
            Engine.SetGlobalFunction("clearImmediate", new ClearTimerDelegate(ClearInterval));
            return this;
        }

        private void Tick() {
            Context.ExecutingModule = this;
            try {
                Now = (int)(DateTime.Now.Ticks / 10000);
                for (int i = 0; i < RegisteredTimers.Count; ) {
                    TimerHandle iHandle = RegisteredTimers[i];
                    if (iHandle.ExecuteAt > Now)
                        break;
                    if (!iHandle.IsInterval) {
                        RegisteredTimers.RemoveAt(i);
                    } else {
                        iHandle.ExecuteAt += iHandle.Delay;
                        i++;
                    }
                    try {
                        iHandle.Callback.Apply(Engine.Global, iHandle.Arguments);
                    } catch (Exception ex) {
                        if (!Context.HandleUncaughtException(ex))
                            throw;
                    }
                }
            } finally {
                Context.ExecutingModule = null;
            }
        }

        public bool MayExit() {
            foreach (TimerHandle iHandle in RegisteredTimers) {
                if (!iHandle.IsUnrefd)
                    return false;
            }
            return true;
        }

        private TimerHandle RegisterHandle(TimerHandle handle) {
            for (int i = 0; i < RegisteredTimers.Count; ++i) {
                TimerHandle iHandle = RegisteredTimers[i];
                if (iHandle.ExecuteAt > handle.ExecuteAt) {
                    RegisteredTimers.Insert(i, handle);
                    return handle;
                }
            }
            RegisteredTimers.Add(handle);
            return handle;
        }

        private bool UnregisterHandle(TimerHandle handle) {
            if (handle == null)
                return false;
            return RegisteredTimers.Remove(handle);
        }

        [JSFunction(Name = "setTimeout", IsEnumerable = true)]
        public TimerHandle SetTimeout(FunctionInstance callback, int delay, params object[] arguments) {
            return RegisterHandle(new TimerHandle(this, callback, delay, arguments, Now + delay, false));
        }

        [JSFunction(Name = "clearTimeout", IsEnumerable = true)]
        public void ClearTimeout(TimerHandle handle) {
            UnregisterHandle(handle);
        }

        [JSFunction(Name = "setInterval", IsEnumerable = true)]
        public TimerHandle SetInterval(FunctionInstance callback, int delay, params object[] arguments) {
            return RegisterHandle(new TimerHandle(this, callback, delay, arguments, Now + delay, true));
        }

        [JSFunction(Name = "clearInterval", IsEnumerable = true)]
        public void ClearInterval(TimerHandle handle) {
            UnregisterHandle(handle);
        }

        [JSFunction(Name = "setImmediate", IsEnumerable = true)]
        public TimerHandle SetImmediate(FunctionInstance callback, params object[] arguments) {
            return RegisterHandle(new TimerHandle(this, callback, 0, arguments, Now, false));
        }

        [JSFunction(Name = "clearImmediate", IsEnumerable = true)]
        public void ClearImmediate(TimerHandle handle) {
            UnregisterHandle(handle);
        }

        public class TimerHandle : ObjectInstance
        {
            public Module Module;
            public FunctionInstance Callback;
            public ArrayInstance Arguments;
            public int Delay;
            public bool IsInterval;
            public long ExecuteAt;
            public bool IsUnrefd;

            public TimerHandle(Module module, FunctionInstance callback, int delay, object[] arguments, long executeAt, bool isInterval) : base(module.Engine.Object.InstancePrototype) {
                Module = module;
                Callback = callback;
                Arguments = module.Engine.Array.Construct(arguments);
                Delay = delay;
                IsInterval = isInterval;
                ExecuteAt = executeAt;
                IsUnrefd = false;
            }

            [JSFunction(Name = "ref", IsEnumerable = true)]
            public void Ref() {
                IsUnrefd = false;
            }

            [JSFunction(Name = "unref", IsEnumerable = true)]
            public void Unref() {
                IsUnrefd = true;
            }
        }

        public delegate TimerHandle SetTimerDelegate(FunctionInstance callback, int delay, params object[] arguments);

        public delegate TimerHandle SetImmediateDelegate(FunctionInstance callback, params object[] arguments);

        public delegate void ClearTimerDelegate(TimerHandle handle);
    }
}
