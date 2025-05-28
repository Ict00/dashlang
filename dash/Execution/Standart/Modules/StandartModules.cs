using dash.Execution;
using dash.Execution.Expressions;
using dash.Execution.Structures;
using dash.Execution.Units;
using dash.Lexing;
using dash.Parsing;

namespace dash.Etc;

public static class StandartModules
{
    public static Dictionary<string, List<(string name, ICallable callable)>> fun =
        new Dictionary<string, List<(string name, ICallable callable)>>()
        {
            ["console"] = [
                new ValueTuple<string, ICallable>("println", new BDFunction((List<DValue> values, ParsedMeta meta,
                    ref ExecCtx ctx) =>
                {
                    string t = "";

                    foreach (var i in values)
                    {
                        t += i.Show();
                    }

                    Console.WriteLine(t);

                    return new DValue(null, SimpleDType.NULL);
                }, -1)),
                new ValueTuple<string, ICallable>("print", new BDFunction((List<DValue> values, ParsedMeta meta,
                    ref ExecCtx ctx) =>
                {
                    foreach (var i in values)
                    {
                        Console.Write(i.Show());
                    }

                    return new DValue(null, SimpleDType.NULL);
                }, -1)),
                new ValueTuple<string, ICallable>("input", new BDFunction((List<DValue> values, ParsedMeta meta,
                    ref ExecCtx ctx) =>
                {
                    var val = Console.ReadLine();

                    return val == null ? new DValue(null, SimpleDType.NULL) : new DValue(val, SimpleDType.STR);
                }, 0)),
                new ValueTuple<string, ICallable>("visible", new BDFunction((List<DValue> values, ParsedMeta meta,
                    ref ExecCtx ctx) =>
                {
                    Console.CursorVisible = values[0].AsBool(meta);

                    return new DValue(null, SimpleDType.NULL);
                }, 1)),
                new ValueTuple<string, ICallable>("bufferHeight", new BDFunction((List<DValue> values, ParsedMeta meta,
                    ref ExecCtx ctx) => new DValue(Console.BufferHeight, SimpleDType.INT), 0)),
                new ValueTuple<string, ICallable>("bufferWidth", new BDFunction((List<DValue> values, ParsedMeta meta,
                    ref ExecCtx ctx) => new DValue(Console.BufferWidth, SimpleDType.INT), 0)),
            ],
            ["list"] = [
                new ValueTuple<string, ICallable>("add", new BDFunction((List<DValue> values, ParsedMeta meta,
                    ref ExecCtx ctx) =>
                {
                    values[0].AsList(meta).Add(values[1]);
                    return new DValue(null, SimpleDType.NULL);
                }, 2)),
                new ValueTuple<string, ICallable>("has", new BDFunction((List<DValue> values, ParsedMeta meta,
                    ref ExecCtx ctx) =>
                {
                    foreach (var i in values[0].AsList(meta))
                    {
                        if (i.GetRawNonNull(meta).Equals(values[1].GetRawNonNull(meta)))
                        {
                            return new DValue(true, SimpleDType.BOOL);
                        }
                    }

                    return new DValue(false, SimpleDType.BOOL);
                }, 2)),
                new ValueTuple<string, ICallable>("removeAt", new BDFunction((List<DValue> values, ParsedMeta meta,
                    ref ExecCtx ctx) =>
                {
                    var ind = values[1].AsInt(meta);
                    var list = values[0].AsList(meta);

                    if (ind < 0)
                    {
                        ind = list.Count + ind;
                    }

                    if (ind >= list.Count)
                    {
                        Eroro.MakeEroro($"Index out of bounds: tried to get '{ind}' in list of '{list.Count}' elements",
                            meta);
                        throw new Exception();
                    }


                    list.RemoveAt(ind);

                    return new DValue(null, SimpleDType.NULL);
                }, 2)),
                new ValueTuple<string, ICallable>("insert", new BDFunction((List<DValue> values, ParsedMeta meta,
                    ref ExecCtx ctx) =>
                {
                    var val = values[1].AsInt(meta);

                    if (val < 0)
                    {
                        val = values[0].AsList(meta).Count + val;
                    }

                    if (val >= values[0].AsList(meta).Count)
                    {
                        Eroro.MakeEroro("Index out of bounds", meta);
                    }

                    values[0].AsList(meta).Insert(val, values[3]);

                    return new DValue(null, SimpleDType.NULL);
                }, 3)),
                new ValueTuple<string, ICallable>("slice", new BDFunction((List<DValue> values, ParsedMeta meta,
                    ref ExecCtx ctx) =>
                {
                    var lst = values[0].AsList(meta);
                    var start = values[1].AsInt(meta);
                    var count = values[2].AsInt(meta);

                    if(start + count < lst.Count && start + count >= 0)
                    {
                        return new DValue(lst.Slice(start, count), SimpleDType.LIST);
                    }

                    Eroro.MakeEroro("Index out of bounds", meta);

                    return new DValue(null, SimpleDType.NULL);
                }, 3)),
                new ValueTuple<string, ICallable>("len", new BDFunction((List<DValue> values, ParsedMeta meta,
                    ref ExecCtx ctx) => new DValue(values[0].AsList(meta).Count, SimpleDType.INT),1)),
            ],
            ["str"] = [
                new ValueTuple<string, ICallable>("len", new BDFunction((List<DValue> values, ParsedMeta meta,
                    ref ExecCtx ctx) => new DValue(values[0].AsString().Length, SimpleDType.INT),1)),
                new ValueTuple<string, ICallable>("upper", new BDFunction((List<DValue> values, ParsedMeta meta,
                    ref ExecCtx ctx) => new DValue(values[0].AsString().ToUpperInvariant(), SimpleDType.STR),1)),
                new ValueTuple<string, ICallable>("lower", new BDFunction((List<DValue> values, ParsedMeta meta,
                    ref ExecCtx ctx) => new DValue(values[0].AsString().ToLowerInvariant(), SimpleDType.STR),1)),
                new ValueTuple<string, ICallable>("split", new BDFunction((List<DValue> values, ParsedMeta meta,
                    ref ExecCtx ctx) =>
                {
                    var toSplit = values[0].AsString();
                    var separator = values[1].AsString();

                    var splitted = toSplit.Split(separator);

                    List<DValue> vals = [];

                    foreach (var i in splitted)
                    {
                        vals.Add(new DValue(i, SimpleDType.STR));
                    }


                    return new DValue(vals, SimpleDType.LIST);
                },2)),
                new ValueTuple<string, ICallable>("replace", new BDFunction((List<DValue> values, ParsedMeta meta,
                    ref ExecCtx ctx) =>
                {
                    var target = values[0].AsString();
                    var toReplace = values[1].AsString();
                    var replaceWith = values[2].AsString();


                    return new DValue(target.Replace(toReplace, replaceWith), SimpleDType.STR);
                },3)),
                new ValueTuple<string, ICallable>("startsWith", new BDFunction((List<DValue> values, ParsedMeta meta,
                    ref ExecCtx ctx) =>
                {
                    var target = values[0].AsString();
                    var start = values[1].AsString();

                    return new DValue(target.StartsWith(start), SimpleDType.BOOL);
                },2)),
                new ValueTuple<string, ICallable>("endsWith", new BDFunction((List<DValue> values, ParsedMeta meta,
                    ref ExecCtx ctx) =>
                {
                    var target = values[0].AsString();
                    var end = values[1].AsString();

                    return new DValue(target.EndsWith(end), SimpleDType.BOOL);
                },2)),
                new ValueTuple<string, ICallable>("subString", new BDFunction((List<DValue> values, ParsedMeta meta,
                    ref ExecCtx ctx) =>
                {
                    var target = values[0].AsString();
                    var start = values[1].AsInt(meta);
                    var end = values[2].AsInt(meta);

                    if (start >= 0 && end >= 0 && start < end && end <= target.Length && start < target.Length)
                    {
                        return new DValue(target.Substring(start, end), SimpleDType.STR);
                    }
                    else
                    {
                        Eroro.MakeEroro($"Wrong arguments given; [start: {start}, end: {end}, length of str: {target.Length}]", meta);
                        throw new Exception();
                    }
                },3)),
            ],
            ["thread"] = [
                new ValueTuple<string, ICallable>("sleep", new BDFunction((List<DValue> values, ParsedMeta meta,
                    ref ExecCtx ctx) =>
                {
                    var ms = values[0].AsInt(meta);

                    Thread.Sleep(ms);

                    return new DValue(null, SimpleDType.NULL);
                },1)),
                new ValueTuple<string, ICallable>("start", new BDFunction((List<DValue> values, ParsedMeta meta,
                    ref ExecCtx ctx) =>
                {
                    var callable = values[0].AsCallable(meta);

                    void Call(ExecCtx ctx)
                    {
                        callable.Call([], meta, ref ctx);
                    }

                    var execCtx = ctx;
                    new Thread(() => Call(execCtx)).Start();

                    return new DValue(null, SimpleDType.NULL);
                },1)),
            ],
            ["reflection"] =
            [
                new ValueTuple<string, ICallable>("bindMod", new BDFunction((List<DValue> values, ParsedMeta meta,
                    ref ExecCtx ctx) =>
                {
                    var module = values[0].AsString();
                    var name = values[1].AsString();

                    ctx.CurrentState.Modules[module].Set(name, values[2]);

                    return new DValue(null, SimpleDType.NULL);
                },3)),
                new ValueTuple<string, ICallable>("makeStruct", new BDFunction((List<DValue> values, ParsedMeta meta,
                    ref ExecCtx ctx) =>
                {
                    var name = values[0].AsString();
                    var dict = values[1].AsDict(meta);

                    Dictionary<string, DValue> vals = [];
                    Dictionary<string, DType> types = [];

                    foreach (var i in dict)
                    {
                        vals[i.Key] = i.Value;
                        types[i.Key] = i.Value.GetDType();
                    }


                    return new DValue(new Struct()
                    {
                        _types = types,
                        _values = vals,
                        Name = name
                    }, new StructDType(name, []));
                },2)),
                new ValueTuple<string, ICallable>("invoke", new BDFunction((List<DValue> values, ParsedMeta meta,
                    ref ExecCtx ctx) =>
                {
                    var callable = values[0].AsCallable(meta);
                    var args = values[1].AsList(meta);

                    List<ADValue> vals = [];

                    foreach (var i in args)
                    {
                        vals.Add(i);
                    }

                    var res = callable.Call(vals, meta, ref ctx);


                    return res;
                },2)),
                new ValueTuple<string, ICallable>("using", new BDFunction((List<DValue> values, ParsedMeta meta,
                    ref ExecCtx ctx) =>
                {
                    var modules = values[0].AsList(meta);

                    foreach (var i in modules)
                    {
                        if (!ctx.CurrentState.Modules.ContainsKey(i.AsString()))
                        {
                            Eroro.MakeEroro($"Module '{i.AsString()}' is not imported", meta);
                        }
                        ctx.CurrentState.Modules[i.AsString()].CopyTo(ctx.CurrentScope);
                    }

                    return new DValue(null, SimpleDType.NULL);
                },1)),
                new ValueTuple<string, ICallable>("addField", new BDFunction((List<DValue> values, ParsedMeta meta,
                    ref ExecCtx ctx) =>
                {
                    var sourceStruct = values[0].AsStruct(meta);
                    var fieldName = values[1].AsString();

                    sourceStruct._values[fieldName] = values[2];
                    sourceStruct._types[fieldName] = values[2].GetDType();

                    return new DValue(null, SimpleDType.NULL);
                },3)),
                new ValueTuple<string, ICallable>("fields", new BDFunction((List<DValue> values, ParsedMeta meta,
                    ref ExecCtx ctx) =>
                {
                    var sourceStruct = values[0].AsStruct(meta);
                    List<DValue> vals = [];

                    foreach (var i in sourceStruct._values)
                    {
                        vals.Add(new DValue(new Dictionary<string, DValue>()
                        {
                            ["field"] = new DValue(i.Key,SimpleDType.STR),
                            ["value"] = i.Value
                        }, SimpleDType.DICT));
                    }

                    return new DValue(vals, SimpleDType.LIST);
                },1)),
                new ValueTuple<string, ICallable>("delField", new BDFunction((List<DValue> values, ParsedMeta meta,
                    ref ExecCtx ctx) =>
                {
                    var sourceStruct = values[0].AsStruct(meta);
                    var fieldName = values[1].AsString();
                    if(sourceStruct._values.ContainsKey(fieldName))
                        sourceStruct._values.Remove(fieldName);
                    else
                        Eroro.MakeEroro($"Field '{fieldName}' doesn't exist", meta);
                    return new DValue(null, SimpleDType.NULL);
                },2)),
                new ValueTuple<string, ICallable>("copyMod", new BDFunction((List<DValue> values, ParsedMeta meta,
                    ref ExecCtx ctx) =>
                {
                    var mod1 = values[0].AsString();
                    var mod2 = values[1].AsString();

                    ctx.CurrentState.GetModule(mod1, meta).CopyTo(ctx.CurrentState.GetModule(mod2, meta));

                    return new DValue(null, SimpleDType.NULL);
                },2)),
                new ValueTuple<string, ICallable>("bind", new BDFunction((List<DValue> values, ParsedMeta meta,
                    ref ExecCtx ctx) =>
                {
                    var name = values[0].AsString();

                    ctx.CurrentScope.Set(name, values[1]);

                    return new DValue(null, SimpleDType.NULL);
                },2)),
                new ValueTuple<string, ICallable>("ifHas", new BDFunction((List<DValue> values, ParsedMeta meta,
                    ref ExecCtx ctx) =>
                {
                    var name = values[0].AsString();

                    if (ctx.CurrentScope.Exists(name))
                    {
                        return ctx.CurrentScope.Get(name, meta);
                    }
                    else
                    {
                        return new DValue(null, SimpleDType.NULL);
                    }
                },1)),
                new ValueTuple<string, ICallable>("unimport", new BDFunction((List<DValue> values, ParsedMeta meta,
                    ref ExecCtx ctx) =>
                {
                    var lst = values[0].AsList(meta);

                    foreach (var i in lst)
                    {
                        if (!ctx.CurrentState.Modules.ContainsKey(i.AsString()))
                        {
                            Eroro.MakeEroro($"Module '{i.AsString()}' can't be un-imported since it doesn't exist", meta);
                            break;
                        }
                        ctx.CurrentState.Modules.Remove(i.AsString());
                    }

                    return new DValue(null, SimpleDType.NULL);
                },1)),
                new ValueTuple<string, ICallable>("del", new BDFunction((List<DValue> values, ParsedMeta meta,
                    ref ExecCtx ctx) =>
                {
                    var name = values[0].AsString();

                    if (ctx.CurrentScope.Exists(name))
                    {
                        ctx.CurrentScope.Remove(name, meta);
                    }

                    return new DValue(null, SimpleDType.NULL);
                },1)),
                new ValueTuple<string, ICallable>("delFromMod", new BDFunction((List<DValue> values, ParsedMeta meta,
                    ref ExecCtx ctx) =>
                {

                    var modName = values[0].AsString();
                    var name = values[1].AsString();

                    if (ctx.CurrentState.GetModule(modName, meta).Exists(name))
                    {
                        ctx.CurrentState.GetModule(modName, meta).Remove(name, meta);
                    }

                    return new DValue(null, SimpleDType.NULL);
                },2))
            ],
            ["dict"] = [
                new ValueTuple<string, ICallable>("hasKey", new BDFunction((List<DValue> values, ParsedMeta meta,
                    ref ExecCtx ctx) => new DValue(values[0].AsDict(meta).ContainsKey(values[1].AsString()), SimpleDType.BOOL), 2)),
            ],
            ["file"] = [
                new ValueTuple<string, ICallable>("exist", new BDFunction((List<DValue> values, ParsedMeta meta,
                    ref ExecCtx ctx) => new DValue(File.Exists(values[0].AsString()), SimpleDType.BOOL),1)),
                new ValueTuple<string, ICallable>("readAll", new BDFunction((List<DValue> values, ParsedMeta meta,
                    ref ExecCtx ctx) =>
                {
                    var name = values[0].AsString();

                    if (!File.Exists(name))
                    {
                        Eroro.MakeEroro($"File with path '{name}' doesn't exist", meta);
                        throw new Exception();
                    }

                    return new DValue(File.ReadAllText(name), SimpleDType.STR);
                },1)),
                new ValueTuple<string, ICallable>("writeAll", new BDFunction((List<DValue> values, ParsedMeta meta,
                    ref ExecCtx ctx) =>
                {
                    var name = values[0].AsString();

                    File.WriteAllText(name, values[1].AsString());

                    return new DValue(null, SimpleDType.NULL);
                },2)),
            ],
            ["dir"] = [
                new ValueTuple<string, ICallable>("exist", new BDFunction((List<DValue> values, ParsedMeta meta,
                    ref ExecCtx ctx) => new DValue(Directory.Exists(values[0].AsString()), SimpleDType.BOOL),1)),
                new ValueTuple<string, ICallable>("files", new BDFunction((List<DValue> values, ParsedMeta meta,
                    ref ExecCtx ctx) =>
                {
                    var i = values[0].AsString();

                    if (!Directory.Exists(i))
                    {
                        Eroro.MakeEroro($"Directory '{i}' doesn't exist", meta);
                    }

                    List<DValue> vals = [];

                    foreach (var c in Directory.GetFiles(i))
                    {
                        vals.Add(new DValue(c, SimpleDType.STR));
                    }

                    return new DValue(vals, SimpleDType.LIST);
                },1)),
                new ValueTuple<string, ICallable>("dirs", new BDFunction((List<DValue> values, ParsedMeta meta,
                    ref ExecCtx ctx) =>
                {
                    var i = values[0].AsString();

                    if (!Directory.Exists(i))
                    {
                        Eroro.MakeEroro($"Directory '{i}' doesn't exist", meta);
                    }

                    List<DValue> vals = [];

                    foreach (var c in Directory.GetDirectories(i))
                    {
                        vals.Add(new DValue(c, SimpleDType.STR));
                    }

                    return new DValue(vals, SimpleDType.LIST);
                },1)),
            ],
            ["env"] = [
                new ValueTuple<string, ICallable>("getPwd", new BDFunction((List<DValue> values, ParsedMeta meta,
                    ref ExecCtx ctx) =>
                {
                    return new DValue(Environment.CurrentDirectory, SimpleDType.STR);
                },0)),
                new ValueTuple<string, ICallable>("user", new BDFunction((List<DValue> values, ParsedMeta meta,
                    ref ExecCtx ctx) =>
                {
                    return new DValue(Environment.UserName, SimpleDType.STR);
                },0)),
                new ValueTuple<string, ICallable>("setPwd", new BDFunction((List<DValue> values, ParsedMeta meta,
                    ref ExecCtx ctx) =>
                {
                    var dir = values[0].AsString();

                    if (!Directory.Exists(dir))
                    {
                        Eroro.MakeEroro($"Directory '{dir}' doesn't exist", meta);
                    }

                    Environment.CurrentDirectory = dir;

                    return new DValue(null, SimpleDType.NULL);
                },1)),
                new ValueTuple<string, ICallable>("cmdArgs", new BDFunction((List<DValue> values, ParsedMeta meta,
                    ref ExecCtx ctx) =>
                {
                    List<DValue> vals = [];

                    foreach (var i in Environment.GetCommandLineArgs())
                    {
                        vals.Add(new DValue(i, SimpleDType.STR));
                    }

                    return new DValue(vals, SimpleDType.LIST);
                },1)),
                new ValueTuple<string, ICallable>("exit", new BDFunction((List<DValue> values, ParsedMeta meta,
                    ref ExecCtx ctx) =>
                {
                    if (values.Count == 1)
                    {
                        Environment.Exit(values[0].AsInt(meta));
                        return new DValue(null, SimpleDType.NULL);
                    }

                    Environment.Exit(0);

                    return new DValue(null, SimpleDType.NULL);
                },-1)),
            ]
        };
}
