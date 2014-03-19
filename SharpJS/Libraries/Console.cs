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
using System.Collections.Generic;

namespace SharpJS.Libraries
{
    public class Console : Module {

        public readonly ConsoleConstructor Constructor;
        public readonly ConsoleInstance /* Context-bound */ Instance;

        public Console(Context context) : base(context) {
            Constructor = new ConsoleConstructor(context);
            Instance = Constructor.Construct(null, null);
            Populate();
        }

        public override object Run() {
            return Instance;
        }

        public override Module RegisterGlobals() {
            Engine.SetGlobalValue("console", Instance);
            return this;
        }

        public enum MessageStyle
        {
            Regular,
            Information,
            Warning,
            Error,
        }

        public class ConsoleConstructor : ConstructorFunction
        {
            public ConsoleConstructor(Context context) : base(context, "Console", new ConsoleInstance(context.Engine.Object.InstancePrototype, false)) {
                Populate();
            }

            [JSConstructorFunction]
            public ConsoleInstance Construct(object stdout, object stderr) {
                return new ConsoleInstance(Context, this.InstancePrototype);
            }
        }

        public delegate void LogEventHandler(object sender, LogEventArgs e);

        public class LogEventArgs : EventArgs
        {
            public MessageStyle Style;
            public string Message;

            public LogEventArgs(MessageStyle style, string message) {
                Style = style;
                Message = message;
            }
        }

        public class ConsoleInstance : ContextObjectInstance
        {
            public string Prefix = null;
            public event LogEventHandler DoLog;

            // Prototype constructor
            public ConsoleInstance(ObjectInstance nextPrototype, bool extended) : base(nextPrototype, true) {
                if (!extended)
                    Populate();
            }

            // Instance constructor
            public ConsoleInstance(Context context, ObjectInstance thisPrototype) : base(context, thisPrototype) {
            }

            [JSProperty(Name = "Console", IsEnumerable = true)]
            public ConsoleConstructor Console_ {
                get {
                    return Context.Console.Constructor;
                }
            }

            [JSFunction(Name = "dir", IsEnumerable = true)]
            public void dir(params object[] arguments) {
                // TODO
                Log(arguments);
            }

            [JSFunction(Name = "log", IsEnumerable = true)]
            public void Log(params object[] arguments) {
                Log(MessageStyle.Regular, FormatObjects(arguments));
            }

            [JSFunction(Name = "info", IsEnumerable = true)]
            public void Info(params object[] arguments) {
                Log(MessageStyle.Information, FormatObjects(arguments));
            }

            [JSFunction(Name = "error", IsEnumerable = true)]
            public void Error(params object[] arguments) {
                Log(MessageStyle.Error, FormatObjects(arguments));
            }

            [JSFunction(Name = "warn", IsEnumerable = true)]
            public void Warn(params object[] arguments) {
                Log(MessageStyle.Warning, FormatObjects(arguments));
            }

            public void Time(string label) {
                TimeLabel timeLabel = new TimeLabel();
                timeLabel.Name = label;
                timeLabel.StartTime = DateTime.Now.Ticks / 10000;
            }

            public struct TimeLabel
            {
                public string Name;
                public long StartTime;
            }

            /// <summary>
            /// Logs a message to the console.
            /// </summary>
            /// <param name="style"> A style which influences the icon and text color. </param>
            /// <param name="objects"> The objects to output to the console. These can be strings or
            /// ObjectInstances. </param>
            public void Log(MessageStyle style, object[] objects) {
                // Convert the objects to a string.
                var message = new System.Text.StringBuilder();
                if (Prefix != null && Prefix.Length > 0) {
                    message.Append(Prefix);
                    message.Append(' ');
                }
                for (int i = 0; i < objects.Length; i++) {
                    if (i > 0)
                        message.Append(' ');
                    message.Append(TypeConverter.ToString(objects[i]));
                }

                // Allow event handlers to override default behaviour
                if (DoLog != null) {
                    DoLog(this, new LogEventArgs(style, message.ToString()));
                    return;
                }

#if !SILVERLIGHT
                var original = System.Console.ForegroundColor;
                switch (style) {
                    case MessageStyle.Information:
                        System.Console.ForegroundColor = ConsoleColor.White;
                        break;
                    case MessageStyle.Warning:
                        System.Console.ForegroundColor = ConsoleColor.Yellow;
                        break;
                    case MessageStyle.Error:
                        System.Console.ForegroundColor = ConsoleColor.Red;
                        break;
                }
#endif

                // Output the message to the console.
                System.Console.WriteLine(message.ToString());


#if !SILVERLIGHT
                if (style != MessageStyle.Regular)
                    System.Console.ForegroundColor = original;
#endif
            }
        }

        // From FirebugConsole.cs:

        /// <summary>
        /// Formats a message.  The objects provided will be converted to strings then
        /// joined together in a space separated line.  The first parameter can be a string
        /// containing the following patterns:
        ///  %s	 String
        ///  %d, %i	 Integer
        ///  %f	 Floating point number
        ///  %o	 Object hyperlink
        /// </summary>
        /// <param name="items"> The items to format. </param>
        /// <returns> An array containing formatted strings interspersed with objects. </returns>
        public static object[] FormatObjects(object[] items) {
            if (items.Length == 0)
                return new object[0];
            var result = new List<object>();
            var formattedString = new System.Text.StringBuilder();

            // If the first item is a string, then it is assumed to be a format string.
            int itemsConsumed = 1;
            if (items[0] is string) {
                string formatString = (string)items[0];

                int previousPatternIndex = 0, patternIndex;
                while (items.Length > itemsConsumed) {
                    // Find a percent sign.
                    patternIndex = formatString.IndexOf('%', previousPatternIndex);
                    if (patternIndex == -1 || patternIndex == formatString.Length - 1)
                        break;

                    // Append the text that didn't contain a pattern to the result.
                    formattedString.Append(formatString, previousPatternIndex, patternIndex - previousPatternIndex);

                    // Extract the pattern type.
                    char patternType = formatString[patternIndex + 1];

                    // Determine the replacement string.
                    string replacement;
                    switch (patternType) {
                        case 's':
                            replacement = TypeConverter.ToString(items[itemsConsumed++]);
                            break;
                        case 'd':
                        case 'i':
                            var number = TypeConverter.ToNumber(items[itemsConsumed++]);
                            replacement = (number >= 0 ? Math.Floor(number) : Math.Ceiling(number)).ToString();
                            break;
                        case 'f':
                            replacement = TypeConverter.ToNumber(items[itemsConsumed++]).ToString();
                            break;
                        case '%':
                            replacement = "%";
                            break;
                        case 'o':
                            replacement = string.Empty;
                            if (formattedString.Length > 0)
                                result.Add(formattedString.ToString());
                            result.Add(items[itemsConsumed++]);
                            formattedString.Remove(0, formattedString.Length);
                            break;
                        default:
                            replacement = "%" + patternType;
                            break;
                    }

                    // Replace the pattern with the corresponding argument.
                    formattedString.Append(replacement);

                    // Start searching just after the end of the pattern.
                    previousPatternIndex = patternIndex + 2;
                }

                // Append the text that didn't contain a pattern to the result.
                formattedString.Append(formatString, previousPatternIndex, formatString.Length - previousPatternIndex);

                // Add the formatted string to the resulting array.
                if (formattedString.Length > 0)
                    result.Add(formattedString.ToString());

                // Append the items that weren't consumed to the end of the resulting array.
                for (int i = itemsConsumed; i < items.Length; i++)
                    result.Add(items[i]);
                return result.ToArray();
            } else {
                // The first item is not a string - just return the objects verbatim.
                return items;
            }
        }
    }
}
