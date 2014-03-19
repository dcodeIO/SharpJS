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
using Jurassic.Library;
using SharpJS.Helpers;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SharpJS.Libraries
{
    public class QueryString : /* Constructor-less */ Module
    {
        public QueryString(Context context) : base(context) {
            Populate();
        }

        private Regex SpacesRe = new Regex("\\+", RegexOptions.Compiled);

        [JSFunction(Name = "unescape", IsEnumerable = true)]
        public string Unescape(string str, [DefaultParameterValue(false)] bool decodeSpaces = false) {
            if (decodeSpaces)
                str = SpacesRe.Replace(str, "%20");
            return GlobalObject.DecodeURIComponent(Engine, str);
        }

        [JSFunction(Name = "escape", IsEnumerable = true)]
        public string Escape(string str) {
            return GlobalObject.EncodeURIComponent(Engine, str);
        }

        [JSFunction(Name = "stringify", IsEnumerable = true)]
        public string Stringify(object obj, [DefaultParameterValue("&")] string sep = "&", [DefaultParameterValue("=")] string eq = "=") {
            if (obj == null || !Util.IsObject(obj)) {
                return "";
            }
            ObjectInstance o = obj as ObjectInstance;
            List<string> parts = new List<string>();
            foreach (PropertyNameAndValue kv in o.Properties) {
                string ks = Escape(kv.Name) + eq;
                if (TypeUtil.IsArray(kv.Value)) {
                    foreach (object v in (kv.Value as ArrayInstance).ElementValues) {
                        parts.Add(ks + Escape(TypeUtil.ToString(v)));
                    }
                } else {
                    parts.Add(ks + Escape(TypeUtil.ToString(kv.Value)));
                }
            }
            return string.Join(sep, parts.ToArray());
        }

        [JSFunction(Name = "encode", IsEnumerable = true)]
        public string Encode(object obj, [DefaultParameterValue("&")] string sep = "&", [DefaultParameterValue("=")] string eq = "=") {
            return Stringify(obj, sep, eq);
        }

        [JSFunction(Name = "parse", IsEnumerable = true)]
        public object Parse(string qs, [DefaultParameterValue("&")] string sep = "&", [DefaultParameterValue("=")] string eq = "=", [DefaultParameterValue(null)] ObjectInstance options = null) {
            ObjectInstance o = Engine.Object.Construct();
            if (qs == null || qs.Length == 0)
                return o;
            string[] parts = qs.Split(sep[0]);
            int maxKeys = 1000;
            if (options != null) {
                object _maxKeys = options.GetPropertyValue("maxKey");
                if (TypeUtil.IsInteger(_maxKeys))
                    maxKeys = TypeUtil.ToInteger(options.GetPropertyValue("maxKeys"));
            }
            int len = parts.Length;
            if (maxKeys > 0 && len > maxKeys) {
                len = maxKeys;
            }
            for (int i = 0; i < len; ++i) {
                string x = SpacesRe.Replace(parts[i], "%20");
                int idx = x.IndexOf(eq);
                string kstr, vstr, k = null, v = null;
                if (idx >= 0) {
                    kstr = x.Substring(0, idx);
                    vstr = x.Substring(idx + 1);
                } else {
                    kstr = x;
                    vstr = "";
                }
                try {
                    k = Unescape(kstr);
                    v = Unescape(vstr);
                } catch (Exception) {
                    try {
                        k = Unescape(kstr, true);
                        v = Unescape(vstr, true);
                    } catch (Exception) {
                        continue;
                    }
                }
                if (!o.HasProperty(k)) {
                    o[k] = v;
                } else if (TypeUtil.IsArray(o[k])) {
                    (o[k] as ArrayInstance).Push(v);
                } else {
                    o[k] = Engine.Array.Construct(new object[] { o[k], v });
                }
            }
            return o;
        }

        [JSFunction(Name = "decode", IsEnumerable = true)]
        public object Decode(string qs, [DefaultParameterValue("&")] string sep = "&", [DefaultParameterValue("=")] string eq = "=", [DefaultParameterValue(null)] ObjectInstance options = null) {
            return Parse(qs, sep, eq, options);
        }
    }


}
