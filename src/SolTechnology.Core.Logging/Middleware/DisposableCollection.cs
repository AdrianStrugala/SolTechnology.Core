using System.Collections;

namespace SolTechnology.Core.Logging.Middleware;

public class DisposableCollection : ICollection<IDisposable>, IDisposable
{
    private readonly List<IDisposable> _internalDisposables = new();
    public bool Remove(IDisposable item)
    {
        return _internalDisposables.Remove(item);
    }

    public int Count => _internalDisposables.Count;
    public bool IsReadOnly => false;

    IEnumerator<IDisposable> IEnumerable<IDisposable>.GetEnumerator()
    {
        return _internalDisposables.GetEnumerator();
    }

    public IEnumerator GetEnumerator()
    {
        return _internalDisposables.GetEnumerator();
    }

    public bool Contains(IDisposable item)
    {
        return _internalDisposables.Contains(item);
    }

    public void CopyTo(IDisposable[] array, int index)
    {
        _internalDisposables.CopyTo(array, index);
    }

    public void Dispose()
    {
        _internalDisposables.ForEach(x => x.Dispose());
    }

    public void Add(IDisposable toAdd)
    {
        _internalDisposables.Add(toAdd);
    }

    public void Clear()
    {
        _internalDisposables.Clear();
    }
}