using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace IntentEngine
{
    public static class DataTableExtensions
    {
        public static IEnumerable<DataRow> AsEnumerable(this DataTable table)
        {
            if (table == null) yield break;
            for (int i = 0; i < table.Rows.Count; i++)
                yield return table.Rows[i];
        }

        public static List<T> ToList<T>(this DataTable table, Func<DataRow, T> mapper)
        {
            var list = new List<T>();
            if (table == null) return list;
            for (int i = 0; i < table.Rows.Count; i++)
                list.Add(mapper(table.Rows[i]));
            return list;
        }
    }
}
