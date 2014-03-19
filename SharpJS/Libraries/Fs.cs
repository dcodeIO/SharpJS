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
using SharpJS.Encodings;
using SharpJS.Helpers;
using System;
using System.IO;
using System.Text;
using System.Threading;

namespace SharpJS.Libraries
{
    public class Fs : /* Constructor-less */ Module
    {
        public bool AllowSync = true;
        public string Chroot = null;

        public Fs(Context context, bool allowSync = true, string chroot = null) : base(context) {
            AllowSync = allowSync;
            Chroot = chroot;
            Populate();
        }

        public string CheckPath(string path, bool allowRoot = false) {
            if (path == null)
                throw new ArgumentNullException("path is null");
            path = Context.Path.Resolve(path);
            if (Chroot != null) {
                if (path.Equals(Chroot)) {
                    if (!allowRoot)
                        throw new AccessViolationException("Unauthorized access to " + path);
                } else if (!path.StartsWith(Chroot + Path.Sep, Path.IsWindows ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                    throw new AccessViolationException("Unauthorized access to " + path + ", restricted to " + Chroot + Path.Sep + "**");
            }
            return path;
        }

        [JSFunction(Name = "rename", IsEnumerable = true)]
        public void Rename(string oldPath, string newPath, [DefaultParameterValue(null)] FunctionInstance callback = null) {
            ThreadPool.QueueUserWorkItem(new WaitCallback(RenameAsync), new object[] { oldPath, newPath, callback });
        }

        private void RenameAsync(object arguments) {
            object[] args = arguments as object[];
            FunctionInstance callback = args[2] as FunctionInstance;
            try {
                string oldPath = CheckPath(args[0] as string);
                string newPath = CheckPath(args[1] as string);
                File.Move(oldPath, newPath);
                if (callback != null)
                    Context.NextTick(callback.Bind(this, null));
            } catch (Exception ex) {
                if (callback != null)
                    Context.NextTick(callback.Bind(this, Engine.Error.Construct(ex.Message)));
            }
        }

        [JSFunction(Name = "renameSync", IsEnumerable = true)]
        public void RenameSync(string oldPath, string newPath) {
            if (!AllowSync)
                throw new JavaScriptException(Engine, "Error", "synchronous operations are not permitted");
            try {
                File.Move(CheckPath(oldPath), CheckPath(newPath));
            } catch (Exception ex) {
                throw new JavaScriptException(Engine, "Error", ex.Message);
            }
        }

        [JSFunction(Name = "truncate", IsEnumerable = true)]
        public void Truncate(string path, [DefaultParameterValue(0)] int len = 0, [DefaultParameterValue(null)] FunctionInstance callback = null) {
            ThreadPool.QueueUserWorkItem(new WaitCallback(TruncateAsync), new object[] { path, len, callback });
        }

        private void TruncateAsync(object arguments) {
            object[] args = arguments as object[];
            FunctionInstance callback = args[2] as FunctionInstance;
            try {
                string path = args[0] as string;
                int len = (int)args[1];
                FileStream fs = new FileStream(CheckPath(path), FileMode.Truncate);
                try {
                    fs.SetLength(len);
                } finally {
                    fs.Close();
                }
                if (callback != null)
                    Context.NextTick(callback.Bind(this, null));
            } catch (Exception ex) {
                if (callback != null)
                    Context.NextTick(callback.Bind(this, Engine.Error.Construct(ex.Message)));
            }
        }

        [JSFunction(Name = "truncateSync", IsEnumerable = true)]
        public void TruncateSync(string path, [DefaultParameterValue(0)] int len = 0) {
            if (!AllowSync)
                throw new JavaScriptException(Engine, "Error", "synchronous operations are not permitted");
            try {
                FileStream fs = new FileStream(CheckPath(path), FileMode.Truncate);
                try {
                    fs.SetLength(len);
                } finally {
                    fs.Close();
                }
            } catch (Exception ex) {
                throw new JavaScriptException(Engine, "Error", ex.Message);
            }
        }

        [JSFunction(Name = "unlink", IsEnumerable = true)]
        public void Unlink(string path, [DefaultParameterValue(null)] FunctionInstance callback = null) {
            ThreadPool.QueueUserWorkItem(new WaitCallback(UnlinkAsync), new object[] { path, callback });
        }

        private void UnlinkAsync(object arguments) {
            object[] args = arguments as object[];
            FunctionInstance callback = args[1] as FunctionInstance;
            try {
                string path = args[0] as string;
                File.Delete(CheckPath(path));
                if (callback != null)
                    Context.NextTick(callback.Bind(this, null));
            } catch (Exception ex) {
                if (callback != null)
                    Context.NextTick(callback.Bind(this, Engine.Error.Construct(ex.Message)));
            }
        }
        
        [JSFunction(Name = "unlinkSync", IsEnumerable = true)]
        public void UnlinkSync(string path) {
            if (!AllowSync)
                throw new JavaScriptException(Engine, "Error", "synchronous operations are not permitted");
            try {
                File.Delete(CheckPath(path));
            } catch (Exception ex) {
                throw new JavaScriptException(Engine, "Error", ex.Message);
            }
        }

        [JSFunction(Name = "rmdir", IsEnumerable = true)]
        public void Rmdir(string path, [DefaultParameterValue(null)] FunctionInstance callback = null) {
            ThreadPool.QueueUserWorkItem(new WaitCallback(RmdirAsync), new object[] { path, callback });
        }

        private void RmdirAsync(object arguments) {
            object[] args = arguments as object[];
            FunctionInstance callback = args[1] as FunctionInstance;
            try {
                string path = args[0] as string;
                Directory.Delete(CheckPath(path));
                if (callback != null)
                    Context.NextTick(callback.Bind(this, null));
            } catch (Exception ex) {
                if (callback != null)
                    Context.NextTick(callback.Bind(this, Engine.Error.Construct(ex.Message)));
            }
        }

        [JSFunction(Name = "rmdirSync", IsEnumerable = true)]
        public void RmdirSync(string path) {
            if (!AllowSync)
                throw new JavaScriptException(Engine, "Error", "synchronous operations are not permitted");
            try {
                Directory.Delete(CheckPath(path));
            } catch (Exception ex) {
                throw new JavaScriptException(Engine, "Error", ex.Message);
            }
        }

        [JSFunction(Name = "mkdir", IsEnumerable = true)]
        public void Mkdir(string path, params object[] args) {
            ThreadPool.QueueUserWorkItem(new WaitCallback(MkdirAsync), new object[] { path, args });
        }

        private void MkdirAsync(object arguments) {
            object[] args = arguments as object[];
            object[] mkdirArgs = args[1] as object[];
            FunctionInstance callback = null;
            try {
                int mode = 0777;
                if (mkdirArgs.Length >= 2) {
                    mode = (int)mkdirArgs[0];
                    callback = mkdirArgs[1] as FunctionInstance;
                } else if (mkdirArgs.Length >= 1) {
                    if (mkdirArgs[0] is int) {
                        mode = (int)mkdirArgs[0];
                    } else if (mkdirArgs[0] is FunctionInstance) {
                        callback = mkdirArgs[0] as FunctionInstance;
                    }
                }
                string path = CheckPath(args[0] as string);
                Directory.CreateDirectory(path);
                if (callback != null)
                    Context.NextTick(callback.Bind(this, null));
            } catch (Exception ex) {
                if (callback != null)
                    Context.NextTick(callback.Bind(this, Engine.Error.Construct(ex.Message)));
            }
        }

        [JSFunction(Name = "mkdirSync", IsEnumerable = true)]
        public void MkdirSync(string path, [DefaultParameterValue(0777)] int mode = 0777) {
            if (!AllowSync)
                throw new JavaScriptException(Engine, "Error", "synchronous operations are not permitted");
            try {
                Directory.CreateDirectory(CheckPath(path));
            } catch (Exception ex) {
                throw new JavaScriptException(Engine, "Error", ex.Message);
            }
        }

        [JSFunction(Name = "readdir", IsEnumerable = true)]
        public void Readdir(string path, FunctionInstance callback) {
            ThreadPool.QueueUserWorkItem(new WaitCallback(ReaddirAsync), new object[] { path, callback });
        }

        private void ReaddirAsync(object arguments) {
            object[] args = arguments as object[];
            FunctionInstance callback = args[1] as FunctionInstance;
            try {
                string path = args[0] as string;
                string[] files = Directory.GetFileSystemEntries(CheckPath(path, true));
                ArrayInstance result = Engine.Array.Construct();
                foreach (string file in files) {
                    if (file != "." && file != "..")
                        result.Push(System.IO.Path.GetFileName(file));
                }
                if (callback != null)
                    Context.NextTick(callback.Bind(this, null, result));
            } catch (Exception ex) {
                if (callback != null)
                    Context.NextTick(callback.Bind(this, Engine.Error.Construct(ex.Message)));
            }
        }

        [JSFunction(Name = "readdirSync", IsEnumerable = true)]
        public ArrayInstance ReaddirSync(string path) {
            if (!AllowSync)
                throw new JavaScriptException(Engine, "Error", "synchronous operations are not permitted");
            try {
                string[] files = Directory.GetFileSystemEntries(CheckPath(path));
                ArrayInstance result = Engine.Array.Construct();
                foreach (string file in files) {
                    if (file != "." && file != "..")
                        result.Push(System.IO.Path.GetFileName(file));
                }
                return result;
            } catch (Exception ex) {
                throw new JavaScriptException(Engine, "Error", ex.Message);
            }
        }

        [JSFunction(Name = "readFile", IsEnumerable = true)]
        public void ReadFile(string path, params object[] args) {
            ThreadPool.QueueUserWorkItem(new WaitCallback(ReadFileAsync), new object[] { path, args });
        }

        private void ReadFileAsync(object arguments) {
            object[] args = arguments as object[];
            FunctionInstance callback = null;
            ObjectInstance options = null;
            try {
                object[] readArgs = args[1] as object[];
                if (readArgs.Length >= 2) {
                    options = readArgs[0] as ObjectInstance;
                    callback = readArgs[1] as FunctionInstance;
                } else if (readArgs.Length >= 1) {
                    if (readArgs[0] is FunctionInstance) {
                        callback = readArgs[0] as FunctionInstance;
                    }
                }
                string path = CheckPath(args[0] as string);
                string encoding = options != null ? options.GetPropertyValue("encoding") as string : null;
                byte[] b = File.ReadAllBytes(path);
                object result = new BufferInstance(Context, Context.Buffer.Constructor.InstancePrototype, b, 0, b.Length);
                if (encoding != null) {
                    result = (result as BufferInstance).toString(encoding);
                }
                if (callback != null)
                    Context.NextTick(callback.Bind(this, null, result));
            } catch (Exception ex) {
                if (callback != null)
                    Context.NextTick(callback.Bind(this, Engine.Error.Construct(ex.Message)));
            }
        }

        [JSFunction(Name = "readFileSync", IsEnumerable = true)]
        public object ReadFileSync(string path, [DefaultParameterValue(null)] ObjectInstance options = null) {
            if (!AllowSync)
                throw new JavaScriptException(Engine, "Error", "synchronous operations are not permitted");
            try {
                string encoding = options != null ? options.GetPropertyValue("encoding") as string : null;
                byte[] b = File.ReadAllBytes(CheckPath(path));
                object result = new BufferInstance(Context, Context.Buffer.Constructor.InstancePrototype, b, 0, b.Length);
                if (encoding != null) {
                    result = (result as BufferInstance).toString(encoding);
                }
                return result;
            } catch (Exception ex) {
                throw new JavaScriptException(Engine, "Error", ex.Message);
            }
        }

        [JSFunction(Name = "writeFile", IsEnumerable = true)]
        public void WriteFile(string path, object data, params object[] args) {
            ThreadPool.QueueUserWorkItem(new WaitCallback(WriteFileAsync), new object[] { path, data, args });
        }

        private void WriteFileAsync(object arguments) {
            object[] args = arguments as object[];
            FunctionInstance callback = null;
            ObjectInstance options = null;
            try {
                object data = args[1];
                object[] writeArgs = args[2] as object[];
                if (writeArgs.Length >= 2) {
                    options = writeArgs[0] as ObjectInstance;
                    callback = writeArgs[1] as FunctionInstance;
                } else if (writeArgs.Length >= 1) {
                    if (writeArgs[0] is FunctionInstance) {
                        callback = writeArgs[0] as FunctionInstance;
                    }
                }
                string path = CheckPath(args[0] as string);
                if (TypeUtil.IsString(data)) {
                    string str = TypeUtil.ToString(data);
                    string encoding = options != null ? options.GetPropertyValue("encoding") as string : null;
                    if (encoding == null)
                        encoding = "utf8";
                    Encoding enc = Context.GetEncoding(encoding);
                    if (enc == null)
                        throw new Exception("Unknown encoding: " + encoding);
                    File.WriteAllBytes(path, enc.GetBytes(str));
                } else if (data is BufferInstance) {
                    BufferInstance buf = data as BufferInstance;
                    if (buf.Offset == 0 && buf.Limit == buf.Buffer.Length) {
                        File.WriteAllBytes(path, buf.Buffer);
                    } else {
                        ArraySegment<byte> seg = new ArraySegment<byte>(buf.Buffer, buf.Offset, buf.Limit - buf.Offset);
                        File.WriteAllBytes(path, seg.Array);
                    }
                } else {
                    throw new Exception("illegal data");
                }
                if (callback != null)
                    Context.NextTick(callback.Bind(this, null));
            } catch (Exception ex) {
                System.Console.WriteLine(ex.ToString()+"\n"+ex.StackTrace);
                if (callback != null)
                    Context.NextTick(callback.Bind(this, Engine.Error.Construct(ex.Message)));
            }
        }

        [JSFunction(Name = "writeFileSync", IsEnumerable = true)]
        public void WriteFileSync(string path, object data, [DefaultParameterValue(null)] ObjectInstance options = null) {
            if (!AllowSync)
                throw new JavaScriptException(Engine, "Error", "synchronous operations are not permitted");
            try {
                path = CheckPath(path);
                if (data is string) {
                    string str = data as string;
                    string encoding = options != null ? options.GetPropertyValue("encoding") as string : null;
                    if (encoding == null)
                        encoding = "utf8";
                    Encoding enc = Context.GetEncoding(encoding);
                    if (enc == null)
                        throw new Exception("Unknown encoding: " + encoding);
                    byte[] b = enc.GetBytes(str);
                    File.WriteAllBytes(path, b);
                } else if (data is BufferInstance) {
                    BufferInstance buf = data as BufferInstance;
                    if (buf.Offset == 0 && buf.Limit == buf.Buffer.Length) {
                        File.WriteAllBytes(path, buf.Buffer);
                    } else {
                        ArraySegment<byte> seg = new ArraySegment<byte>(buf.Buffer, buf.Offset, buf.Limit - buf.Offset);
                        File.WriteAllBytes(path, seg.Array);
                    }
                } else {
                    throw new Exception("illegal data");
                }
            } catch (Exception ex) {
                throw new JavaScriptException(Engine, "Error", ex.Message);
            }
        }

        [JSFunction(Name = "exists", IsEnumerable = true)]
        public void Exists(string path, FunctionInstance callback) {
            ThreadPool.QueueUserWorkItem(new WaitCallback(ExistsAsync), new object[] { path, callback });
        }

        private void ExistsAsync(object arguments) {
            object[] args = arguments as object[];
            FunctionInstance callback = args[1] as FunctionInstance;
            try {
                string path = CheckPath(args[0] as string);
                bool result = File.Exists(path);
                if (callback != null)
                    Context.NextTick(callback.Bind(this, null, result));
            } catch (Exception ex) {
                if (callback != null)
                    Context.NextTick(callback.Bind(this, Engine.Error.Construct(ex.Message)));
            }
        }

        [JSFunction(Name = "existsSync", IsEnumerable = true)]
        public bool ExistsSync(string path) {
            if (!AllowSync)
                throw new JavaScriptException(Engine, "Error", "synchronous operations are not permitted");
            try {
                return File.Exists(CheckPath(path));
            } catch (Exception) {
                return false;
            }
        }

        // TODO: fs.WriteStream/ReadStream
    }
}
