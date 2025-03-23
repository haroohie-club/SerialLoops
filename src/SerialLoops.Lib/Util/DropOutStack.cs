using System.Collections.Generic;
using System.Linq;

namespace SerialLoops.Lib.Util;

public class DropOutStack<T>(int capacity) where T : class
{
    public int Capacity { get; set; } = capacity;
    private readonly List<T> _stack = [];

    public void Push(T item)
    {
        _stack.Add(item);
    }

    public T Pop()
    {
        T last = Peek();
        if (last is not null)
        {
            _stack.RemoveAt(_stack.Count - 1);
        }
        return last;
    }

    public T Peek()
    {
        return _stack.Count == 0 ? null : _stack[^1];
    }
}
