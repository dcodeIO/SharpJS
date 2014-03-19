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

namespace SharpJS.Libraries
{
    public class Assert : /* Constructor-less */ Module
    {
        public readonly AssertionErrorConstructor AssertionError;

        public Assert(Context context) : base(context) {
            AssertionError = new AssertionErrorConstructor(context);
            Populate();
            SetPropertyValue("AssertionArray", AssertionError, true);
        }

        private static string truncate(string s, int n) {
            if (s == null)
                return null;
            return s.Length < n ? s : s.Substring(0, n);
        }

        public static ErrorInstance MakeError(Assert self, object actual, object expected, object message, object operatr) {
            bool generated = false;
            if (message == null || TypeComparer.Equals(message, Undefined.Value)) {
                message = truncate(JSONObject.Stringify(self.Engine, actual), 128)
                        + ' '
                        + operatr
                        + ' '
                        + truncate(JSONObject.Stringify(self.Engine, expected), 128);
                generated = true;
            }
            ErrorInstance err = self.Engine.Error.Construct(message as string);
            err.SetPropertyValue("message", message, false);
            err.SetPropertyValue("name", "AssertionError", false);
            err.SetPropertyValue("actual", actual, false);
            err.SetPropertyValue("expected", expected, false);
            err.SetPropertyValue("operator", operatr, false);
            err.SetPropertyValue("generatedMessage", generated, false);
            return err;
        }

        [JSFunction(Name = "fail", IsEnumerable = true)]
        public void Fail(object actual, object expected, object message, object operatr) {
            throw new JavaScriptException(MakeError(this, actual, expected, message, operatr), -1, "Assert.cs");
        }

        [JSFunction(Name = "ok", IsEnumerable = true)]
        public void Ok(object value, object message) {
            if (TypeComparer.Equals(value, false))
                Fail(value, true, message, "==");
        }

        [JSFunction(Name = "ifError", IsEnumerable = true)]
        public void IfError(object value, object message) {
            if (TypeComparer.Equals(value, true))
                throw new JavaScriptException(value, 49, "Assert.cs");
        }

        [JSFunction(Name = "equal", IsEnumerable = true)]
        public void Equal(object actual, object expected, object message) {
            if (!TypeComparer.Equals(actual, expected))
                Fail(actual, expected, message, "==");
        }

        [JSFunction(Name = "notEqual", IsEnumerable = true)]
        public void NotEqual(object actual, object expected, object message) {
            if (TypeComparer.Equals(actual, expected))
                Fail(actual, expected, message, "!=");
        }

        [JSFunction(Name = "strictEqual", IsEnumerable = true)]
        public void StrictEqual(object actual, object expected, object message) {
            if (!TypeComparer.StrictEquals(actual, expected))
                Fail(actual, expected, message, "===");
        }

        [JSFunction(Name = "notStrictEqual", IsEnumerable = true)]
        public void NotStrictEqual(object actual, object expected, object message) {
            if (TypeComparer.StrictEquals(actual, expected))
                Fail(actual, expected, message, "!==");
        }

        private bool _ExpectedException(object actual, object expected) {
            if (actual == null || expected == null)
                return false;
            if (actual is RegExpInstance)
                return (actual as RegExpInstance).Test(actual as string);
            if (actual is FunctionInstance && (actual as FunctionInstance).HasInstance(expected)) {
                return true;
            }
            if (expected is FunctionInstance && (expected as FunctionInstance).Call(Engine.Object.Construct(), actual) == (object)true)
                return true;
            return false;
        }

        private void _Throws(bool shouldThrow, FunctionInstance block, object expected, object message) {
            if (expected is string) {
                message = expected;
                expected = null;
            }
            JavaScriptException actual = null;
            try {
                block.Call(null);
            } catch (JavaScriptException e) {
                actual = e;
            }
            message = (expected != null && expected is FunctionInstance ? " (" + (expected as FunctionInstance).Name + ")." : ".") + (message != null ? " " + message : ".");
            if (shouldThrow && actual == null)
                Fail(actual, expected, "Missing expected exception" + message, null);
            if (!shouldThrow && _ExpectedException(actual, expected))
                Fail(actual, expected, "Got unwanted exception" + message, null);
            if ((shouldThrow && actual != null && expected != null && !_ExpectedException(actual, expected)) || (!shouldThrow && actual != null))
                throw new JavaScriptException(actual, -1, "Assert.cs");
        }

        [JSFunction(Name = "throws", IsEnumerable = true)]
        public void Throws(FunctionInstance block, object expected, object message) {
            _Throws(true, block, expected, message);
        }

        [JSFunction(Name = "doesNotThrow", IsEnumerable = true)]
        public void DowsNotThrow(FunctionInstance block, object message) {
            _Throws(false, block, null, message);
        }

        public class AssertionErrorConstructor : ClrFunction
        {
            private Context Context;
            public AssertionErrorConstructor(Context context) : base(context.Engine.Function.InstancePrototype, "AssertionError", context.Engine.Error.InstancePrototype) {
                Context = context;
            }

            [JSConstructorFunction]
            public ErrorInstance Construct(object options_) {
                ObjectInstance options = (options_ as ObjectInstance);
                if (options == null)
                    options = Context.Engine.Object.Construct();
                return MakeError(Context.GetModule("assert") as Assert, options.GetPropertyValue("actual"), options.GetPropertyValue("expected"), options.GetPropertyValue("message"), options.GetPropertyValue("operator"));
            }
        }
    }
}
