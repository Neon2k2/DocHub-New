using DocHub.Core.Entities;

namespace DocHub.Core.Interfaces
{
    public interface IDynamicTableService
    {
        Task<string> CreateDynamicTableAsync(Guid letterTypeId, Guid excelUploadId, List<ColumnDefinition> columns, List<Dictionary<string, object>> data);
        Task<bool> InsertDataIntoDynamicTableAsync(string tableName, List<Dictionary<string, object>> data);
        Task<bool> DropDynamicTableAsync(string tableName);
        Task<List<Dictionary<string, object>>> GetDataFromDynamicTableAsync(string tableName, int skip = 0, int take = 100);
        Task<TableSchema?> GetTableSchemaAsync(Guid letterTypeId, Guid excelUploadId);
        Task<List<ColumnDefinition>> DetectColumnTypesAsync(List<Dictionary<string, object>> sampleData);
        Task<bool> TableExistsAsync(string tableName);
        Task<string> GenerateTableNameAsync(Guid letterTypeId, string prefix = "Data");
        Task<string> GetTableNameForLetterTypeAsync(Guid letterTypeId);
    }
}
