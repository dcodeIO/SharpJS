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
using System.Collections.Generic;

namespace SharpJS.Libraries
{
    public class Events : Module
    {
        public readonly EventEmitterConstructor EventEmitter;

        public Events(Context context) : base(context) {
            EventEmitter = new EventEmitterConstructor(context, "EventEmitter", new EventEmitterInstance(context.Engine.Object.InstancePrototype, false));
            Populate();
            SetPropertyValue("EventEmitter", EventEmitter, true);
        }

        public class EventEmitterConstructor : ConstructorFunction
        {
            public int DefaultMaxListeners = 10;

            public EventEmitterConstructor(Context context, string name, ObjectInstance instancePrototype) : base(context, name, instancePrototype) {
                Populate();
            }

            [JSConstructorFunction]
            public EventEmitterInstance Construct() {
                return new EventEmitterInstance(Context, this);
            }

            [JSProperty(Name = "defaultMaxListeners", IsEnumerable = true)]
            public int defaultMaxListeners {
                get {
                    return DefaultMaxListeners;
                }
                set {
                    DefaultMaxListeners = value;
                }
            }
        }

        public class EventEmitterInstance : ContextObjectInstance
        {
            private Dictionary<string, List<FunctionInstance>> Listeners = new Dictionary<string, List<FunctionInstance>>();

            // Prototype constructor
            public EventEmitterInstance(ObjectInstance nextPrototype, bool extended) : base(nextPrototype, true) {
                if (!extended)
                    Populate();
            }

            // Instance constructor
            public EventEmitterInstance(Context context, ObjectInstance thisPrototype) : base(context, thisPrototype) {
            }

            [JSFunction(Name = "setMaxListeners", IsEnumerable = true)]
            public EventEmitterInstance SetMaxListeners(int n) {
                if (n < 0)
                    throw new JavaScriptException(Engine, "TypeError", "n must be a positive number");
                this.SetPropertyValue("_maxListeners", n, false);
                return this;
            }

            public bool HasListener(string type) {
                return Listeners.ContainsKey(type);
            }

            [JSFunction(Name = "addListener", IsEnumerable = true)]
            public EventEmitterInstance AddListener(string type, FunctionInstance listener) {
                if (type == null || listener == null)
                    throw new JavaScriptException(Engine, "TypeError", "null");
                List<FunctionInstance> listeners = null;
                Listeners.TryGetValue(type, out listeners);
                if (listeners == null) {
                    listeners = new List<FunctionInstance>();
                    Listeners.Add(type, listeners);
                } else if (listeners.Contains(listener)) {
                    return this;
                }
                listeners.Add(listener);
                Emit("newListener", type, listener is OnceListener ? (listener as OnceListener).Listener : listener);
                return this;
            }

            [JSFunction(Name = "on", IsEnumerable = true)]
            public EventEmitterInstance On(string type, FunctionInstance listener) {
                return AddListener(type, listener);
            }

            [JSFunction(Name = "once", IsEnumerable = true)]
            public EventEmitterInstance Once(string type, FunctionInstance listener) {
                if (listener == null)
                    throw new JavaScriptException(Engine, "TypeError", "null");
                return AddListener(type, new OnceListener(this, listener));
            }

            [JSFunction(Name = "removeListener", IsEnumerable = true)]
            public EventEmitterInstance RemoveListener(string type, FunctionInstance listener) {
                if (type == null || listener == null)
                    throw new JavaScriptException(Engine, "TypeError", "null");
                List<FunctionInstance> listeners = null;
                Listeners.TryGetValue(type, out listeners);
                if (listeners == null)
                    return this;
                if (listeners.Remove(listener)) {
                    Emit("removeListener", type, listener);
                }
                if (listeners.Count == 0) {
                    Listeners.Remove(type);
                }
                return this;
            }

            [JSFunction(Name = "removeAllListeners", IsEnumerable = true)]
            public EventEmitterInstance RemoveAllListeners() {
                return RemoveAllListeners(null);
            }

            [JSFunction(Name = "removeAllListeners", IsEnumerable = true)]
            public EventEmitterInstance RemoveAllListeners(string type) {
                if (type == null) {
                    Listeners.Clear();
                } else {
                    Listeners.Remove(type);
                }
                return this;
            }

            [JSFunction(Name = "emit", IsEnumerable = true)]
            public EventEmitterInstance Emit(string type, params object[] arguments) {
                if (type == null)
                    throw new JavaScriptException(Engine, "TypeError", "null");
                List<FunctionInstance> listeners = null;
                Listeners.TryGetValue(type, out listeners);
                if (listeners == null)
                    return this;
                foreach (var listener in listeners) {
                    if (listener is OnceListener) {
                        listeners.Remove(listener);
                        Emit("removeListener", type, listener);
                        (listener as OnceListener).Listener.Apply(this, Engine.Array.Construct(arguments));
                    } else {
                        listener.Apply(this, Engine.Array.Construct(arguments));
                    }
                }
                return this;
            }

            public class OnceListener : FunctionInstance
            {
                public FunctionInstance Listener;

                public OnceListener(EventEmitterInstance eventEmitter, FunctionInstance listener) : base(eventEmitter.Engine.Function.InstancePrototype) {
                    Listener = listener;
                }

                public override object CallLateBound(object thisObject, params object[] argumentValues) {
                    throw new System.NotImplementedException();
                }
            }
        }
    }
}
