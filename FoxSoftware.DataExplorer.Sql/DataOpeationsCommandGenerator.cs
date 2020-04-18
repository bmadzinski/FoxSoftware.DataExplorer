using FoxSoftware.DataExplorer.Interfaces;
using FoxSoftware.DataExplorer.Models;
using FoxSoftware.DataExplorer.Sql.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace FoxSoftware.DataExplorer.Sql
{
	public class DataOpeationsCommandGenerator : IDataQueryGenerator<SqlCommand>, IDataInsertGenerator<SqlCommand>
	{
		public SqlCommand GetRow(string tableName, IDictionary<string, object> primaryKeyValues)
		{
			string commandScript = BuildGetRowCommandScript(tableName, primaryKeyValues);

			var command = new SqlCommand(commandScript);
			command.Parameters.AddWithValueNullSecured(primaryKeyValues);

			return command;
		}

		public SqlCommand AddRow(string tableName, IDictionary<string, object> row)
		{
			string script = BuildAddRowCommandScript(tableName, row);

			var command = new SqlCommand(script);
			command.Parameters.AddWithValueNullSecured(row);

			return command;
		}
		public SqlCommand AddRows(string tableName, IEnumerable<string> columns, object[,] values)
		{
			string script = BuildAddRowsCommandScript(tableName, columns, values.GetLength(0));

			var command = new SqlCommand(script);
			command.Parameters.AddWithValueNullSecured(columns, values);

			return command;
		}

		public SqlCommand Query(string tableName, IEnumerable<OrderData> orderColumns, IEnumerable<ConditionData> conditions, int? skip = null, int? take = null)
		{
			string script = BuildQueryCommandScript(tableName, orderColumns, conditions, skip, take);

			var command = new SqlCommand(script);

			ParameterizeQueryCommand(conditions, skip, take, command);

			return command;
		}

		protected static void ParameterizeQueryCommand(IEnumerable<ConditionData> conditions, int? skip, int? take, SqlCommand command)
		{
			if (skip.HasValue || take.HasValue)
			{
				command.Parameters.AddWithValueNullSecured("@skip", skip ?? 0);
			}

			if (take.HasValue)
			{
				command.Parameters.AddWithValueNullSecured("@take", take.Value);
			}

			var splited = conditions
				.Select((x, i) => new { i, x.Value, x.Condition })
				.ToLookup(x => x.Condition == Condition.In);

			command.Parameters.AddWithValueNullSecured(
				splited[true].SelectMany(conditionData =>
				{
					var array = ObjectToEnumerable(conditionData.Value);
					return array.Select((x, i) => new { param = $"value{conditionData.i}_{i}", value = x });
				}).ToDictionary(x => x.param, x => x.value)
			);
			command.Parameters.AddWithValueNullSecured(
				splited[false].ToDictionary((x) => $"value{x.i}", (x) => x.Value)
			);
		}

		protected static string BuildQueryCommandScript(string tableName, IEnumerable<OrderData> orderColumns, IEnumerable<ConditionData> conditions, int? skip, int? take)
		{
			var queryScirptBuilder = new StringBuilder();
			queryScirptBuilder.AppendLine($"SELECT * FROM [{tableName}]");

			if (conditions.Any())
			{
				queryScirptBuilder
					.AppendLine("WHERE")
					.AppendLine(string.Join(" AND\n",
						conditions.Select((x, i) =>
						{
							var conditionOperator = GetConditionOperator(x.Condition);
							var negationText = x.Negate ? "NOT " : "";
							var valueParameter = GetConditionParameter(i, x.Value, x.Condition);

							return $"\t{negationText}[{x.Column}] {conditionOperator} {valueParameter}";
						})
					));
			}

			if (orderColumns.Any())
			{
				queryScirptBuilder
					.AppendLine("ORDER BY")
					.AppendLine(string.Join(",\n",
						orderColumns.Select(x =>
						{
							var orderDirection = x.Descending ? "DESC" : "ASC";
							return $"\t[{x.ColumnName}] {orderDirection}";
						})
					));
			}

			if (skip.HasValue || take.HasValue)
			{
				queryScirptBuilder.AppendLine($"OFFSET @skip ROWS");
			}
			if (take.HasValue)
			{
				queryScirptBuilder.AppendLine($"FETCH NEXT @take ROWS ONLY");
			}
			queryScirptBuilder.AppendLine();
			var script = queryScirptBuilder.ToString();
			return script;
		}

		protected static object GetConditionParameter(int i, object value, Condition condition)
		{
			if (condition != Condition.In)
			{
				return $"@value{i}";
			}

			var count = ObjectToEnumerable(value).Count();

			var conditionParams = Enumerable.Range(0, count).Select(x => $"@value{i}_{x}");
			var joined = string.Join(", ", conditionParams);
			return $"({joined})";
		}

		protected static IEnumerable<object> ObjectToEnumerable(object value)
		{
			var enumerable = (IEnumerator)value.GetType().GetMethod(nameof(IEnumerable.GetEnumerator)).Invoke(value, null);
			while (enumerable.MoveNext())
			{
				yield return enumerable.Current;
			}
		}

		protected static string GetConditionOperator(Condition condition)
		{
			switch (condition)
			{
				case Condition.Equal:
					return "=";
				case Condition.NotEqual:
					return "<>";
				case Condition.Greater:
					return ">";
				case Condition.GreaterOrEqual:
					return ">=";
				case Condition.Less:
					return "<";
				case Condition.LessOrEqual:
					return "<=";
				case Condition.Like:
					return "LIKE";
				case Condition.In:
					return "IN";
				default:
					throw new NotSupportedException($"Condition not supported: {condition}");
			}
		}

		protected static string BuildAddRowCommandScript(string tableName, IDictionary<string, object> columnValues)
		{
			var addRowBuilder = new StringBuilder();

			var columnsNames = string.Join(", ", columnValues.Keys.Select(x => $"[{x}]"));
			var columnsValuesParametersNames = string.Join(", ", columnValues.Keys.Select(x => $"@{x}"));
			addRowBuilder
				.AppendLine($"INSERT INTO [{tableName}] ({columnsNames})")
				.AppendLine($"VALUES ({columnsValuesParametersNames});");

			var script = addRowBuilder.ToString();
			return script;
		}
		protected static string BuildAddRowsCommandScript(string tableName, IEnumerable<string> columns, int numberOfRows)
		{
			var addRowsBuilder = new StringBuilder();

			var columnsNames = string.Join(", ", columns.Select(x => $"[{x}]"));
			Func<int, string> columnsValuesParametersNames = (int i) => string.Join(", ", columns.Select(x => $"@{x}{i}"));

			addRowsBuilder
				.AppendLine($"INSERT INTO [{tableName}] ({columnsNames})")
				.AppendLine("VALUES");

			for (int i = 0; i < numberOfRows; i++)
			{
				addRowsBuilder.Append($"\t({columnsValuesParametersNames(i)})");
				if (i < numberOfRows - 1)
				{
					addRowsBuilder.AppendLine(",");
				}
				else
				{
					addRowsBuilder.AppendLine(";");
				}
			}

			var script = addRowsBuilder.ToString();
			return script;
		}

		protected static string BuildGetRowCommandScript(string tableName, IDictionary<string, object> primayKeyValues)
		{
			var rowQueryBuilder = new StringBuilder();
			rowQueryBuilder.AppendLine($"SELECT TOP 1 *\nFROM [{tableName}]\nWHERE ");

			rowQueryBuilder.Append(string.Join(
				" AND\n",
				primayKeyValues
					.Select(x => $"\t[{x.Key}] = @{x.Key}")
			));
			var commandScript = rowQueryBuilder.ToString();
			return commandScript;
		}
	}
}
