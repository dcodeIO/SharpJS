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

namespace SharpJS.Libraries
{
    public class StringDecoder : Module
    {
        public readonly StringDecoderConstructor Constructor;

        public StringDecoder(Context context) : base(context) {
            Constructor = new StringDecoderConstructor(context);
            Populate();
            SetPropertyValue("StringDecoder", Constructor, true);
        }

        public class StringDecoderConstructor : ConstructorFunction
        {
            public StringDecoderConstructor(Context context) : base(context, "StringDecoder", new StringDecoderInstance(context.Engine.Object.InstancePrototype, false)) {
                Populate();
            }

            [JSConstructorFunction]
            public StringDecoderInstance Construct([DefaultParameterValue("utf8")] string encoding = "utf8") {
                return new StringDecoderInstance(Context, this.InstancePrototype, encoding);
            }
        }

        public class StringDecoderInstance : ContextObjectInstance
        {
            public System.Text.Encoding Encoding;
            private System.Text.Decoder Decoder;

            // Prototype constructor
            public StringDecoderInstance(ObjectInstance nextPrototype, bool extended) : base(nextPrototype, true) {
                if (!extended)
                    Populate();
            }

            // Instance constructor
            public StringDecoderInstance(Context context, ObjectInstance thisPrototype, string encoding) : base(context, thisPrototype) {
                Encoding = Context.GetEncoding(encoding);
                if (Encoding == null)
                    throw new JavaScriptException(Engine, "TypeError", "Unknown encoding: " + encoding);
                Decoder = Encoding.GetDecoder();
            }

            [JSFunction(Name = "write", IsEnumerable = true)]
            public string Write(BufferInstance buffer) {
                try {
                    int charCount = Decoder.GetCharCount(buffer.Buffer, buffer.Offset, buffer.Limit - buffer.Offset, false);
                    char[] chars = new char[charCount];
                    int n = Decoder.GetChars(buffer.Buffer, buffer.Offset, buffer.Limit - buffer.Offset, chars, 0, false);
                    return new string(chars, 0, n);
                } catch (Exception ex) {
                    throw new JavaScriptException(Engine, "Error", ex.Message);
                }
            }

            private static byte[] emptyByteArray = new byte[0];

            [JSFunction(Name = "end", IsEnumerable = true)]
            public string End([DefaultParameterValue(null)] BufferInstance buffer = null) {
                try {
                    char[] chars;
                    int charCount, n;
                    if (buffer != null) {
                        charCount = Decoder.GetCharCount(buffer.Buffer, buffer.Offset, buffer.Limit - buffer.Offset, true);
                        chars = new char[charCount];
                        n = Decoder.GetChars(buffer.Buffer, buffer.Offset, buffer.Limit - buffer.Offset, chars, 0, true);
                    } else {
                        charCount = Decoder.GetCharCount(emptyByteArray, 0, 0, true);
                        chars = new char[charCount];
                        n = Decoder.GetChars(emptyByteArray, 0, 0, chars, 0, true);
                    }
                    return new string(chars, 0, n);
                } catch (Exception ex) {
                    throw new JavaScriptException(Engine, "Error", ex.Message);
                }
            }
        }
    }
}
