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
                if (rowIndex == -1)
                {
                    rowMap.Add(weight);
                    table.AddRow(columns);
                }
                else if (rowMap[rowIndex] == weight)
                {
                    // update
                    table.UpdateRow(rowIndex, columns);
                }
                else
                {
                    // insert
                    rowMap.Insert(rowIndex, weight);
                    table.InsertRow(rowIndex, columns);
                }

                if (rowMap.Count != table.Rows.Count) throw new InvalidOperationException($"Row mismatch: rowMap: {rowMap.Count}, table: {table.Rows.Count}");
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

        /// <inheritdoc cref="UpdateWeighted(Table, int, int, IRenderable)"/>
        public static Task UpdateWeightedRow(this Table table, int weight, int columnIndex, string value)
            => UpdateWeightedRow(table, weight, columnIndex, new Markup(value));

        /// <summary>
        /// Update a row based on its weight. The row must have been added with
        /// <see cref="AddOrUpdateWeightedRow"/> for this to work.
        /// </summary>
        /// <param name="table">Table to update</param>
        /// <param name="weight">Weight of row to update. Throws an exception if not found</param>
        /// <param name="columnIndex">Column to update</param>
        /// <param name="value">New value</param>
        public static async Task UpdateWeightedRow(this Table table, int weight, int columnIndex, IRenderable value)
        {
            var rowMap = Mappings.GetOrAdd(table, _ => new List<int>());

            using (await _lock.ObtainLock())
            {
                var rowIndex = rowMap.FindIndex(x => x == weight);
                if (rowIndex == -1) throw new ArgumentOutOfRangeException("weight not found");

                table.UpdateCell(rowIndex, columnIndex, value);
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
    }
}
