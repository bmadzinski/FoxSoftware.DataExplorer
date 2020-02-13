using System.Collections.Generic;

namespace FoxSoftware.DataExplorer.Interfaces
{
	public interface IDataInsertGenerator<TInsertModel>
	{
		TInsertModel AddRow(string tableName, IDictionary<string, object> row);
		TInsertModel AddRows(string tableName, IEnumerable<string> columns, object[,] values);
	}
}
