using FoxSoftware.DataExplorer.Models;
using System.Collections.Generic;

namespace FoxSoftware.DataExplorer.Interfaces
{
	public interface IDataQueryGenerator<TQueryModel>
	{
		TQueryModel GetRow(string tableName, IDictionary<string, object> primaryKeyValues);
		TQueryModel Query(string tableName, IEnumerable<OrderData> orderColumns, IEnumerable<ConditionData> conditions, int? skip = null, int? take = null);
	}
}
