using System;
using System.Collections.Generic;

namespace Twice.Models.Columns
{
	internal interface IColumnDefinitionList
	{
		event EventHandler ColumnsChanged;

		void AddColumns( IEnumerable<ColumnDefinition> newColumns );

		IEnumerable<ColumnDefinition> Load();

		void Remove( IEnumerable<ColumnDefinition> columnDefinitions );

		void Save( IEnumerable<ColumnDefinition> definitions );

		void Update( IEnumerable<ColumnDefinition> definitions );
	}
}