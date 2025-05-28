using dash.Execution.Units;
using dash.Lexing;
#pragma warning disable CS8600

namespace dash.Execution;

public class Scope
{
    private Dictionary<string, DValue> _variables = [];

    public virtual void CopyTo(Scope other)
    {
        foreach (var i in _variables)
        {
            other.Set(i.Key, i.Value);
        }
    }
    
    public virtual DValue Get(string name, ParsedMeta meta)
    {
        if (_variables.TryGetValue(name, out DValue t))
        {
            return t;
        }
        
        Eroro.MakeEroro($"Variable '{name}' not found", meta);
        throw new Exception();
    }

    public virtual bool Exists(string name)
    {
        return _variables.ContainsKey(name);
    }

    public virtual void ReAssign(string name, DValue value, ParsedMeta meta)
    {
        if (_variables.ContainsKey(name))
        {
            _variables[name] = value;
            return;
        }
        Eroro.MakeEroro($"Variable with name '{name}' doesn't exist", meta);
    }

    public virtual void Let(string name, DValue value, ParsedMeta meta)
    {
        if (_variables.ContainsKey(name))
        {
            Eroro.MakeEroro($"Variable with name '{name}' already exists", meta);
            throw new Exception();
        }

        _variables[name] = value;
    }

    public virtual ChildScope MakeScope()
    {
        return new(this);
    }

    public virtual void Set(string name, DValue value)
    {
        _variables[name] = value;
    }

    public virtual void Remove(string name, ParsedMeta meta)
    {
        if (_variables.ContainsKey(name))
        {
            _variables.Remove(name);
            return;
        }
        Eroro.MakeEroro($"Variable with name '{name}' doesn't exist", meta);
        throw new Exception();
    }
}

public class ChildScope(Scope parent) : Scope
{
    private Scope _this = new();
    public Scope Parent = parent;

    public override bool Exists(string name)
    {
        if (_this.Exists(name))
        {
            return true;
        }
        return Parent.Exists(name);
    }

    public override void CopyTo(Scope other)
    {
        Parent.CopyTo(other);
        _this.CopyTo(other);
    }

    public override DValue Get(string name, ParsedMeta meta)
    {
        if (_this.Exists(name))
        {
            return _this.Get(name, meta);
        }
        return Parent.Get(name, meta);
    }

    public override void Let(string name, DValue value, ParsedMeta meta)
    {
        _this.Let(name, value, meta);
    }

    public override void ReAssign(string name, DValue value, ParsedMeta meta)
    {
        if (_this.Exists(name))
        {
            _this.ReAssign(name, value, meta);
            return;
        }
        Parent.ReAssign(name, value, meta);
    }

    public override ChildScope MakeScope()
    {
        return new(this);
    }

    public override void Set(string name, DValue value)
    {
        _this.Set(name, value);
    }

    public override void Remove(string name, ParsedMeta meta)
    {
        if (_this.Exists(name))
        {
            _this.Remove(name, meta);
            return;
        }
        Parent.Remove(name, meta);
    }

    public void SetParent(string name, DValue value)
    {
        Parent.Set(name, value);
    }
}