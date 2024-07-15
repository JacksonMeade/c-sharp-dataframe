using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LinqFramer;

/// <summary>
/// Represents a series of frames with an index and observation type.
/// </summary>
/// <typeparam name="Index">The type of the index.</typeparam>
/// <typeparam name="ObservationType">The type of the observation.</typeparam>
public class FrameSeries<Index, ObservationType> : Series<Index, ObservationType?>
    where Index : notnull
{
    private readonly Action<Index, ObservationType?>? _setter;

    /// <summary>
    /// Initializes a new instance of the FrameSeries class.
    /// </summary>
    /// <param name="setter">Optional action to set the value.</param>
    /// <param name="isRow">Indicates if the series represents a row.</param>
    internal FrameSeries(Action<Index, ObservationType?>? setter = null, bool isRow = true) : base(isRow)
    {
        _setter = setter;
    }

    /// <summary>
    /// Initializes a new instance of the FrameSeries class with a collection.
    /// </summary>
    /// <param name="collection">A collection of key-value pairs to initialize the series.</param>
    /// <param name="setter">Optional action to set the value.</param>
    /// <param name="isRow">Indicates if the series represents a row.</param>
    internal FrameSeries(IEnumerable<KeyValuePair<Index, ObservationType?>> collection, Action<Index, ObservationType?>? setter = null, bool isRow = true) : base(collection, isRow)
    {
        _setter = setter;
    }

    /// <summary>
    /// Sets the value for the specified index.
    /// </summary>
    /// <param name="index">The index to set the value for.</param>
    /// <param name="value">The value to set.</param>
    protected override void Setter(Index index, ObservationType? value)
    {
        base.Setter(index, value);
        if (_setter is not null)
        {
            _setter(index, value);
        }
    }
}

/// <summary>
/// Represents a data frame with rows and columns.
/// </summary>
/// <typeparam name="RowKey">The type of the row key.</typeparam>
/// <typeparam name="ColKey">The type of the column key.</typeparam>
public class Frame<RowKey, ColKey> where RowKey : notnull where ColKey : notnull
{
    private readonly Dictionary<RowKey, Dictionary<ColKey, object?>> _rowMajor;
    private readonly Dictionary<ColKey, Dictionary<RowKey, object?>> _colMajor;

    /// <summary>
    /// Initializes a new instance of the Frame class.
    /// </summary>
    public Frame()
    {
        _rowMajor = new Dictionary<RowKey, Dictionary<ColKey, object?>>();
        _colMajor = new Dictionary<ColKey, Dictionary<RowKey, object?>>();
    }

    private Frame(Dictionary<RowKey, Dictionary<ColKey, object?>> rowMajor) : this(rowMajor, rowMajor.Transpose()) { }

    private Frame(Dictionary<ColKey, Dictionary<RowKey, object?>> colMajor) : this(colMajor.Transpose(), colMajor) { }

    private Frame(Dictionary<RowKey, Dictionary<ColKey, object?>> rowMajor, Dictionary<ColKey, Dictionary<RowKey, object?>> colMajor)
    {
        _rowMajor = rowMajor;
        _colMajor = colMajor;
    }

    /// <summary>
    /// Gets or sets the value at the specified row and column.
    /// </summary>
    /// <param name="row">The row key.</param>
    /// <param name="col">The column key.</param>
    /// <returns>The value at the specified row and column.</returns>
    public object? this[RowKey row, ColKey col]
    {
        get
        {
            EnsureDictionaries(row, col);
            return _rowMajor[row][col];
        }
        set
        {
            EnsureDictionaries(row, col);
            _rowMajor[row][col] = value;
            _colMajor[col][row] = value;
        }
    }

    /// <summary>
    /// Gets the series for the specified row.
    /// </summary>
    /// <param name="row">The row key.</param>
    /// <returns>The series for the specified row.</returns>
    public FrameSeries<ColKey, object?> this[RowKey row]
    {
        get
        {
            return new FrameSeries<ColKey, object?>(_rowMajor[row], (col, value) => this[row, col] = value, false);
        }
    }

    /// <summary>
    /// Gets the series for the specified column.
    /// </summary>
    /// <param name="col">The column key.</param>
    /// <returns>The series for the specified column.</returns>
    public FrameSeries<RowKey, object?> this[ColKey col]
    {
        get
        {
            return new FrameSeries<RowKey, object?>(_colMajor[col], (row, value) => this[col, row] = value, true);
        }
    }

    /// <summary>
    /// Gets or sets the value at the specified column and row.
    /// </summary>
    /// <param name="col">The column key.</param>
    /// <param name="row">The row key.</param>
    /// <returns>The value at the specified column and row.</returns>
    public object? this[ColKey col, RowKey row]
    {
        get
        {
            EnsureDictionaries(row, col);
            return _colMajor[col][row];
        }
        set
        {
            EnsureDictionaries(row, col);
            _rowMajor[row][col] = value;
            _colMajor[col][row] = value;
        }
    }

    /// <summary>
    /// Gets the series of rows in the frame.
    /// </summary>
    public FrameSeries<RowKey, FrameSeries<ColKey, object>> Rows
    {
        get
        {
            var rows = new FrameSeries<RowKey, FrameSeries<ColKey, object>>();
            foreach (var (rowKey, row) in _rowMajor)
            {
                rows.Add(rowKey, new FrameSeries<ColKey, object>(row, (colKey, value) => this[rowKey, colKey] = value));
            }
            return rows;
        }
    }

    /// <summary>
    /// Gets the series of columns in the frame.
    /// </summary>
    public FrameSeries<ColKey, FrameSeries<RowKey, object>> Columns
    {
        get
        {
            var cols = new FrameSeries<ColKey, FrameSeries<RowKey, object>>();
            foreach (var (colKey, col) in _colMajor)
            {
                cols.Add(colKey, new FrameSeries<RowKey, object>(col, (rowKey, value) => this[colKey, rowKey] = value));
            }
            return cols;
        }
    }

    /// <summary>
    /// Gets the frame for the specified column keys.
    /// </summary>
    /// <param name="colKeys">The column keys.</param>
    /// <returns>The frame for the specified column keys.</returns>
    public Frame<RowKey, ColKey> this[IEnumerable<ColKey> colKeys]
    {
        get
        {
            return FromColumns(colKeys.Select(colKey => new KeyValuePair<ColKey, IEnumerable<KeyValuePair<RowKey, object?>>>(
                colKey,
                _colMajor[colKey]
            )));
        }
    }

    /// <summary>
    /// Gets the series for the specified row key and column keys.
    /// </summary>
    /// <param name="rowKey">The row key.</param>
    /// <param name="colKeys">The column keys.</param>
    /// <returns>The series for the specified row key and column keys.</returns>
    public FrameSeries<ColKey, FrameSeries<RowKey, object?>> this[RowKey rowKey, IEnumerable<ColKey> colKeys]
    {
        get
        {
            var cols = new FrameSeries<ColKey, FrameSeries<RowKey, object?>>();
            foreach (var colKey in colKeys)
            {
                cols.Add(colKey, this[colKey]);
            }
            return cols;
        }
    }

    /// <summary>
    /// Gets the frame for the specified row keys.
    /// </summary>
    /// <param name="rowKeys">The row keys.</param>
    /// <returns>The frame for the specified row keys.</returns>
    public Frame<RowKey, ColKey> this[IEnumerable<RowKey> rowKeys]
    {
        get
        {
            return FromRows(rowKeys.Select(rowKey => new KeyValuePair<RowKey, IEnumerable<KeyValuePair<ColKey, object?>>>(
                rowKey,
                _rowMajor[rowKey]
            )));
        }
    }

    /// <summary>
    /// Gets the series for the specified column key and row keys.
    /// </summary>
    /// <param name="colKey">The column key.</param>
    /// <param name="rowKeys">The row keys.</param>
    /// <returns>The series for the specified column key and row keys.</returns>
    public FrameSeries<RowKey, FrameSeries<ColKey, object?>> this[ColKey colKey, IEnumerable<RowKey> rowKeys]
    {
        get
        {
            var rows = new FrameSeries<RowKey, FrameSeries<ColKey, object?>>();
            foreach (var rowKey in rowKeys)
            {
                rows.Add(rowKey, this[rowKey]);
            }
            return rows;
        }
    }

    /// <summary>
    /// Finds the row and column keys for the specified value.
    /// </summary>
    /// <typeparam name="TObservation">The type of the observation.</typeparam>
    /// <param name="value">The value to find.</param>
    /// <returns>The row and column keys for the specified value.</returns>
    public (RowKey, ColKey) Find<TObservation>(TObservation value) where TObservation : IEquatable<TObservation>
    {
        return Find<TObservation>(observation => observation?.Equals(value) ?? false);
    }

    /// <summary>
    /// Finds the row and column keys that match the specified predicate.
    /// </summary>
    /// <typeparam name="TObservation">The type of the observation.</typeparam>
    /// <param name="predicate">The predicate to match.</param>
    /// <returns>The row and column keys that match the specified predicate.</returns>
    public (RowKey, ColKey) Find<TObservation>(Func<TObservation?, bool> predicate)
    {
        foreach (var (rowKey, row) in _rowMajor)
        {
            foreach (var (colKey, observation) in row)
            {
                var ob = (TObservation?)observation;
                if (predicate((TObservation?)observation))
                {
                    return (rowKey, colKey);
                }
            }
        }
        throw new InvalidOperationException("No matching observation found.");
    }

    /// <summary>
    /// Tries to find the row and column keys for the specified value.
    /// </summary>
    /// <typeparam name="TObservation">The type of the observation.</typeparam>
    /// <param name="value">The value to find.</param>
    /// <param name="result">The result of the search.</param>
    /// <returns>True if the value is found, otherwise false.</returns>
    public bool TryFind<TObservation>(TObservation value, out (RowKey, ColKey) result) where TObservation : IEquatable<TObservation>
    {
        return TryFind<TObservation>(observation => observation?.Equals(value) ?? false, out result);
    }

    /// <summary>
    /// Tries to find the row and column keys that match the specified predicate.
    /// </summary>
    /// <typeparam name="TObservation">The type of the observation.</typeparam>
    /// <param name="predicate">The predicate to match.</param>
    /// <param name="result">The result of the search.</param>
    /// <returns>True if the predicate matches, otherwise false.</returns>
    public bool TryFind<TObservation>(Func<TObservation?, bool> predicate, out (RowKey, ColKey) result)
    {
        foreach (var (rowKey, row) in _rowMajor)
        {
            foreach (var (colKey, observation) in row)
            {
                if (predicate((TObservation?)observation))
                {
                    result = (rowKey, colKey);
                    return true;
                }
            }
        }
        result = default;
        return false;
    }

    /// <summary>
    /// Gets the shape of the frame as a tuple (number of rows, number of columns).
    /// </summary>
    public (int x, int y) Shape => (_rowMajor.Count, _colMajor.Count);

    private void EnsureDictionaries(RowKey rowKey, ColKey colKey)
    {
        if (!_rowMajor.ContainsKey(rowKey))
        {
            _rowMajor[rowKey] = new Dictionary<ColKey, object?>();
        }
        if (!_colMajor.ContainsKey(colKey))
        {
            _colMajor[colKey] = new Dictionary<RowKey, object?>();
        }

        _rowMajor[rowKey].TryAdd(colKey, null);
        _colMajor[colKey].TryAdd(rowKey, null);
    }

    /// <summary>
    /// Creates a frame from a collection of rows.
    /// </summary>
    /// <typeparam name="TRow">The type of the row key.</typeparam>
    /// <typeparam name="TCol">The type of the column key.</typeparam>
    /// <param name="rows">The collection of rows to initialize the frame.</param>
    /// <returns>The initialized frame.</returns>
    public static Frame<TRow, TCol> FromRows<TRow, TCol>(IEnumerable<KeyValuePair<TRow, IEnumerable<KeyValuePair<TCol, object?>>>> rows)
        where TRow : notnull
        where TCol : notnull
    {
        var rowMajor = new Dictionary<TRow, Dictionary<TCol, object?>>();

        foreach (var row in rows)
        {
            rowMajor[row.Key] = row.Value.ToDictionary();
        }

        return new Frame<TRow, TCol>(rowMajor);
    }

    /// <summary>
    /// Creates a frame from a collection of columns.
    /// </summary>
    /// <typeparam name="TRow">The type of the row key.</typeparam>
    /// <typeparam name="TCol">The type of the column key.</typeparam>
    /// <param name="cols">The collection of columns to initialize the frame.</param>
    /// <returns>The initialized frame.</returns>
    public static Frame<TRow, TCol> FromColumns<TRow, TCol>(IEnumerable<KeyValuePair<TCol, IEnumerable<KeyValuePair<TRow, object?>>>> cols)
        where TRow : notnull
        where TCol : notnull
    {
        var colMajor = new Dictionary<TCol, Dictionary<TRow, object?>>();

        foreach (var col in cols)
        {
            colMajor[col.Key] = col.Value.ToDictionary();
        }

        return new Frame<TRow, TCol>(colMajor);
    }

    /// <summary>
    /// Creates a frame from a collection of records.
    /// </summary>
    /// <typeparam name="TRow">The type of the row key.</typeparam>
    /// <typeparam name="TCol">The type of the column key.</typeparam>
    /// <typeparam name="TObservation">The type of the observation.</typeparam>
    /// <param name="records">The collection of records to initialize the frame.</param>
    /// <returns>The initialized frame.</returns>
    public static Frame<TRow, TCol> FromRecords<TRow, TCol, TObservation>(IEnumerable<Tuple<TRow, TCol, TObservation?>> records)
        where TRow : notnull
        where TCol : notnull
    {
        var rowMajor = new Dictionary<TRow, Dictionary<TCol, object?>>();

        foreach (var record in records)
        {
            if (!rowMajor.ContainsKey(record.Item1))
            {
                rowMajor[record.Item1] = new Dictionary<TCol, object?>();
            }
            rowMajor[record.Item1][record.Item2] = record.Item3;
        }

        return new Frame<TRow, TCol>(rowMajor);
    }

    string COL_SEPARATOR = " : ";
    /// <summary>
    /// Returns a string representation of the frame.
    /// </summary>
    /// <returns>A string representation of the frame.</returns>
    public override string ToString()
    {
        StringBuilder sb = new();

        sb.Append($"Frame[{typeof(RowKey).Name} ({Shape.x}) x {typeof(ColKey).Name} ({Shape.y})] \n");

        int maxIntIndexLength = _rowMajor.Count.ToString().Length;
        int maxRowIndexLength = Math.Max(_rowMajor.Keys.Max(key => key?.ToString()?.Length ?? 1), 4);
        int maxColIndexLength = Math.Max(_colMajor.Keys.Max(key => key?.ToString()?.Length ?? 1), 4);

        // Printing Headers
        Dictionary<ColKey, int> columnLengths = new();
        var blank = "".PadLeft(maxIntIndexLength);
        sb.Append(blank); sb.Append(COL_SEPARATOR);
        sb.Append("Key".PadRight(maxRowIndexLength)); sb.Append(COL_SEPARATOR);
        foreach (var col in _colMajor.Keys)
        {
            int max = col.GetType().Name.Length;
            foreach (var value in this[col])
            {
                max = Math.Max(max, value.Value?.ToString()?.Length ?? max);
            }
            columnLengths[col] = max;
            sb.Append($"{col.ToString()?.PadRight(max) ?? col.GetType().Name}{COL_SEPARATOR}");
        }
        sb.Append("\n");

        // Printing Rows
        int i = 0;

        foreach (var row in _rowMajor)
        {
            sb.Append(i++.ToString().PadLeft(maxIntIndexLength)); sb.Append(COL_SEPARATOR);
            sb.Append(row.Key.ToString()?.PadRight(maxRowIndexLength) ?? blank); sb.Append(COL_SEPARATOR);
            foreach (var col in _colMajor.Keys)
            {
                var value = this[row.Key, col];
                sb.Append(value?.ToString()?.PadRight(columnLengths[col]) ?? "".PadRight(columnLengths[col])); sb.Append(COL_SEPARATOR);
            }
            sb.Append("\n");
        }

        return sb.ToString();
    }
}

/// <summary>
/// Extension methods for the Frame class.
/// </summary>
public static class FrameExtensions
{
    /// <summary>
    /// Transposes the given dictionary.
    /// </summary>
    /// <typeparam name="TRow">The type of the row key.</typeparam>
    /// <typeparam name="TCol">The type of the column key.</typeparam>
    /// <param name="dictionary">The dictionary to transpose.</param>
    /// <returns>The transposed dictionary.</returns>
    public static Dictionary<TCol, Dictionary<TRow, object?>> Transpose<TRow, TCol>(this Dictionary<TRow, Dictionary<TCol, object?>> dictionary)
        where TRow : notnull
        where TCol : notnull
    {
        var colMajor = new Dictionary<TCol, Dictionary<TRow, object?>>();

        foreach (var row in dictionary)
        {
            foreach (var col in row.Value)
            {
                if (!colMajor.ContainsKey(col.Key))
                {
                    colMajor[col.Key] = new Dictionary<TRow, object?>();
                }
                colMajor[col.Key][row.Key] = col.Value;
            }
        }

        return colMajor;
    }
}
