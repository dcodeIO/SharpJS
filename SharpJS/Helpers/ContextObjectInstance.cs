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
using Jurassic.Library;

namespace SharpJS.Helpers
{
    public abstract class ContextObjectInstance : ObjectInstance
    {
        public readonly Context Context;

        /// <summary>
        /// Constructs the object's prototype, not bound to any context.
        /// </summary>
        /// <param name="nextPrototype">Prototype of the prototype</param>
        /// <param name="extended">Whether extending a base object or not</param>
        public ContextObjectInstance(ObjectInstance nextPrototype, bool extended) : base(nextPrototype) {
            // This one derives from ObjectInstance directly, so this is not necessary:
            // if (!extended) {
                // Populate();
            // }
            // It's necessary in all other inherited objects, though.
        }

        /// <summary>
        /// Constructs an object instance, bound to the given context.
        /// </summary>
        /// <param name="context">Context</param>
        /// <param name="thisPrototype">Prototype of this instance</param>
        public ContextObjectInstance(Context context, ObjectInstance thisPrototype) : base(thisPrototype) {
            Context = context;
        }

        /// <summary>
        /// Populates fields and functions.
        /// </summary>
        public void Populate() {
            PopulateFields();
            PopulateFunctions();
        }
    }
}
