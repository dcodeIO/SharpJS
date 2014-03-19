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

namespace SharpJS.Libraries
{
    public class Stream : Module
    {      
        public readonly ReadableConstructor Readable;
        public readonly WritableConstructor Writable;

        public Stream(Context context) : base(context) {
            Readable = new ReadableConstructor(context, "Readable", new ReadableInstance(Engine.Object.InstancePrototype, false));
            Writable = new WritableConstructor(context, "Writable", new WritableInstance(Engine.Object.InstancePrototype, false));
            Populate();
            SetPropertyValue("Readable", Readable, true);
            SetPropertyValue("Writable", Writable, true);
        }

// Readable

        public class ReadableConstructor : ConstructorFunction
        {
            private System.Text.Encoding Encoding = null;
            private bool flowing = false;

            public ReadableConstructor(Context context, string name, ReadableInstance instancePrototype)  : base(context, name, instancePrototype) {
            }

            [JSConstructorFunction]
            public ReadableInstance Construct() {
                return new ReadableInstance(Context, this.InstancePrototype);
            }

            [JSFunction(Name = "setEncoding", IsEnumerable = true)]
            public void SetEncoding(string encoding) {
                var enc = Context.GetEncoding(encoding);
                if (enc == null)
                    throw new JavaScriptException(Engine, "TypeError", "Unknown encoding: "+encoding);
                Encoding = enc;
            }

            [JSFunction(Name = "resume", IsEnumerable = true)]
            public void Resume() {
                if (flowing)
                    return;
                flowing = true;
            }

            [JSFunction(Name = "pause", IsEnumerable = true)]
            public void Pause() {
                if (!flowing)
                    return;
                flowing = false;
            }


        }

        public class ReadableInstance : Events.EventEmitterInstance
        {
            // Prototype constructor
            public ReadableInstance(ObjectInstance nextPrototype, bool extended) : base(nextPrototype, true) {
                if (!extended)
                    Populate();
            }

            // Instance constructor
            public ReadableInstance(Context context, ObjectInstance thisPrototype) : base(context, thisPrototype) {
            }
        }

// Writable

        public class WritableConstructor : ConstructorFunction
        {
            public WritableConstructor(Context context, string name, ObjectInstance instancePrototype) : base(context, name, instancePrototype) {
            }

            [JSConstructorFunction]
            public WritableInstance Construct() {
                return new WritableInstance(Context, this.InstancePrototype);
            }
        }

        public class WritableInstance : Events.EventEmitterInstance
        {
            // Prototype constructor
            public WritableInstance(ObjectInstance nextPrototype, bool extended) : base(nextPrototype, true) {
                if (!extended)
                    Populate();
            }

            // Instance constructor
            public WritableInstance(Context context, ObjectInstance thisPrototype) : base(context, thisPrototype) {
            }
        }
    }
}
