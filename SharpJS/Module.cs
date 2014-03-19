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

namespace SharpJS
{
    public class Module : ObjectInstance
    {
        public Context Context;

        public Module(Context context) : base(context.Engine.Object.InstancePrototype) {
            Context = context;
        }

        public void Populate() {
            PopulateFields();
            PopulateFunctions();
        }

        public object Require(string name) {
            // TODO
            throw new JavaScriptException(Engine, "Error", "Cannot find module '" + name + "'");
        }

        public virtual Module RegisterGlobals() {
            return this;
        }

        public virtual object Run() {
            return this;
        }
    }
}
