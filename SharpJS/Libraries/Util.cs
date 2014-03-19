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
    public class Util : /* Constructor-less */ Module
    {
        public Util(Context context) : base(context) {
            Populate();
        }

        public static bool IsNull(object obj) {
            return obj == Null.Value;
        }

        public static bool IsUndefined(object obj) {
            return /* does not exist */ obj == null || /* explicit */ obj == Undefined.Value;
        }

        public static bool IsNullOrUndefined(object obj) {
            return obj == null || obj == Null.Value || obj == Undefined.Value;
        }

        public static bool IsBoolean(object obj) {
            return obj is bool;
        }

        public static bool IsNumber(object obj) {
            return obj is int || obj is double;
        }

        public static bool IsString(object obj) {
            return obj is string || obj is ConcatenatedString;
        }

        // public static bool isSymbol(object obj) {
        // }

        public static bool IsObject(object obj) {
            return IsNull(obj) || obj is ObjectInstance;
        }

        public static bool IsArray(object obj) {
            return obj is ArrayInstance;
        }

        public static bool IsFunction(object obj) {
            return obj is FunctionInstance;
        }

        // [JSFunction(Name = "inherits", IsEnumerable = true)]
        // public void Inherits(ObjectInstance ctor, ObjectInstance superCtor) {
        // }

        // public string Inspect(object obj) {
        //    return Inspect(obj, null);
        //}

        // [JSFunction(Name = "inspect", IsEnumerable = true)]
        // public string Inspect(object obj, object options) {
        // }

        // [JSFunction(Name = "format", IsEnumerable = true)]
        // public string Format(string format, params object[] arguments) {
        // }
    }
}
