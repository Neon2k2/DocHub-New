using DocHub.Core.Entities;
using DocHub.Core.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Text.Json;

namespace DocHub.Application.Services
{
    public class DynamicTableService : IDynamicTableService
    {
        private readonly IDbContext _dbContext;
        private readonly IRepository<TableSchema> _tableSchemaRepository;
        private readonly ILogger<DynamicTableService> _logger;

        public DynamicTableService(
            IDbContext dbContext,
            IRepository<TableSchema> tableSchemaRepository,
            ILogger<DynamicTableService> logger)
        {
            _dbContext = dbContext;
            _tableSchemaRepository = tableSchemaRepository;
            _logger = logger;
        }

        public async Task<string> CreateDynamicTableAsync(Guid letterTypeId, Guid excelUploadId, List<ColumnDefinition> columns, List<Dictionary<string, object>> data)
        {
            try
            {
                var tableName = await GenerateTableNameAsync(letterTypeId);
                _logger.LogInformation("🔧 [DYNAMIC-TABLE] Creating dynamic table: {TableName}", tableName);
                _logger.LogInformation("📊 [DYNAMIC-TABLE] Column definitions: {ColumnDefinitions}", 
                    string.Join(", ", columns.Select(c => $"{c.ColumnName}({c.DataType}){(c.IsNullable ? " NULL" : " NOT NULL")}")));

                // Generate CREATE TABLE SQL
                var createTableSql = GenerateCreateTableSql(tableName, columns);
                _logger.LogInformation("📝 [DYNAMIC-TABLE] Generated SQL: {Sql}", createTableSql);

                // Execute CREATE TABLE
                await ExecuteRawSqlAsync(createTableSql);
                _logger.LogInformation("✅ [DYNAMIC-TABLE] Table created successfully: {TableName}", tableName);

                // Insert data
                if (data.Any())
                {
                    _logger.LogInformation("📊 [DYNAMIC-TABLE] Starting data insertion for {RowCount} rows", data.Count);
                    await InsertDataIntoDynamicTableAsync(tableName, data);
                    _logger.LogInformation("📊 [DYNAMIC-TABLE] Inserted {RowCount} rows into {TableName}", data.Count, tableName);
                }
                else
                {
                    _logger.LogWarning("⚠️ [DYNAMIC-TABLE] No data to insert into {TableName}", tableName);
                }

                // Save table schema metadata
                var tableSchema = new TableSchema
                {
                    Id = Guid.NewGuid(),
                    TableName = tableName,
                    LetterTypeDefinitionId = letterTypeId,
                    ExcelUploadId = excelUploadId == Guid.Empty ? null : excelUploadId, // Allow null for tables created without Excel upload
                    ColumnDefinitions = JsonSerializer.Serialize(columns),
                    TotalRows = data.Count,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                await _tableSchemaRepository.AddAsync(tableSchema);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("💾 [DYNAMIC-TABLE] Table schema saved to metadata");
                return tableName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [DYNAMIC-TABLE] Error creating dynamic table for letter type {LetterTypeId}", letterTypeId);
                throw;
            }
        }

        public async Task<bool> InsertDataIntoDynamicTableAsync(string tableName, List<Dictionary<string, object>> data)
        {
            try
            {
                _logger.LogInformation("🔍 [DYNAMIC-TABLE] Starting data insertion into {TableName}", tableName);
                _logger.LogInformation("📊 [DYNAMIC-TABLE] Data count: {DataCount}", data.Count);

                if (!data.Any()) 
                {
                    _logger.LogWarning("⚠️ [DYNAMIC-TABLE] No data to insert into {TableName}", tableName);
                    return true;
                }

                var columns = data.First().Keys.ToList();
                _logger.LogInformation("📋 [DYNAMIC-TABLE] Excel columns: {Columns}", string.Join(", ", columns));
                
                // Trim column names to ensure consistency with table creation
                var trimmedColumns = columns.Select(c => c.Trim()).ToList();
                _logger.LogInformation("🔧 [DYNAMIC-TABLE] Trimmed columns: {TrimmedColumns}", string.Join(", ", trimmedColumns));
                
                var columnNames = string.Join(", ", trimmedColumns.Select(c => $"[{c}]"));
                
                // Create valid parameter names by replacing spaces and special characters
                var parameterMappings = trimmedColumns.Select((c, index) => new { 
                    Column = c, 
                    Parameter = $"@param{index}" 
                }).ToList();
                var parameterNames = string.Join(", ", parameterMappings.Select(p => p.Parameter));

                var insertSql = $"INSERT INTO [{tableName}] ({columnNames}) VALUES ({parameterNames})";
                _logger.LogInformation("📝 [DYNAMIC-TABLE] Insert SQL: {InsertSql}", insertSql);

                using var connection = new SqlConnection(((Microsoft.EntityFrameworkCore.DbContext)_dbContext).Database.GetConnectionString());
                await connection.OpenAsync();

                using var transaction = connection.BeginTransaction();
                try
                {
                    var rowIndex = 0;
                    foreach (var row in data)
                    {
                        _logger.LogInformation("🔄 [DYNAMIC-TABLE] Processing row {RowIndex}/{TotalRows}", rowIndex + 1, data.Count);
                        _logger.LogInformation("📊 [DYNAMIC-TABLE] Row data: {RowData}", string.Join(", ", row.Select(kv => $"{kv.Key}={kv.Value}")));
                        
                        using var command = new SqlCommand(insertSql, connection, transaction);
                        
                        foreach (var mapping in parameterMappings)
                        {
                            // Try to find the value using the original column name (with spaces) first
                            var originalColumn = columns.FirstOrDefault(c => c.Trim() == mapping.Column);
                            var value = originalColumn != null && row.ContainsKey(originalColumn) 
                                ? row[originalColumn] 
                                : DBNull.Value;
                            
                            // Format EMP ID to avoid scientific notation
                            if (mapping.Column.Equals("EMP ID", StringComparison.OrdinalIgnoreCase) && value != null && value != DBNull.Value)
                            {
                                if (double.TryParse(value.ToString(), out double empIdValue))
                                {
                                    // Convert to integer and then to string to avoid scientific notation
                                    value = ((long)empIdValue).ToString();
                                }
                            }
                            
                            _logger.LogDebug("🔧 [DYNAMIC-TABLE] Column: {Column}, Original: {OriginalColumn}, Value: {Value}", 
                                mapping.Column, originalColumn, value);
                            
                            // Handle null values gracefully - use DBNull.Value for missing columns
                            command.Parameters.AddWithValue(mapping.Parameter, value ?? DBNull.Value);
                        }

                        await command.ExecuteNonQueryAsync();
                        rowIndex++;
                    }

                    transaction.Commit();
                    _logger.LogInformation("✅ [DYNAMIC-TABLE] Successfully inserted {RowCount} rows into {TableName}", data.Count, tableName);
                    return true;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    _logger.LogError(ex, "❌ [DYNAMIC-TABLE] Error during transaction, rolling back");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [DYNAMIC-TABLE] Error inserting data into table {TableName}", tableName);
                return false;
            }
        }

        public async Task<bool> DropDynamicTableAsync(string tableName)
        {
            try
            {
                var dropSql = $"DROP TABLE IF EXISTS [{tableName}]";
                await ExecuteRawSqlAsync(dropSql);
                
                // Mark table schema as inactive
                var tableSchema = await _tableSchemaRepository.GetFirstIncludingAsync(ts => ts.TableName == tableName);
                if (tableSchema != null)
                {
                    tableSchema.IsActive = false;
                    tableSchema.UpdatedAt = DateTime.UtcNow;
                    await _dbContext.SaveChangesAsync();
                }

                _logger.LogInformation("🗑️ [DYNAMIC-TABLE] Dropped table: {TableName}", tableName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [DYNAMIC-TABLE] Error dropping table {TableName}", tableName);
                return false;
            }
        }

        public async Task<List<Dictionary<string, object>>> GetDataFromDynamicTableAsync(string tableName, int skip = 0, int take = 100)
        {
            try
            {
                var selectSql = $"SELECT * FROM [{tableName}] ORDER BY (SELECT NULL) OFFSET {skip} ROWS FETCH NEXT {take} ROWS ONLY";
                
                using var connection = new SqlConnection(((Microsoft.EntityFrameworkCore.DbContext)_dbContext).Database.GetConnectionString());
                await connection.OpenAsync();

                using var command = new SqlCommand(selectSql, connection);
                using var reader = await command.ExecuteReaderAsync();

                var results = new List<Dictionary<string, object>>();
                while (await reader.ReadAsync())
                {
                    var row = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var columnName = reader.GetName(i);
                        var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                        
                        // Format EMP ID to avoid scientific notation when retrieving
                        if (columnName.Equals("EMP ID", StringComparison.OrdinalIgnoreCase) && value != null)
                        {
                            if (double.TryParse(value.ToString(), out double empIdValue))
                            {
                                // Convert to integer and then to string to avoid scientific notation
                                value = ((long)empIdValue).ToString();
                            }
                        }
                        
                        row[columnName] = value ?? string.Empty;
                    }
                    results.Add(row);
                }

                _logger.LogInformation("📊 [DYNAMIC-TABLE] Retrieved {RowCount} rows from {TableName}", results.Count, tableName);
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [DYNAMIC-TABLE] Error retrieving data from table {TableName}", tableName);
                return new List<Dictionary<string, object>>();
            }
        }

        public async Task<TableSchema?> GetTableSchemaAsync(Guid letterTypeId, Guid excelUploadId)
        {
            return await _tableSchemaRepository.GetFirstIncludingAsync(ts => 
                ts.LetterTypeDefinitionId == letterTypeId && 
                ts.ExcelUploadId == excelUploadId && 
                ts.IsActive);
        }

        public Task<List<ColumnDefinition>> DetectColumnTypesAsync(List<Dictionary<string, object>> sampleData)
        {
            if (!sampleData.Any()) return Task.FromResult(new List<ColumnDefinition>());

            var columns = new List<ColumnDefinition>();
            var firstRow = sampleData.First();

            foreach (var column in firstRow.Keys)
            {
                var columnDef = new ColumnDefinition
                {
                    ColumnName = SanitizeColumnName(column),
                    DataType = DetectDataType(sampleData.Select(row => row.ContainsKey(column) ? row[column] : null).ToList()),
                    MaxLength = DetectMaxLength(sampleData.Select(row => row.ContainsKey(column) ? row[column] : null).ToList()),
                    IsNullable = true, // All columns are nullable to handle missing data
                    DefaultValue = "" // Empty string as default for missing values
                };

                columns.Add(columnDef);
            }

            _logger.LogInformation("🔍 [DYNAMIC-TABLE] Detected {ColumnCount} columns with types", columns.Count);
            return Task.FromResult(columns);
        }

        public async Task<bool> TableExistsAsync(string tableName)
        {
            try
            {
                var checkSql = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @tableName";
                
                using var connection = new SqlConnection(((Microsoft.EntityFrameworkCore.DbContext)_dbContext).Database.GetConnectionString());
                await connection.OpenAsync();

                using var command = new SqlCommand(checkSql, connection);
                command.Parameters.AddWithValue("@tableName", tableName);
                
                var result = await command.ExecuteScalarAsync();
                var count = result != null ? (int)result : 0;
                return count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [DYNAMIC-TABLE] Error checking if table exists: {TableName}", tableName);
                return false;
            }
        }

        public async Task<string> GenerateTableNameAsync(Guid letterTypeId, string prefix = "Data")
        {
            // Get the letter type definition to use its display name as the base
            var letterType = await _dbContext.LetterTypeDefinitions.FindAsync(letterTypeId);
            string baseName;
            
            if (letterType != null && !string.IsNullOrEmpty(letterType.DisplayName))
            {
                // Use the display name from the letter type definition (tab name)
                baseName = System.Text.RegularExpressions.Regex.Replace(letterType.DisplayName, @"[^a-zA-Z0-9_]", "_");
            }
            else
            {
                // Fallback to using the letter type ID
                baseName = $"{prefix}_{letterTypeId.ToString("N")[..8]}";
            }
            
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var tableName = $"{baseName}_{timestamp}";
            
            // Ensure table name is unique
            var counter = 1;
            var originalTableName = tableName;
            while (await TableExistsAsync(tableName))
            {
                tableName = $"{originalTableName}_{counter}";
                counter++;
            }

            return tableName;
        }

        private string GenerateCreateTableSql(string tableName, List<ColumnDefinition> columns)
        {
            var columnDefinitions = columns.Select(col =>
            {
                var dataType = GetSqlDataType(col);
                // Make all columns nullable to handle missing data gracefully
                var nullable = "NULL";
                var defaultValue = !string.IsNullOrEmpty(col.DefaultValue) ? $"DEFAULT '{col.DefaultValue}'" : "";
                
                // Trim column name to ensure consistency
                var trimmedColumnName = col.ColumnName.Trim();
                return $"[{trimmedColumnName}] {dataType} {nullable} {defaultValue}".Trim();
            });

            return $"CREATE TABLE [{tableName}] (\n    " +
                   string.Join(",\n    ", columnDefinitions) +
                   "\n)";
        }

        private string GetSqlDataType(ColumnDefinition column)
        {
            // All columns are NVARCHAR for simplicity
            return $"NVARCHAR({Math.Max(column.MaxLength, 255)})";
        }

        private string DetectDataType(List<object?> values)
        {
            var nonNullValues = values.Where(v => v != null).ToList();
            if (!nonNullValues.Any()) return "nvarchar";

            // Check for integers
            if (nonNullValues.All(v => IsInteger(v)))
                return "int";

            // Check for decimals
            if (nonNullValues.All(v => IsDecimal(v)))
                return "decimal";

            // Check for dates
            if (nonNullValues.All(v => IsDateTime(v)))
                return "datetime";

            // Check for booleans
            if (nonNullValues.All(v => IsBoolean(v)))
                return "bit";

            // Default to string
            return "nvarchar";
        }

        private int DetectMaxLength(List<object?> values)
        {
            var nonNullValues = values.Where(v => v != null).ToList();
            if (!nonNullValues.Any()) return 255;

            var maxLength = nonNullValues.Max(v => v?.ToString()?.Length ?? 0);
            return Math.Max(maxLength, 50); // Minimum 50 characters
        }

        private string SanitizeColumnName(string columnName)
        {
            // Remove special characters and replace spaces with underscores
            var sanitized = System.Text.RegularExpressions.Regex.Replace(columnName, @"[^a-zA-Z0-9_]", "_");
            
            // Ensure it starts with a letter or underscore
            if (char.IsDigit(sanitized[0]))
                sanitized = "_" + sanitized;

            return sanitized;
        }

        private bool IsInteger(object? value)
        {
            return value switch
            {
                int => true,
                long => true,
                short => true,
                byte => true,
                string str => int.TryParse(str, out _),
                _ => false
            };
        }

        private bool IsDecimal(object? value)
        {
            return value switch
            {
                decimal => true,
                double => true,
                float => true,
                string str => decimal.TryParse(str, out _),
                _ => false
            };
        }

        private bool IsDateTime(object? value)
        {
            return value switch
            {
                DateTime => true,
                string str => DateTime.TryParse(str, out _),
                _ => false
            };
        }

        private bool IsBoolean(object? value)
        {
            return value switch
            {
                bool => true,
                string str => bool.TryParse(str, out _),
                _ => false
            };
        }

        private async Task ExecuteRawSqlAsync(string sql)
        {
            using var connection = new SqlConnection(((Microsoft.EntityFrameworkCore.DbContext)_dbContext).Database.GetConnectionString());
            await connection.OpenAsync();
            using var command = new SqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }

        public async Task<string> GetTableNameForLetterTypeAsync(Guid letterTypeId)
        {
            try
            {
                var tableSchema = await _tableSchemaRepository.GetFirstIncludingAsync(
                    t => t.LetterTypeDefinitionId == letterTypeId && t.IsActive,
                    t => t.LetterTypeDefinition
                );

                if (tableSchema == null)
                {
                    _logger.LogWarning("No active table schema found for letter type {LetterTypeId}", letterTypeId);
                    return string.Empty;
                }

                _logger.LogInformation("Found table schema for letter type {LetterTypeId}: {TableName}", 
                    letterTypeId, tableSchema.TableName);
                
                return tableSchema.TableName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting table name for letter type {LetterTypeId}", letterTypeId);
                return string.Empty;
            }
        }
    }
}
