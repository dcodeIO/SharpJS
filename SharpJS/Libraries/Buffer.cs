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
using System.Text;

namespace SharpJS.Libraries
{
    // TODO: Think about a proper CLR-way to optimize memory allocation.

    public class Buffer : Module
    {
        public readonly BufferConstructor Constructor;
        private int InspectMaxBytes = 50;

        public Buffer(Context context) : base(context) {
            Constructor = new BufferConstructor(context);
            SetPropertyValue("Buffer", Constructor, true);
            SetPropertyValue("SlowBuffer", Constructor, true);
        }

        public override Module RegisterGlobals() {
            Engine.SetGlobalValue("Buffer", Constructor);
            return this;
        }

        [JSProperty(Name = "INSPECT_MAX_BYTES", IsEnumerable = true)]
        public int INSPECT_MAX_BYTES {
            get {
                return InspectMaxBytes;
            }
            set {
                InspectMaxBytes = value;
            }
        }
    }

    public class BufferConstructor : ConstructorFunction
    {

        public BufferConstructor(Context context) : base(context, "Buffer", new BufferInstance(context.Engine.Object.InstancePrototype, false)) {
            Populate();
        }

        [JSConstructorFunction]
        public BufferInstance Construct(object subject, [DefaultParameterValue(null)] object encoding = null) {
            if (subject is byte[]) {
                byte[] b = subject as byte[];
                return new BufferInstance(Context, this.InstancePrototype, b, 0, b.Length);
            } else if (TypeUtil.IsNumber(subject)) {
                int size = TypeUtil.ToInteger(subject);
                if (size < 0)
                    throw new JavaScriptException(Engine, "RangeError", "size < 0");
                return new BufferInstance(Context, this.InstancePrototype, new byte[size], 0, size);
            } else if (TypeUtil.IsArray(subject)) {
                ArrayInstance array = TypeUtil.ToArray(subject);
                byte[] b = new byte[array.Length];
                int i = 0;
                foreach (object v in array.ElementValues) {
                    try {
                        b[i++] = unchecked((byte)TypeConverter.ToUint32(v));
                    } catch (Exception ex) {
                        throw new JavaScriptException(Engine, "TypeError", ex.Message);
                    }
                }
                return new BufferInstance(Context, this.InstancePrototype, b, 0, b.Length);
            } else if (TypeUtil.IsString(subject)) {
                string str = TypeUtil.ToString(subject);
                Encoding enc = Context.GetEncoding(encoding as string);
                if (enc == null)
                    throw new JavaScriptException(Engine, "TypeError", "Unknown encoding: " + encoding);
                try {
                    byte[] b = enc.GetBytes(str);
                    return new BufferInstance(Context, this.InstancePrototype, b, 0, b.Length);
                } catch (Exception ex) {
                    throw new JavaScriptException(Engine, "TypeError", ex.Message);
                }
            } else {
                throw new JavaScriptException(Engine, "TypeError", "subject is not a number, array or string");
            }

        }

        [JSFunction(Name = "isBuffer", IsEnumerable = true)]
        public bool IsBuffer(object obj) {
            return obj != null && obj is BufferInstance;
        }

        [JSFunction(Name = "isEncoding", IsEnumerable = true)]
        public bool IsEncoding(object encoding) {
            return Context.GetEncoding(encoding as string) != null;
        }

        [JSFunction(Name = "byteLength", IsEnumerable = true)]
        public int ByteLength(string str) {
            return ByteLength(str, "utf8");
        }

        [JSFunction(Name = "byteLength", IsEnumerable = true)]
        public int ByteLength(string str, object encoding) {
            if (encoding == Undefined.Value || encoding == null || encoding == Null.Value)
                encoding = "utf8";
            Encoding enc = Context.GetEncoding(encoding as string);
            if (enc == null)
                throw new JavaScriptException(Engine, "TypeError", "Unknown encoding: " + enc);
            return enc.GetByteCount(str);
        }

    }

    public class BufferInstance : ContextObjectInstance
    {
        internal byte[] Buffer;
        internal int Offset;
        internal int Limit;

        // Prototype constructor
        public BufferInstance(ObjectInstance nextPrototype, bool extended) : base(nextPrototype, true) {
            if (!extended)
                Populate();
        }

        // Instance constructor
        public BufferInstance(Context context, ObjectInstance thisPrototype, byte[] buffer, int offset, int limit) : base(context, thisPrototype) {
            Buffer = buffer;
            Offset = offset;
            Limit = limit;
        }

// Array access

        protected override object GetMissingPropertyValue(string propertyName) {
            try {
                return (uint)Buffer[Offset + Convert.ToUInt32(propertyName)];
            } catch (Exception ex) {
                return null;
            }
        }

        public override void SetPropertyValue(uint index, object value, bool throwOnError) {
            try {
                Buffer[Offset + index] = unchecked((byte)TypeConverter.ToInt32(value));
            } catch (Exception) {
            }
        }

        [JSProperty(Name = "length", IsEnumerable = false)]
        public int Length {
            get {
                return Limit - Offset;
            }
        }

        // NOTE: This does not implement noAssert behaviour. It's cumbersome, has a huge overhad and has no real value.
        // Instead values will always be converted unchecked to the target type and an exception will be thrown when
        // the offset is out of range.

// Unsigned

        [JSFunction(Name = "readUInt8", IsEnumerable = true)]
        public uint ReadUInt8(int offset) {
            int o = offset + Offset;
            if (o < Offset || o + 1 > Limit)
                throw new JavaScriptException(Engine, "RangeError", "index out of range");
            return Buffer[o];
        }

        [JSFunction(Name = "writeUInt8", IsEnumerable = true)]
        public int WriteUInt8(object value, int offset) {
            int o = offset + Offset;
            if (o < Offset || o + 1 > Limit)
                throw new JavaScriptException(Engine, "RangeError", "index out of range");
            try {
                uint v = TypeConverter.ToUint32(value);
                unchecked {
                    Buffer[o] = (byte)v;
                }
                return o + 1;
            } catch (ArgumentException) {
                throw new JavaScriptException(Engine, "TypeError", "illegal value");
            } catch (OverflowException) {
                throw new JavaScriptException(Engine, "RangeError", "index out of range");
            }
        }

        [JSFunction(Name = "readUInt16LE", IsEnumerable = true)]
        public uint ReadUInt16LE(int offset) {
            int o = offset + Offset;
            if (o < Offset || o + 2 > Limit)
                throw new JavaScriptException(Engine, "RangeError", "index out of range");
            return unchecked((ushort)((Buffer[o + 1] << 8) | Buffer[o]));
        }

        [JSFunction(Name = "writeUInt16LE", IsEnumerable = true)]
        public int WriteUInt16LE(object value, int offset) {
            int o = offset + Offset;
            if (o < Offset || o + 2 > Limit)
                throw new JavaScriptException(Engine, "RangeError", "index out of range");
            try {
                uint v = TypeConverter.ToUint32(value);
                unchecked {
                    Buffer[o + 1] = (byte)((v >> 8) & 0xFF);
                    Buffer[o    ] = (byte)( v       & 0xFF);
                }
                return o + 2;
            } catch (ArgumentException) {
                throw new JavaScriptException(Engine, "TypeError", "illegal value");
            }
        }

        [JSFunction(Name = "readUInt16BE", IsEnumerable = true)]
        public uint ReadUInt16BE(int offset) {
            int o = offset + Offset;
            if (o < Offset || o + 2 > Limit)
                throw new JavaScriptException(Engine, "RangeError", "index out of range");
            return unchecked((uint)(
                (Buffer[o    ] << 8) |
                 Buffer[o + 1]
            ));
        }

        [JSFunction(Name = "writeUInt16BE", IsEnumerable = true)]
        public int WriteUInt16BE(object value, int offset) {
            int o = offset + Offset;
            if (o < Offset || o + 2 > Limit)
                throw new JavaScriptException(Engine, "RangeError", "index out of range");
            try {
                uint v = TypeConverter.ToUint32(value);
                unchecked {
                    Buffer[o    ] = (byte)((v >> 8) & 0xFF);
                    Buffer[o + 1] = (byte)( v       & 0xFF);
                }
                return o + 2;
            } catch (ArgumentException) {
                throw new JavaScriptException(Engine, "TypeError", "illegal value");
            }
        }

        [JSFunction(Name = "readUInt32LE", IsEnumerable = true)]
        public uint ReadUInt32LE(int offset) {
            int o = offset + Offset;
            if (o < Offset || o + 2 > Limit)
                throw new JavaScriptException(Engine, "RangeError", "index out of range");
            return unchecked((uint)(
                (Buffer[o + 3] << 24) |
                (Buffer[o + 2] << 16) |
                (Buffer[o + 1] <<  8) |
                 Buffer[o]
            ));
        }

        [JSFunction(Name = "writeUInt32LE", IsEnumerable = true)]
        public int WriteUInt32LE(object value, int offset) {
            int o = offset + Offset;
            if (o < Offset || o + 4 > Limit)
                throw new JavaScriptException(Engine, "RangeError", "index out of range");
            try {
                uint v = TypeConverter.ToUint32(value);
                unchecked {
                    Buffer[o + 3] = (byte)((v >> 24) & 0xFF);
                    Buffer[o + 2] = (byte)((v >> 16) & 0xFF);
                    Buffer[o + 1] = (byte)((v >>  8) & 0xFF);
                    Buffer[o    ] = (byte)( v        & 0xFF);
                }
                return o + 4;
            } catch (ArgumentException) {
                throw new JavaScriptException(Engine, "TypeError", "illegal value");
            }
        }

        [JSFunction(Name = "readUInt32BE", IsEnumerable = true)]
        public uint ReadUInt32BE(int offset) {
            int o = offset + Offset;
            if (o < Offset || o + 4 > Limit)
                throw new JavaScriptException(Engine, "RangeError", "index out of range");
            return unchecked((uint)(
                (Buffer[o    ] << 24) |
                (Buffer[o + 1] << 16) |
                (Buffer[o + 2] <<  8) |
                 Buffer[o + 3]
            ));
        }

        [JSFunction(Name = "writeUInt32BE", IsEnumerable = true)]
        public int WriteUInt32BE(object value, int offset) {
            int o = offset + Offset;
            if (o < Offset || o + 4 > Limit)
                throw new JavaScriptException(Engine, "RangeError", "index out of range");
            try {
                uint v = TypeConverter.ToUint32(value);
                unchecked {
                    Buffer[o    ] = (byte)((v >> 24) & 0xFF);
                    Buffer[o + 1] = (byte)((v >> 16) & 0xFF);
                    Buffer[o + 2] = (byte)((v >>  8) & 0xFF);
                    Buffer[o + 3] = (byte)( v        & 0xFF);
                }
                return o + 4;
            } catch (ArgumentException) {
                throw new JavaScriptException(Engine, "TypeError", "illegal value");
            }
        }

// Signed

        [JSFunction(Name = "readInt8", IsEnumerable = true)]
        public int ReadInt8(int offset) {
            return unchecked((sbyte)ReadUInt8(offset));
        }

        [JSFunction(Name = "writeInt8", IsEnumerable = true)]
        public int WriteInt8(object value, int offset) {
            return WriteUInt8(value, offset);
        }

        [JSFunction(Name = "readInt16LE", IsEnumerable = true)]
        public int ReadInt16LE(int offset) {
            return unchecked((short)ReadUInt16LE(offset));
        }

        [JSFunction(Name = "writeInt16LE", IsEnumerable = true)]
        public int WriteInt16LE(object value, int offset) {
            return WriteUInt16LE(value, offset);
        }

        [JSFunction(Name = "readInt16BE", IsEnumerable = true)]
        public int ReadInt16BE(int offset) {
            return unchecked((short)ReadUInt16BE(offset));
        }

        [JSFunction(Name = "writeInt16BE", IsEnumerable = true)]
        public int WriteInt16BE(object value, int offset) {
            return WriteUInt16BE(value, offset);
        }

        [JSFunction(Name = "readInt32LE", IsEnumerable = true)]
        public int ReadInt32LE(int offset) {
            return unchecked((int)ReadUInt32LE(offset));
        }

        [JSFunction(Name = "writeInt32LE", IsEnumerable = true)]
        public int WriteInt32LE(object value, int offset) {
            return WriteUInt32LE(value, offset);
        }

        [JSFunction(Name = "readInt32BE", IsEnumerable = true)]
        public int ReadInt32BE(int offset) {
            return unchecked((int)ReadUInt32BE(offset));
        }

        [JSFunction(Name = "writeInt32BE", IsEnumerable = true)]
        public int WriteInt32BE(object value, int offset) {
            return WriteUInt32BE(value, offset);
        }

// Floats

        private byte[] FourBytes = new byte[4];
        private byte[] EightBytes = new byte[8];

        [JSFunction(Name = "readFloatLE", IsEnumerable = true)]
        public float ReadFloatLE(int offset) {
            int o = offset + Offset;
            if (o < Offset || o + 4 > Limit)
                throw new JavaScriptException(Engine, "RangeError", "index out of range");
            if (BitConverter.IsLittleEndian)
                return BitConverter.ToSingle(Buffer, o);
            else {
                Buffer.CopyTo(FourBytes, o);
                Array.Reverse(FourBytes);
                return BitConverter.ToSingle(FourBytes, o);
            }
        }

        [JSFunction(Name = "writeFloatLE", IsEnumerable = true)]
        public int WriteFloatLE(object value, int offset) {
            int o = offset + Offset;
            if (o < Offset || o + 4 > Limit)
                throw new JavaScriptException(Engine, "RangeError", "index out of range");
            try {
                float v = Convert.ToSingle(TypeConverter.ToNumber(value));
                if (BitConverter.IsLittleEndian) {
                    BitConverter.GetBytes(v).CopyTo(Buffer, o);
                } else {
                    byte[] vb = BitConverter.GetBytes(v);
                    Array.Reverse(vb);
                    vb.CopyTo(Buffer, o);
                }
                return o + 4;
            } catch (ArgumentException) {
                throw new JavaScriptException(Engine, "TypeError", "illegal value");
            }
        }

        [JSFunction(Name = "readFloatBE", IsEnumerable = true)]
        public float ReadFloatBE(int offset) {
            int o = offset + Offset;
            if (o < Offset || o + 4 > Limit)
                throw new JavaScriptException(Engine, "RangeError", "index out of range");
            if (!BitConverter.IsLittleEndian)
                return BitConverter.ToSingle(Buffer, o);
            else {
                Buffer.CopyTo(FourBytes, o);
                Array.Reverse(FourBytes);
                return BitConverter.ToSingle(FourBytes, o);
            }
        }

        [JSFunction(Name = "writeFloatBE", IsEnumerable = true)]
        public int WriteFloatBE(object value, int offset) {
            int o = offset + Offset;
            if (o < Offset || o + 4 > Limit)
                throw new JavaScriptException(Engine, "RangeError", "index out of range");
            try {
                float v = Convert.ToSingle(TypeConverter.ToNumber(value));
                if (!BitConverter.IsLittleEndian) {
                    BitConverter.GetBytes(v).CopyTo(Buffer, o);
                } else {
                    byte[] vb = BitConverter.GetBytes(v);
                    Array.Reverse(vb);
                    vb.CopyTo(Buffer, o);
                }
                return o + 4;
            } catch (ArgumentException) {
                throw new JavaScriptException(Engine, "TypeError", "illegal value");
            }
        }

// Doubles

        [JSFunction(Name = "readDoubleLE", IsEnumerable = true)]
        public double ReadDoubleLE(int offset) {
            int o = offset + Offset;
            if (o < Offset || o + 8 > Limit)
                throw new JavaScriptException(Engine, "RangeError", "index out of range");
            if (BitConverter.IsLittleEndian)
                return BitConverter.ToDouble(Buffer, o);
            else {
                Buffer.CopyTo(EightBytes, o);
                Array.Reverse(EightBytes);
                return BitConverter.ToDouble(EightBytes, o);
            }
        }

        [JSFunction(Name = "writeDoubleLE", IsEnumerable = true)]
        public int WriteDoubleLE(object value, int offset) {
            int o = offset + Offset;
            if (o < Offset || o + 8 > Limit)
                throw new JavaScriptException(Engine, "RangeError", "index out of range");
            try {
                double v = TypeConverter.ToNumber(value);
                if (BitConverter.IsLittleEndian) {
                    BitConverter.GetBytes(v).CopyTo(Buffer, o);
                } else {
                    byte[] vb = BitConverter.GetBytes(v);
                    Array.Reverse(vb);
                    vb.CopyTo(Buffer, o);
                }
                return o + 8;
            } catch (ArgumentException) {
                throw new JavaScriptException(Engine, "TypeError", "illegal value");
            }
        }

        [JSFunction(Name = "readDoubleBE", IsEnumerable = true)]
        public double ReadDoubleBE(int offset) {
            int o = offset + Offset;
            if (o < Offset || o + 8 > Limit)
                throw new JavaScriptException(Engine, "RangeError", "index out of range");
            if (!BitConverter.IsLittleEndian)
                return BitConverter.ToDouble(Buffer, o);
            else {
                Buffer.CopyTo(EightBytes, o);
                Array.Reverse(EightBytes);
                return BitConverter.ToDouble(EightBytes, o);
            }
        }

        [JSFunction(Name = "writeDoubleBE", IsEnumerable = true)]
        public int WriteDoubleBE(object value, int offset) {
            int o = offset + Offset;
            if (o < Offset || o + 8 > Limit)
                throw new JavaScriptException(Engine, "RangeError", "index out of range");
            try {
                double v = TypeConverter.ToNumber(value);
                if (!BitConverter.IsLittleEndian) {
                    BitConverter.GetBytes(v).CopyTo(Buffer, o);
                } else {
                    byte[] vb = BitConverter.GetBytes(v);
                    Array.Reverse(vb);
                    vb.CopyTo(Buffer, o);
                }
                return o + 8;
            } catch (ArgumentException) {
                throw new JavaScriptException(Engine, "TypeError", "illegal value");
            }
        }

// Strings

        [JSFunction(Name = "write", IsEnumerable = true)]
        public int Write(string str, object encoding) {
            return Write(str, 0, "utf8");
        }

        [JSFunction(Name = "write", IsEnumerable = true)]
        public int Write(string str, int offset, object encoding) {
            return Write(str, 0, Limit - Offset, encoding);
        }

        [JSFunction(Name = "write", IsEnumerable = true)]
        public int Write(string str, int offset, int length, object encoding) {
            if (encoding == Undefined.Value || encoding == null || encoding == Null.Value) {
                encoding = "utf8";
            }
            Encoding enc = Context.GetEncoding(encoding as string);
            if (enc == null)
                throw new JavaScriptException(Engine, "TypeError", "Unknown encoding: "+encoding);
            byte[] b = enc.GetBytes(str);
            int o = offset + Offset;
            int e = offset + Offset + length;
            if (o < Offset || e > Limit || e < o)
                throw new JavaScriptException(Engine, "RangeError", "index out of range");
            for (int i = o, j = 0; i < e; ++i, ++j) {
                Buffer[i] = b[j];
            }
            return offset + length;
        }

        [JSFunction(Name = "toString", IsEnumerable = false)]
        public string toString([DefaultParameterValue("utf8")] string encoding = "utf8", [DefaultParameterValue(0)] int start = 0) {
            return toString(encoding, start, Limit - Offset);
        }

        [JSFunction(Name = "toString", IsEnumerable = false)]
        public string toString(string encoding, int start, int end) {
            if (start == end)
                return "";
            int s = start + Offset;
            int e = end + Offset;
            if (s < Offset || e > Limit || e < s)
                throw new JavaScriptException(Engine, "RangeError", "index out of range");
            Encoding enc = Context.GetEncoding(encoding);
            if (enc == null)
                throw new JavaScriptException(Engine, "TypeError", "Unknown encoding: " + encoding);
            try {
                return enc.GetString(Buffer, s, e - s);
            } catch (Exception ex) {
                throw new JavaScriptException(Engine, "TypeError", ex.Message);
            }
        }

        [JSFunction(Name = "inspect", IsEnumerable = true)]
        public string Inspect() {
            StringBuilder sb = new StringBuilder();
            sb.Append("Buffer");
            for (var i = Offset; i < Math.Min(Limit, Offset + Context.Buffer.INSPECT_MAX_BYTES); ++i) {
                sb.Append(" " + Buffer[i].ToString("X"));
            }
            return sb.ToString();
        }

        // Methods

        [JSFunction(Name = "slice", IsEnumerable = true)]
        public BufferInstance Slice() {
            return new BufferInstance(Context, Context.Buffer.Constructor.InstancePrototype, Buffer, Offset, Limit);
        }

        [JSFunction(Name = "slice", IsEnumerable = true)]
        public BufferInstance Slice(int start) {
            if (start < Offset || start > Limit)
                throw new JavaScriptException(Engine, "RangeError", "index out of range");
            BufferInstance buffer = Slice();
            buffer.Offset = start;
            return buffer;
        }

        [JSFunction(Name = "slice", IsEnumerable = true)]
        public BufferInstance Slice(int start, int end) {
            if (end < start || end > Limit)
                throw new JavaScriptException(Engine, "RangeError", "index out of range");
            BufferInstance buffer = Slice(start);
            buffer.Limit = end;
            return buffer;
        }

        [JSFunction(Name = "copy", IsEnumerable = true)]
        public void Copy(BufferInstance targetBuffer) {
            Copy(targetBuffer, 0);
        }

        [JSFunction(Name = "copy", IsEnumerable = true)]
        public void Copy(BufferInstance targetBuffer, int targetStart) {
            Copy(targetBuffer, targetStart, 0);
        }

        [JSFunction(Name = "copy", IsEnumerable = true)]
        public void Copy(BufferInstance targetBuffer, int targetStart, int sourceStart) {
            Copy(targetBuffer, targetStart, sourceStart, Limit - Offset);
        }

        [JSFunction(Name = "copy", IsEnumerable = true)]
        public void Copy(BufferInstance targetBuffer, int targetStart, int sourceStart, int sourceEnd) {
            int len = sourceEnd - sourceStart;
            if (targetStart < targetBuffer.Offset || targetStart + len > targetBuffer.Limit)
                throw new JavaScriptException(Engine, "RangeError", "target index out of range");
            if (sourceStart < Offset || sourceEnd > Limit || sourceEnd < sourceStart)
                throw new JavaScriptException(Engine, "RangeError", "source index out of range");
            for (int i = sourceStart, j = targetStart; i < sourceEnd; ++i, ++j) {
                targetBuffer.Buffer[j] = Buffer[i];
            }
        }

        [JSFunction(Name = "fill", IsEnumerable = true)]
        public void Fill(object value) {
            Fill(value, 0, Limit - Offset);
        }

        [JSFunction(Name = "fill", IsEnumerable = true)]
        public void Fill(object value, int offset) {
            Fill(value, offset, Limit - Offset);
        }

        [JSFunction(Name = "fill", IsEnumerable = true)]
        public /* is this */ void /* ? */ Fill(object value, int offset, int end) {
            int o = offset + Offset;
            int e = end + Offset;
            if (o < Offset || e > Limit || e < o)
                throw new JavaScriptException(Engine, "RangeError", "index out of range");
            byte val;
            if (value is string) {
                if ((value as string).Length < 1)
                    throw new JavaScriptException(Engine, "TypeError", "empty string");
                val = BitConverter.GetBytes((value as string)[0])[0];
            } else if (value is int) {
                val = unchecked(BitConverter.GetBytes((sbyte)value)[0]);
            } else if (value is uint) {
                val = unchecked(BitConverter.GetBytes((byte)value)[0]);
            } else
                throw new JavaScriptException(Engine, "TypeError", "illegal value");
            for (var i = offset; i < end; i++) {
                Buffer[i] = val;
            }
        }
    }

}
