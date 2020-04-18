using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace FoxSoftware.DataExplorer.Sql.Extensions
{
	static class SqlParameterCollectionExtensions
	{
		public static SqlParameterCollection AddWithValueNullSecured(this SqlParameterCollection sqlParameters, IDictionary<string, object> parameters)
		{
			foreach (var item in parameters)
			{
				sqlParameters.AddWithValueNullSecured($"@{item.Key}", item.Value);
			}
			return sqlParameters;
		}
		public static SqlParameterCollection AddWithValueNullSecured(this SqlParameterCollection sqlParameters, IEnumerable<string> columns, object[,] values)
		{
			var columnList = columns.Select((x, i) => new { column = x, columnIndex = i }).ToList();

			columnList.ForEach((x) =>
			{
				for (int row = 0; row < values.GetLength(0); row++)
				{
					sqlParameters.AddWithValueNullSecured($"@{x.column}{row}", values[row, x.columnIndex]);
				}
			});
			return sqlParameters;
		}

		public static SqlParameterCollection AddWithValueNullSecured(this SqlParameterCollection sqlParameters, string parameterName, object value)
		{
			if(value is null)
			{
				value = DBNull.Value;
			}
			sqlParameters.AddWithValue(parameterName, value);
			return sqlParameters;
		}
	}
}
