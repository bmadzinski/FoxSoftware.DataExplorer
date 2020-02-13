using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace FoxSoftware.DataExplorer.Sql.Extensions
{
	static class SqlParameterCollectionExtensions
	{
		public static SqlParameterCollection AddWithValue(this SqlParameterCollection sqlParameters, IDictionary<string, object> parameters)
		{
			foreach (var item in parameters)
			{
				sqlParameters.AddWithValue($"@{item.Key}", item.Value);
			}
			return sqlParameters;
		}
		public static SqlParameterCollection AddWithValue(this SqlParameterCollection sqlParameters, IEnumerable<string> columns, object[,] values)
		{
			var columnList = columns.Select((x, i) => new { column = x, columnIndex = i }).ToList();

			columnList.ForEach((x) =>
			{
				for (int row = 0; row < values.GetLength(0); row++)
				{
					sqlParameters.AddWithValue($"@{x.column}{row}", values[row, x.columnIndex]);
				}
			});
			return sqlParameters;
		}
	}
}
