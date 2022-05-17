using Spectre.Console;
using Spectre.Console.Rendering;
using System.Collections.Concurrent;
using TraceRtLive.Helpers;

namespace TraceRtLive.UI
{
    public static class WeightedTableExtensions
    {
        private static ConcurrentDictionary<Table, List<int>> Mappings { get; } = new ConcurrentDictionary<Table, List<int>>();

        private static AsyncLock _lock = new AsyncLock();

        /// <inheritdoc cref="AddOrUpdateWeightedRow(Table, int, IEnumerable{IRenderable})"/>
        public static Task AddOrUpdateWeightedRow(this Table table, int weight, params string[] columns)
            => AddOrUpdateWeightedRow(table, weight, columns.Select(column => new Markup(column)));

        /// <summary>
        /// <para>
        /// Add or updates a row with a specified <paramref name="weight"/> value.
        /// If the weight exists, the row will be updated, otherwise it will be added.
        /// Added rows are sorted in the table with other rows based on their
        /// weights.
        /// </para>
        /// <para>
        /// The table rows must only be added/removed using the <see cref="WeightedTableExtensions"/>
        /// methods for this to work.
        /// </para>
        /// </summary>
        /// <param name="table">Table to update</param>
        /// <param name="weight">Weight of the row to add, which determines its placement</param>
        /// <param name="columns">Column data to add</param>
        public static async Task AddOrUpdateWeightedRow(this Table table, int weight, IEnumerable<IRenderable> columns)
        {
            var rowMap = Mappings.GetOrAdd(table, _ => new List<int>());

            using (await _lock.ObtainLock())
            {
                var rowIndex = rowMap.FindIndex(x => x >= weight);
                if (rowMap[rowIndex] == weight)
                {
                    // update
                    table.UpdateRow(rowIndex, columns);
                }
                else
                {
                    AddOrInsertRow(table, rowMap, weight, rowIndex, columns);
                }

                if (rowMap.Count != table.Rows.Count) throw new InvalidOperationException($"Row mismatch: rowMap: {rowMap.Count}, table: {table.Rows.Count}");
            }
        }

        /// <inheritdoc cref="UpdateWeightedCells(Table, int, IEnumerable{(int columnIndex, IRenderable value)})"/>
        public static Task UpdateWeightedCells(this Table table, int weight, IEnumerable<(int columnIndex, string value)> cells)
            => UpdateWeightedCells(table, weight, ToRenderable(cells));

        /// <summary>
        /// Update a row based on its weight. If the row doesn't exist it will be ignored.
        /// </summary>
        /// <param name="table">Table to update</param>
        /// <param name="weight">Weight of row to update. Throws an exception if not found</param>
        /// <param name="cells">Cells and indexes to udpate</param>
        public static Task UpdateWeightedCells(this Table table, int weight, IEnumerable<(int columnIndex, IRenderable value)> cells)
            => UpdateWeightedCells(table, weight, allowAdd: false, cells);


        /// <inheritdoc cref="AddOrUpdateWeightedCells(Table, int, IEnumerable{(int columnIndex, IRenderable value)})"/>
        public static Task AddOrUpdateWeightedCells(this Table table, int weight, IEnumerable<(int columnIndex, string value)> cells)
            => AddOrUpdateWeightedCells(table, weight, ToRenderable(cells));

        /// <summary>
        /// Update a row based on its weight. If the row doesn't exist it will be added.
        /// </summary>
        /// <param name="table">Table to update</param>
        /// <param name="weight">Weight of row to update. Throws an exception if not found</param>
        /// <param name="cells">Cells and indexes to udpate</param>
        public static Task AddOrUpdateWeightedCells(this Table table, int weight, IEnumerable<(int columnIndex, IRenderable value)> cells)
            => UpdateWeightedCells(table, weight, allowAdd: true, cells);

        private static async Task UpdateWeightedCells(this Table table, int weight, bool allowAdd, IEnumerable<(int columnIndex, IRenderable value)> cells)
        {
            var rowMap = Mappings.GetOrAdd(table, _ => new List<int>());

            using (await _lock.ObtainLock())
            {
                var rowIndex = rowMap.FindIndex(x => x >= weight);
                if (rowIndex >= 0 && rowMap[rowIndex] == weight)
                {
                    // row exists: update cells
                    foreach (var cell in cells)
                    {
                        table.UpdateCell(rowIndex, cell.columnIndex, cell.value);
                    }
                }
                else if (allowAdd)
                {
                    // row doesn't exist: fill in full row, and use rowIndex to add or insert
                    var row = FillBlankColumns(cells, table.Columns.Count);
                    AddOrInsertRow(table, rowMap, weight, rowIndex, row);
                }
            }
        }

        /// <summary>
        /// Remove a row based on its weight. The row must have been added with
        /// <see cref="AddWeightedRow"/> for this to work.
        /// </summary>
        /// <param name="table">Table to update</param>
        /// <param name="weight">Weight of row to remove. Throws an exception if not found</param>
        public static async Task RemoveWeightedRow(this Table table, int weight)
        {
            var rowMap = Mappings.GetOrAdd(table, _ => new List<int>());

            using (await _lock.ObtainLock())
            {
                if (rowMap.Count != table.Rows.Count) throw new InvalidOperationException($"Row mismatch: rowMap: {rowMap.Count}, table: {table.Rows.Count}");

                var rowIndex = rowMap.FindIndex(x => x == weight);
                if (rowIndex == -1) throw new ArgumentOutOfRangeException("weight not found");

                rowMap.RemoveAt(rowIndex);
                table.RemoveRow(rowIndex);
            }
        }

        /// <summary>
        /// Updates all cells in a given row
        /// </summary>
        /// <param name="table"></param>
        /// <param name="rowIndex"></param>
        /// <param name="columns"></param>
        private static void UpdateRow(this Table table, int rowIndex, IEnumerable<IRenderable> columns)
        {
            var colIndex = 0;
            foreach (var column in columns)
            {
                table.UpdateCell(rowIndex, colIndex++, column);
            }
        }


        /// <summary>
        /// Adds (or inserts) a row of given <paramref name="weight"/>.
        /// Adds/Inserts to <paramref name="table"/> and <paramref name="rowMap"/> at the same time.
        /// </summary>
        /// <param name="table">Actual table</param>
        /// <param name="rowMap">Mapping of indexes to weights</param>
        /// <param name="weight">Weight</param>
        /// <param name="rowIndex">Index to insert. -1 for adding to end.</param>
        /// <param name="columns">Columns to add</param>
        private static void AddOrInsertRow(Table table, List<int> rowMap, int weight, int rowIndex, IEnumerable<IRenderable> columns)
        {
            if (rowIndex == -1)
            {
                rowMap.Add(weight);
                table.AddRow(columns);
            }
            else
            {
                // insert
                rowMap.Insert(rowIndex, weight);
                table.InsertRow(rowIndex, columns);
            }
        }

        private static IEnumerable<(int columnIndex, IRenderable value)> ToRenderable(IEnumerable<(int columnIndex, string value)> values)
            => values.Select(x => (x.columnIndex, (IRenderable)new Markup(x.value)));

        public static IEnumerable<IRenderable> FillBlankColumns(IEnumerable<(int columnIndex, string value)> columnValues, int numColumns)
            => FillBlankColumns(ToRenderable(columnValues), numColumns);

        public static IEnumerable<IRenderable> FillBlankColumns(IEnumerable<(int columnIndex, IRenderable value)> columnValues, int numColumns)
        {
            //TODO: could optimize this more
            var actuals = columnValues.ToDictionary(k => k.columnIndex, v => v.value);
            for (var i = 0; i< numColumns; i++)
            {
                yield return actuals.TryGetValue(i, out var value) ? value : Text.Empty;
            }
        }
    }
}
