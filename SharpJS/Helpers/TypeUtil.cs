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

namespace SharpJS.Helpers
{
    public static class TypeUtil
    {
        public static bool IsUndefined(object val) {
            return val == null || val == Undefined.Value;
        }

        public static bool IsNull(object val) {
            return val == Null.Value;
        }

        public static bool IsBoolean(object val) {
            return val is bool;
        }

        public static bool ToBoolean(object val) {
            return TypeConverter.ToBoolean(val);
        }

        public static bool IsInteger(object val) {
            return val is int || val is double;
        }

        public static int ToInteger(object val) {
            return TypeConverter.ToInteger(val);
        }

        public static bool IsNumber(object val) {
            return IsInteger(val) || val is double;
        }

        public static double ToNumber(object val) {
            return TypeConverter.ToNumber(val);
        }

        public static bool IsString(object val) {
            return val is string || val is ConcatenatedString;
        }

        public static string ToString(object val) {
            return TypeConverter.ToString(val);
        }

        public static bool IsObject(object val) {
            return val != null && (val == Null.Value || val is ObjectInstance);
        }

        public static bool IsNonNullObject(object val) {
            return val != null && val is ObjectInstance;
        }

        public static bool IsArray(object val) {
            return val is ArrayInstance;
        }

        public static ArrayInstance ToArray(object val) {
            return val as ArrayInstance;
        }

        public static bool IsError(object val) {
            return val is ErrorInstance;
        }

        public static string TypeOf(object val) {
            return TypeUtilities.TypeOf(val);
        }
    }
}
