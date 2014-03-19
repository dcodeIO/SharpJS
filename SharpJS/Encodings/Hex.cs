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
    public class Hex : System.Text.Encoding
    {
        private static char[] Chars = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f' };

        private int CharValue(char c) {
            if (c >= '0' && c <= '9')
                return c - '0';
            if (c >= 'a' && c <= 'f')
                return 10 + c - 'a';
            if (c >= 'A' && c <= 'F')
                return 10 + c - 'A';
            throw new DecoderFallbackException("Illegal character: "+c);
        }

        public Hex() : base() {
        }

        // Decoding

        public override int GetMaxCharCount(int byteCount) {
            return byteCount * 2;
        }

        public override int GetCharCount(byte[] bytes, int index, int count) {
            return count * 2;
        }

        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex) {
            int start = charIndex,
                max = byteIndex + byteCount;
            for (int i = byteIndex; i < max; ++i) {
                chars[charIndex++] = Chars[(bytes[i] >> 4) & 0x0F];
                chars[charIndex++] = Chars[bytes[i] & 0x0F];
            }
            return charIndex - start;
        }

        // Encoding

        public override int GetMaxByteCount(int charCount) {
            return (int)Math.Ceiling(charCount / 2d);
        }

        public override int GetByteCount(char[] chars, int index, int count) {
            return count / 2;
        }

        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex) {
            int start = byteIndex,
                max = charIndex + charCount - 1;
            for (int i = charIndex; i < max; i += 2) {
                bytes[byteIndex++] = (byte)(((CharValue(chars[i]) & 0x0F) << 4) & (CharValue(chars[i + 1]) & 0x0F));
            }
            return byteIndex - start;
        }
    }
}
