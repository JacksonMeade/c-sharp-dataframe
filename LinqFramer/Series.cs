using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinqFramer;

public class Series<Index, ObservationType> : IDictionary<Index, ObservationType> where Index : notnull
{
    /// <summary>
    /// The underlying dictionary that stores the series.
    /// </summary>
    protected readonly Dictionary<Index, ObservationType> _basis;

    private bool _isHorizontal;

    public Series(bool isHorizontal = true)
    {
        _basis = [];
        _isHorizontal = isHorizontal;
    }

    public Series(IEnumerable<KeyValuePair<Index, ObservationType>> collection, bool isHorizontal = true)
    {
        _basis = new Dictionary<Index, ObservationType>(collection);
        _isHorizontal = isHorizontal;
    }
    
    /// <summary>
    /// An overridable method that retrieves an observation from the series.
    /// Allows further customization of the getter method used in indexing.
    /// </summary>
    protected virtual ObservationType Getter(Index index)
    {
        return _basis[index];
    }

    /// <summary>
    /// An overridable method that sets an observation in the series.
    /// Allows further customization of the setter method used in indexing.
    /// </summary>
    protected virtual void Setter(Index index, ObservationType value)
    {
        _basis[index] = value;
    }

    /// <summary>
    /// Access the series by index.
    /// </summary>
    public ObservationType this[Index index]
    {
        get => Getter(index);
        set => Setter(index, value);
    }

    /// <summary>
    /// Access a set of observations by index.
    /// </summary>
    public Series<Index, ObservationType> this[IEnumerable<Index> indices]
    {
        get => indices.Select(index => (index, Getter(index))).ToDictionary();
        set
        {
            foreach (var index in indices)
            {
                Setter(index, value[index]);
            }
        }
    }


    public static implicit operator Series<Index, ObservationType>(Dictionary<Index, ObservationType> dictionary)
    {
        return new Series<Index, ObservationType>(dictionary);
    }

    public virtual (int x, int y) Shape
    {
        get
        {
            bool multiDim = typeof(ObservationType) is ICollection;
            int altDim = multiDim ? (_basis.Values.MaxBy(entry => (entry as ICollection)?.Count ?? 0) as ICollection)?.Count ?? 1 : 1;

            return _isHorizontal ? (Count, altDim) : (altDim, Count);
        }
    }

    #region Where
    /// <summary>
    /// Get all keys in the series that satisfy a predicate.
    /// </summary>
    public IEnumerable<Index> this[Func<ObservationType, bool> predicate] => _basis.Where(kvp => predicate(kvp.Value)).Select(kvp => kvp.Key);

    /// <summary>
    /// Get all keys and values in the series that satisfy a predicate on keys.
    /// </summary>
    public Series<Index, ObservationType> Where(Func<Index, bool> predicate)
    {
        var newSeries = new Series<Index, ObservationType>();
        foreach (var (index, observation) in this)
        {
            if (predicate(index))
            {
                newSeries.Add(index, observation);
            }
        }
        return newSeries;
    }

    /// <summary>
    /// Get all the keys and values in the series that satisfy a predicate on values.
    /// </summary>
    public Series<Index, ObservationType> Where(Func<ObservationType, bool> predicate)
    {
        var newSeries = new Series<Index, ObservationType>();
        foreach (var (index, observation) in this)
        {
            if (predicate(observation))
            {
                newSeries.Add(index, observation);
            }
        }
        return newSeries;
    }

    /// <summary>
    /// Get all the keys and values in the series that satisfy a predicate on keys and values.
    /// </summary>
    public Series<Index, ObservationType> Where(Func<Index, ObservationType, bool> predicate)
    {
        var newSeries = new Series<Index, ObservationType>();
        foreach (var (index, observation) in this)
        {
            if (predicate(index, observation))
            {
                newSeries.Add(index, observation);
            }
        }
        return newSeries;
    }

    #endregion

    #region IndexWith
    /// <summary>
    /// Index the series with a new type.
    /// </summary>
    public Series<NewIndex, ObservationType> IndexWith<NewIndex>() where NewIndex : notnull
    {
        var newSeries = new Series<NewIndex, ObservationType>();
        foreach (var (index, observation) in this)
        {
            newSeries.Add((NewIndex)Convert.ChangeType(index, typeof(NewIndex)), observation);
        }
        return newSeries;
    }

    /// <summary>
    /// Index the series with a new type using a custom mapping function.
    /// </summary>
    public Series<NewIndex, ObservationType> IndexWith<NewIndex>(Func<Index, NewIndex> indexSelector) where NewIndex : notnull
    {
        var newSeries = new Series<NewIndex, ObservationType>();
        foreach (var (index, observation) in this)
        {
            newSeries.Add(indexSelector(index), observation);
        }
        return newSeries;
    }

    /// <summary>
    /// Index the series with a new type using a collection of new indices who have an Equals method to the original index type.
    /// </summary>
    public Series<NewIndex, ObservationType> IndexWith<NewIndex>(ICollection<NewIndex> newIndices) where NewIndex : notnull, IEquatable<Index>
    {
        if (newIndices.Count != Count)
        {
            throw new ArgumentException("The number of new indices must match the number of observations in the series.");
        }

        var newSeries = new Series<NewIndex, ObservationType>();
        foreach (var (index, observation) in this)
        {
            newSeries.Add(newIndices.First(i => i.Equals(index)), observation);
        }
        return newSeries;
    }
    #endregion

    #region Functional Application
    /// <summary>
    /// Get a new series by applying a function between each observation in this series and another series.
    /// </summary>
    public Series<Index, ObservationType> Apply(Series<Index, ObservationType> other, Func<ObservationType, ObservationType, ObservationType> predicate)
    {
        var newSeries = new Series<Index, ObservationType>();
        foreach (var (index, observation) in this)
        {
            var otherObservation = other[index];
            newSeries.Add(index, predicate(observation, otherObservation));
        }
        return newSeries;
    }

    /// <summary>
    /// Get a new series whose observations are a cast of the original observations.
    /// </summary>
    public Series<Index, NewType?> As<NewType>()
    {
        var newSeries = new Series<Index, NewType?>();
        foreach (var (index, observation) in this)
        {
            newSeries.Add(index, (NewType?)Convert.ChangeType(observation, typeof(NewType)));
        }
        return newSeries;
    }

    /// <summary>
    /// Get a new series whose observations are a cast of the original observations using a custom selector function.
    /// </summary>
    public Series<Index, NewType?> As<NewType>(Func<ObservationType, NewType> selector)
    {
        var newSeries = new Series<Index, NewType?>();
        foreach (var (index, observation) in this)
        {
            newSeries.Add(index, selector(observation));
        }
        return newSeries;
    }
    #endregion

    string COL_SEPARATOR = " : ";
    string ENTRY_SEPARATOR = " => ";
    public override string ToString()
    {
        StringBuilder sb = new();

        sb.Append($"Series[{typeof(Index).Name} ({Shape.x}) x {typeof(ObservationType).Name} ({Shape.y})] \n");

        // Determine the maximum lengths of the index and value strings
        int maxIntIndexLength = Count.ToString().Length;
        int maxIndexLength = Keys.Max(k => k.ToString()?.Length ?? 0);
        int maxValueLength = Values.Max(v => v?.ToString()?.Length ?? 0);

        int i = 0;
        foreach (var kvp in this)
        {
            string indexString = (kvp.Key.ToString() ?? "null").PadRight(maxIndexLength);
            string valueString = (kvp.Value?.ToString() ?? "null").PadRight(maxValueLength);
            string intString = (i++).ToString().PadRight(maxIntIndexLength);
            sb.AppendLine($"{intString}{COL_SEPARATOR}{indexString}{ENTRY_SEPARATOR}{valueString}");
        }

        return sb.ToString();
    }

    #region IDictionary<Index, ObservationType> implementation

    public ICollection<Index> Keys => ((IDictionary<Index, ObservationType>)_basis).Keys;

    public ICollection<ObservationType> Values => ((IDictionary<Index, ObservationType>)_basis).Values;

    public int Count => ((ICollection<KeyValuePair<Index, ObservationType>>)_basis).Count;

    public bool IsReadOnly => ((ICollection<KeyValuePair<Index, ObservationType>>)_basis).IsReadOnly;

    public void Add(Index key, ObservationType value)
    {
        ((IDictionary<Index, ObservationType>)_basis).Add(key, value);
    }

    public void Add(KeyValuePair<Index, ObservationType> item)
    {
        ((ICollection<KeyValuePair<Index, ObservationType>>)_basis).Add(item);
    }

    public void Clear()
    {
        ((ICollection<KeyValuePair<Index, ObservationType>>)_basis).Clear();
    }

    public bool Contains(KeyValuePair<Index, ObservationType> item)
    {
        return ((ICollection<KeyValuePair<Index, ObservationType>>)_basis).Contains(item);
    }

    public bool ContainsKey(Index key)
    {
        return ((IDictionary<Index, ObservationType>)_basis).ContainsKey(key);
    }

    public void CopyTo(KeyValuePair<Index, ObservationType>[] array, int arrayIndex)
    {
        ((ICollection<KeyValuePair<Index, ObservationType>>)_basis).CopyTo(array, arrayIndex);
    }

    public IEnumerator<KeyValuePair<Index, ObservationType>> GetEnumerator()
    {
        return ((IEnumerable<KeyValuePair<Index, ObservationType>>)_basis).GetEnumerator();
    }

    public bool Remove(Index key)
    {
        return ((IDictionary<Index, ObservationType>)_basis).Remove(key);
    }

    public bool Remove(KeyValuePair<Index, ObservationType> item)
    {
        return ((ICollection<KeyValuePair<Index, ObservationType>>)_basis).Remove(item);
    }

    public bool TryGetValue(Index key, [MaybeNullWhen(false)] out ObservationType value)
    {
        return ((IDictionary<Index, ObservationType>)_basis).TryGetValue(key, out value);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_basis).GetEnumerator();
    }
    #endregion
}

public class Series<ObservationType> : Series<int, ObservationType> where ObservationType : IConvertible, IEquatable<ObservationType>
{

    public Series(ICollection<ObservationType> observations) : base()
    {
        for (int i = 0; i < observations.Count; i++)
        {
            _basis.Add(i, observations.ElementAt(i));
        }
    }
}