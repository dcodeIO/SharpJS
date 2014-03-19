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
using System.Threading;

namespace SharpJS
{
    class Program
    {
        static Context Context;

        static void Main(string[] args) {
            Context = new Context();
            Context.Fs.Chroot = Context.Process.Instance.Cwd();
            Module main = new JavaScriptModule(Context, "tests/test.js");
            Context.Run(main);
            Thread t = new Thread(new ThreadStart(Run));
            t.Start();
        }

        static void Run() {
            while (true) {
                Context.Tick();
                if (Context.MayExit()) {
                    Context.OnExit();
                    Console.ReadKey();
                    Environment.Exit(0);
                }
                Thread.Sleep(100);
            }
        }
    }
}
