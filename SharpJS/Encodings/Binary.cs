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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpJS.Encodings
{
    class Binary : System.Text.Encoding
    {
        public Binary() : base() {
        }

        // Decoding

        public override int GetMaxCharCount(int byteCount) {
            return byteCount;
        }

        public override int GetCharCount(byte[] bytes, int index, int count) {
            return count;
        }

        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex) {
            int start = charIndex,
                max = byteIndex + byteCount;
            for (int i = byteIndex; i < max; ++i) {
                chars[charIndex++] = (char)bytes[i];
            }
            return charIndex - start;
        }

        // Encoding

        public override int GetMaxByteCount(int charCount) {
            return charCount;
        }

        public override int GetByteCount(char[] chars, int index, int count) {
            return count;
        }

        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex) {
            int start = byteIndex,
                max = charIndex + charCount;
            for (int i = charIndex; i < max; ++i) {
                try {
                    bytes[byteIndex++] = checked((byte)chars[i]);
                } catch (Exception) {
                    throw new EncoderFallbackException("Illegal value: "+((int)chars[i]));
                }
            }
            return byteIndex - start;
        }
    }
}
