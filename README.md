# LinqFramer

LinqFramer is a basic dataframe library written in C#. It allows for easy manipulation and querying of tabular data. The library provides a simple API for creating and working with data frames, supporting operations such as getting and setting values, transposing data, and finding specific observations.

## Features

- Create data frames from rows, columns, or records
- Access and manipulate data using row and column keys
- Transpose data frames
- Find observations based on values or predicates
- Print data frames in a human-readable format

## Installation

To use LinqFramer in your project, clone the repository and include the `LinqFramer` namespace in your C# files.

```bash
git clone https://github.com/JacksonMeade/LinqFramer.git
```

## API Reference

### Frame Class

- `Frame<RowKey, ColKey>()`: Initializes a new empty frame.
- `this[RowKey row, ColKey col]`: Gets or sets the value at the specified row and column.
- `this[RowKey row]`: Gets the series for the specified row.
- `this[ColKey col]`: Gets the series for the specified column.
- `this[IEnumerable<ColKey> colKeys]`: Gets the frame for the specified column keys.
- `this[RowKey rowKey, IEnumerable<ColKey> colKeys]`: Gets the series for the specified row key and column keys.
- `this[IEnumerable<RowKey> rowKeys]`: Gets the frame for the specified row keys.
- `this[ColKey colKey, IEnumerable<RowKey> rowKeys]`: Gets the series for the specified column key and row keys.
- `Find<TObservation>(TObservation value)`: Finds the row and column keys for the specified value.
- `Find<TObservation>(Func<TObservation?, bool> predicate)`: Finds the row and column keys that match the specified predicate.
- `TryFind<TObservation>(TObservation value, out (RowKey, ColKey) result)`: Tries to find the row and column keys for the specified value.
- `TryFind<TObservation>(Func<TObservation?, bool> predicate, out (RowKey, ColKey) result)`: Tries to find the row and column keys that match the specified predicate.
- `Shape`: Gets the shape of the frame as a tuple (number of rows, number of columns).
- `ToString()`: Returns a string representation of the frame.

### FrameExtensions Class

- `Transpose<TRow, TCol>(this Dictionary<TRow, Dictionary<TCol, object?>> dictionary)`: Transposes the given dictionary.

## Contributing

Contributions are welcome! Please open an issue or submit a pull request on GitHub.

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/JacksonMeade/c-sharp-dataframe/blob/main/LICENSE) file for details.

## Acknowledgements

Inspired by various data manipulation libraries in other programming languages. Special thanks to the open-source community for their contributions and support.

---

Happy coding!
