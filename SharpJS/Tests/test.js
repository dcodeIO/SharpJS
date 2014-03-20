console.log("filename: " + __filename);
console.log("dirname: " + __dirname);

var events = require("events");

function inspect(obj, name) {
    for (var i in obj) {
        if (obj.hasOwnProperty(i)) {
            console.log(name + "." + i + " = " + obj[i]);
        }
    }
}

inspect(events, "events");
inspect(events.EventEmitter, "EventEmitter");
inspect(events.EventEmitter.prototype, "EventEmitter.prototype");
inspect(process, "process");
inspect(process.memoryUsage(), "process.memoryUsage()");
console.log("process.on: " + process.on);
var events = require("events");
console.log("process instanceof EventEmitter: "+(process instanceof events.EventEmitter));

setTimeout(function () {
    console.info("at 1s");
}, 1000);

var n = 0;
var to = setInterval(function () {
    console.log("at 2s interval");
    n++;
    if (n >= 3) {
        console.warn("Exiting");
        clearInterval(to);
    }
}, 2000);

var buffer = new Buffer(10);
console.log(buffer.length);
console.log(Array.isArray(buffer));
buffer.writeUInt8(-1, 0, true);
buffer[0] += 2;
console.log("read: " + buffer[0]);
console.log("not def: " + buffer[10]);
buffer = new Buffer("Hello World!", "utf8");
// buffer.writeInt32BE(0x12345678, 0);
console.log(buffer.toString("utf8")+" == "+buffer.toString("hex")+" == "+buffer.inspect());

var bin = new Buffer(256);
for (var i = 0; i < 256; i++) {
    bin[i] = i;
}
bin = new Buffer(bin.toString("binary"), "binary");
for (var i = 0; i < 256; i++) {
    if (bin[i] != i)
        throw new Error("Invalid value at "+i);
}

var path = require("path");
var cwd = process.cwd();
console.log("path.isAbsolute: " + path.isAbsolute(cwd));
console.log("path.join: " + path.join(cwd, ".."));
console.log("dumb path.join: " + path.join("test", "test2", ".."));
console.log("path.resolve: " + path.resolve("."));
console.log("path.dirname: " + path.dirname(process.cwd() + "\\somefile.js"));
console.log("path.basename: " + path.basename(process.cwd() + "\\somefile.js"));
console.log("path.extname: " + path.extname(process.cwd() + "\\somefile.js"));
console.log("path.relative: " + path.relative(process.cwd(), process.cwd()));

// console.log(JSON.stringify(process.env));

var qs = require("querystring"), qstr;
console.log("qs.stringify: "+(qstr=qs.stringify({ "a": "b", "c": ["d","e"] })));
console.log("qs.parse: "+JSON.stringify(qs.parse(qstr)));

var fs = require("fs");
fs.readdir(".", function (err, files) {
    if (err) {
        console.log("readdir: " + err);
        return;
    }
    console.log("readdir ok: "+JSON.stringify(files));
});

fs.writeFile("test.txt", "hello", function (err) {
    if (err) {
        console.log("writeFile failed: " + err);
        return;
    }
    console.log("writeFile ok");
    fs.exists("test.txt", function (err, exists) {
        if (err) {
            console.log("exists failed: " + err);
            return;
        }
        console.log("exists ok: " + exists);
        fs.readFile("test.txt", function (err, data) {
            if (err) {
                console.log("readFile failed: " + err);
                return;
            }
            console.log("readFile ok: " + data.toString("utf8"));
            fs.unlink("test.txt", function (err) {
                if (err) {
                    console.log("unlink failed: " + err);
                    return;
                }
                console.log("unlink ok");
            });
        });
    });
});

console.log("require: "+require("sub/requireme"));
console.log("require.cache: " + Object.keys(require.cache));