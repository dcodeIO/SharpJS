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
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SharpJS.Libraries
{
    public class Path : /* Constructor-less */ Module
    {
        public static readonly bool IsWindows = Environment.OSVersion.VersionString.IndexOf("Microsoft Windows") >= 0;
        public static readonly bool IsPosix = !IsWindows;
        public static readonly char Sep = System.IO.Path.DirectorySeparatorChar;
        public static readonly char Delimiter = System.IO.Path.PathSeparator;

        private static Regex WindowsSplitDeviceRe = new Regex("^([a-zA-Z]:|[\\\\/]{2}[^\\\\/]+[\\\\/]+[^\\\\/]+)?([\\\\/])?([\\s\\S]*?)$", RegexOptions.Compiled);
        private static Regex WindowsSplitTailRe = new Regex("^([\\s\\S]*?)((?:\\.{1,2}|[^\\\\/]+?|)(\\.[^./\\\\]*|))(?:[\\\\/]*)$", RegexOptions.Compiled);
        private static Regex WindowsPreSplitTailRe = new Regex("[\\\\/]+", RegexOptions.Compiled);
        private static Regex WindowsTrailingSlashRe = new Regex("[\\\\/]$", RegexOptions.Compiled);
        private static Regex WindowsTrailingSlashAllRe = new Regex("[\\\\/]+$", RegexOptions.Compiled);
        private static Regex WindowsPreUncRe = new Regex("^[\\\\/]+", RegexOptions.Compiled);
        private static Regex WindowsPostUncRe = new Regex("[\\\\/]+", RegexOptions.Compiled);

        public Path(Context context) : base(context) {
            Populate();
        }

        private static string NormalizeUNCRoot(string device) {
            return "\\\\" + WindowsPostUncRe.Replace(WindowsPreUncRe.Replace(device, ""), "\\");
        }

        private static string[] NormalizeArray(string[] parts, bool allowAboveRoot) {
            // if the path tries to go above the root, `up` ends up > 0
            int up = 0;
            List<string> normalized = new List<string>(parts.Length);
            for (int i = parts.Length - 1; i >= 0; --i) {
                string last = parts[i];
                if (last == "" || last == ".") {
                    continue;
                }
                if (last == "..") {
                    up++;
                    continue;
                } else if (up > 0) {
                    --up;
                    continue;
                }
                normalized.Insert(0, last);
            }
            // if the path is allowed to go above the root, restore leading ..s
            if (allowAboveRoot) {
                for (; up-- > 0; ) {
                    normalized.Insert(0, "..");
                }
            }
            return normalized.ToArray();
        }

        [JSFunction(Name = "normalize", IsEnumerable = true)]
        public string Normalize(string path) {
            bool isAbsolute = IsAbsolute(path);
            if (IsWindows) {
                Match result = WindowsSplitDeviceRe.Match(path);
                string device = result.Success ? result.Groups[1].Value : "";
                bool isUnc = device != "" && device[1] != ':';
                string tail = result.Success ? result.Groups[3].Value : "";
                bool trailingSlash = WindowsTrailingSlashRe.IsMatch(path);

                // If device is a drive letter, we'll normalize to lower case.
                if (device != "" && device[1] == ':') {
                    device = ("" + device[0]).ToLower() + device.Substring(1);
                }

                // Normalize the tail path
                tail = string.Join("\\", NormalizeArray(WindowsPreSplitTailRe.Split(tail), !isAbsolute));
                if (tail == "" && !isAbsolute) {
                    tail = ".";
                }
                if (tail != "" && trailingSlash) {
                    tail += '\\';
                }

                // Convert slashes to backslashes when `device` points to an UNC root.
                // Also squash multiple slashes into a single one where appropriate.
                if (isUnc) {
                    device = NormalizeUNCRoot(device);
                }

                return device + (isAbsolute ? "\\" : "") + tail;
            } else {
                bool trailingSlash = path.Length > 0 && path[path.Length - 1] == '/';
                string[] segments = path.Split('/');

                // Normalize the path
                path = string.Join("/", NormalizeArray(segments, !isAbsolute));

                if (path == "" && !isAbsolute) {
                    path = ".";
                }
                if (path != "" && trailingSlash) {
                    path += '/';
                }

                return (isAbsolute ? "/" : "") + path;
            }
        }

        [JSFunction(Name = "isAbsolute", IsEnumerable = true)]
        public bool IsAbsolute(string path) {
            if (path == null)
                return false;
            if (IsWindows) {
                Match result = WindowsSplitDeviceRe.Match(path);
                string device = result.Success ? result.Groups[1].Value : "";
                bool isUnc = device.Length > 1 && device[1] != ':';
                return result.Success && result.Groups[2].Value != "" || isUnc;
            } else {
                return path.Length > 0 && path[0] == '/';
            }
        }

        // The following methods use System.IO. Is this safe also on Mono?

        [JSFunction(Name = "join", IsEnumerable = true)]
        public string Join(params string[] arguments) {
            string path = arguments[0];
            for (var i = 1; i < arguments.Length; i++) {
                path = System.IO.Path.Combine(path, arguments[i]);
            }
            return Normalize(path);
        }

        [JSFunction(Name = "resolve", IsEnumerable = true)]
        public string Resolve(params string[] arguments) {
            string to = arguments[arguments.Length - 1];
            string res = null;
            try {
                res = System.IO.Path.GetFullPath(to);
            } catch (Exception) {
            }
            if (res != null)
                return Normalize(res);
            for (var i = arguments.Length - 2; i >= 0; --i) {
                to = System.IO.Path.Combine(arguments[i], to);
                try {
                    res = System.IO.Path.GetFullPath(to);
                } catch (Exception) {
                }
                if (res != null)
                    return Normalize(res);
            }
            res = System.IO.Path.Combine(Context.Process.Instance.Cwd(), arguments[arguments.Length - 1]);
            return Normalize(System.IO.Path.GetFullPath(res));
        }

        [JSFunction(Name = "dirname", IsEnumerable = true)]
        public string Dirname(string path) {
            if (path == null)
                throw new JavaScriptException(Engine, "TypeError", "null");
            return System.IO.Path.GetDirectoryName(path);
        }

        [JSFunction(Name = "basename", IsEnumerable = true)]
        public string Basename(string path) {
            if (path == null)
                throw new JavaScriptException(Engine, "TypeError", "null");
            return Basename(path, null);
        }

        [JSFunction(Name = "basename", IsEnumerable = true)]
        public string Basename(string path, string ext) {
            if (path == null)
                throw new JavaScriptException(Engine, "TypeError", "null");
            string filename = System.IO.Path.GetFileName(path);
            if (ext != null && ext != "" && filename.EndsWith(ext, IsWindows ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                filename = filename.Substring(0, filename.Length - ext.Length);
            return filename;
        }

        [JSFunction(Name = "extname", IsEnumerable = true)]
        public string Extname(string path) {
            if (path == null)
                throw new JavaScriptException(Engine, "TypeError", "null");
            return System.IO.Path.GetExtension(path);
        }

        [JSProperty(Name = "sep", IsEnumerable = true)]
        public string sep {
            get {
                return new string(Sep, 1);
            }
        }

        [JSProperty(Name = "delimiter", IsEnumerable = true)]
        public string delimiter {
            get {
                return new string(Delimiter, 1);
            }
        }

        [JSFunction(Name = "relative", IsEnumerable = true)]
        public string Relative(string from, string to) {
            if (from == null || to == null)
                throw new JavaScriptException(Engine, "TypeError", "null");
            if (IsWindows) {
                from = Resolve(from);
                to = Resolve(to);
                string lowerFrom = from.ToLower();
                string lowerTo = to.ToLower();
                string[] toParts = WindowsTrailingSlashAllRe.Replace(to, "").Split('\\');
                string[] lowerFromParts = WindowsTrailingSlashAllRe.Replace(from, "").Split('\\');
                string[] lowerToParts = WindowsTrailingSlashAllRe.Replace(to, "").Split('\\');
                int length = Math.Min(lowerFromParts.Length, lowerToParts.Length);
                int samePartsLength = length;
                for (int i = 0; i < length; ++i) {
                    if (lowerFromParts[i] != lowerToParts[i]) {
                        samePartsLength = i;
                        break;
                    }
                }
                if (samePartsLength == 0) {
                    return to;
                }
                List<string> outputParts = new List<string>();
                for (var i = samePartsLength; i < lowerFromParts.Length; ++i) {
                    outputParts.Add("..");
                }
                for (var i = samePartsLength; i < toParts.Length; ++i) {
                    outputParts.Add(toParts[i]);
                }
                return string.Join("\\", outputParts.ToArray());
            } else {
                from = Resolve(from).Substring(1);
                to = Resolve(to).Substring(1);
                string[] fromParts = WindowsTrailingSlashAllRe.Replace(from, "").Split('/');
                string[] toParts = WindowsTrailingSlashAllRe.Replace(to, "").Split('/');
                int length = Math.Min(fromParts.Length, toParts.Length);
                int samePartsLength = length;
                for (int i = 0; i < length; ++i) {
                    if (fromParts[i] != toParts[i]) {
                        samePartsLength = i;
                        break;
                    }
                }
                List<string> outputParts = new List<string>();
                for (int i = samePartsLength; i < fromParts.Length; ++i) {
                    outputParts.Add("..");
                }
                for (var i = samePartsLength; i < toParts.Length; ++i) {
                    outputParts.Add(toParts[i]);
                }
                return string.Join("/", outputParts.ToArray());
            }
        }
    }
}
