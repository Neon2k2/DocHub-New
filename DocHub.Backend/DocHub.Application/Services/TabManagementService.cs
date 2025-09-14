using DocHub.Core.Entities;
using DocHub.Core.Interfaces;
using DocHub.Core.Interfaces.Repositories;
using DocHub.Shared.DTOs.Tabs;
using DocHub.Shared.DTOs.Common;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DocHub.Application.Services;

public class TabManagementService : ITabManagementService
{
    private readonly IRepository<LetterTypeDefinition> _letterTypeRepository;
    private readonly IRepository<DynamicField> _dynamicFieldRepository;
    private readonly IRepository<TableSchema> _tableSchemaRepository;
    private readonly IDbContext _dbContext;
    private readonly ILogger<TabManagementService> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IDynamicTableService _dynamicTableService;

    public TabManagementService(
        IRepository<LetterTypeDefinition> letterTypeRepository,
        IRepository<DynamicField> dynamicFieldRepository,
        IRepository<TableSchema> tableSchemaRepository,
        IDbContext dbContext,
        ILogger<TabManagementService> logger,
        ILoggerFactory loggerFactory,
        IDynamicTableService dynamicTableService)
    {
        _letterTypeRepository = letterTypeRepository;
        _dynamicFieldRepository = dynamicFieldRepository;
        _tableSchemaRepository = tableSchemaRepository;
        _dbContext = dbContext;
        _logger = logger;
        _loggerFactory = loggerFactory;
        _dynamicTableService = dynamicTableService;
    }

    public async Task<LetterTypeDefinitionDto> CreateLetterTypeAsync(CreateLetterTypeRequest request, string userId)
    {
        try
        {
            _logger.LogInformation("🔄 [TAB-SERVICE] Starting CreateLetterTypeAsync");
            _logger.LogInformation("📋 [TAB-SERVICE] Request details - TypeKey: {TypeKey}, DisplayName: {DisplayName}, UserId: {UserId}", 
                request.TypeKey, request.DisplayName, userId);

            // Check if a tab with this TypeKey already exists
            _logger.LogInformation("🔍 [TAB-SERVICE] Checking if tab with TypeKey '{TypeKey}' already exists...", request.TypeKey);
            var existingTab = await _letterTypeRepository.FirstOrDefaultAsync(lt => lt.TypeKey == request.TypeKey);
            if (existingTab != null)
            {
                _logger.LogWarning("⚠️ [TAB-SERVICE] Tab with TypeKey '{TypeKey}' already exists with ID: {Id}", 
                    request.TypeKey, existingTab.Id);
                throw new InvalidOperationException($"A tab with the key '{request.TypeKey}' already exists. Please use a different key.");
            }
            _logger.LogInformation("✅ [TAB-SERVICE] No existing tab found with TypeKey '{TypeKey}'", request.TypeKey);

            _logger.LogInformation("🏗️ [TAB-SERVICE] Creating LetterTypeDefinition entity...");
            var letterType = new LetterTypeDefinition
            {
                TypeKey = request.TypeKey,
                DisplayName = request.DisplayName,
                Description = request.Description,
                DataSourceType = request.DataSourceType,
                FieldConfiguration = request.FieldConfiguration,
                TableSchema = request.TableSchema,
                IsActive = true
            };
            _logger.LogInformation("✅ [TAB-SERVICE] LetterTypeDefinition entity created with ID: {Id}", letterType.Id);

            _logger.LogInformation("💾 [TAB-SERVICE] Adding letter type to repository...");
            await _letterTypeRepository.AddAsync(letterType);
            _logger.LogInformation("✅ [TAB-SERVICE] Letter type added to repository");

            _logger.LogInformation("💾 [TAB-SERVICE] Saving changes to database...");
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("✅ [TAB-SERVICE] Changes saved to database successfully");

            // Create dynamic fields if provided
            List<CreateDynamicFieldRequest> fieldsToCreate = new List<CreateDynamicFieldRequest>();
            
            // First, try to get fields from request.Fields
            if (request.Fields != null && request.Fields.Any())
            {
                _logger.LogInformation("🔧 [TAB-SERVICE] Using fields from request.Fields: {FieldCount}", request.Fields.Count);
                fieldsToCreate.AddRange(request.Fields);
            }
            // If no fields in request.Fields, try to parse from FieldConfiguration JSON
            else if (!string.IsNullOrEmpty(request.FieldConfiguration))
            {
                _logger.LogInformation("🔧 [TAB-SERVICE] Parsing fields from FieldConfiguration JSON...");
                try
                {
                    var fieldConfig = JsonSerializer.Deserialize<FieldConfigurationDto>(request.FieldConfiguration);
                    if (fieldConfig?.Fields != null && fieldConfig.Fields.Any())
                    {
                        _logger.LogInformation("🔧 [TAB-SERVICE] Parsed {FieldCount} fields from FieldConfiguration", fieldConfig.Fields.Count);
                        fieldsToCreate.AddRange(fieldConfig.Fields.Select(f => new CreateDynamicFieldRequest
                        {
                            FieldKey = f.FieldKey,
                            FieldName = f.FieldName,
                            DisplayName = f.DisplayName,
                            FieldType = "Text", // Always Text type for simplicity
                            IsRequired = f.IsRequired,
                            OrderIndex = f.Order
                        }));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ [TAB-SERVICE] Error parsing FieldConfiguration JSON: {Message}", ex.Message);
                }
            }

            if (fieldsToCreate.Any())
            {
                _logger.LogInformation("🔧 [TAB-SERVICE] Creating {FieldCount} dynamic fields...", fieldsToCreate.Count);
                foreach (var fieldRequest in fieldsToCreate)
                {
                    _logger.LogInformation("🏗️ [TAB-SERVICE] Creating field: {FieldKey} ({FieldType})", 
                        fieldRequest.FieldKey, fieldRequest.FieldType);
                    
                    var field = new DynamicField
                    {
                        LetterTypeDefinitionId = letterType.Id,
                        FieldKey = fieldRequest.FieldKey,
                        FieldName = fieldRequest.FieldName,
                        DisplayName = fieldRequest.DisplayName,
                        FieldType = "Text", // Always Text type for simplicity
                        IsRequired = fieldRequest.IsRequired,
                        ValidationRules = fieldRequest.ValidationRules,
                        DefaultValue = fieldRequest.DefaultValue,
                        OrderIndex = fieldRequest.OrderIndex
                    };

                    await _dynamicFieldRepository.AddAsync(field);
                    _logger.LogInformation("✅ [TAB-SERVICE] Field created: {FieldKey}", fieldRequest.FieldKey);
                }

                _logger.LogInformation("💾 [TAB-SERVICE] Saving field changes to database...");
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("✅ [TAB-SERVICE] Field changes saved successfully");

                // Create dynamic table based on the fields
                _logger.LogInformation("🏗️ [TAB-SERVICE] Creating dynamic table for tab: {DisplayName}", letterType.DisplayName);
                await CreateDynamicTableForTab(letterType.Id, letterType.DisplayName, fieldsToCreate);
            }
            else
            {
                _logger.LogInformation("ℹ️ [TAB-SERVICE] No dynamic fields to create");
            }

            _logger.LogInformation("🔄 [TAB-SERVICE] Mapping to DTO...");
            var result = await MapToDto(letterType);
            _logger.LogInformation("✅ [TAB-SERVICE] DTO mapping completed");
            _logger.LogInformation("🎉 [TAB-SERVICE] Letter type created successfully: {TypeKey}", letterType.TypeKey);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [TAB-SERVICE] Error creating letter type: {TypeKey} - {Message}", 
                request.TypeKey, ex.Message);
            throw;
        }
    }

    public async Task<LetterTypeDefinitionDto> UpdateLetterTypeAsync(Guid id, UpdateLetterTypeRequest request, string userId)
    {
        try
        {
            var letterType = await _letterTypeRepository.GetByIdAsync(id);
            if (letterType == null)
            {
                throw new ArgumentException("Letter type not found");
            }

            // Update properties
            if (!string.IsNullOrEmpty(request.DisplayName))
                letterType.DisplayName = request.DisplayName;
            
            if (!string.IsNullOrEmpty(request.Description))
                letterType.Description = request.Description;
            
            if (!string.IsNullOrEmpty(request.DataSourceType))
                letterType.DataSourceType = request.DataSourceType;
            
            if (!string.IsNullOrEmpty(request.FieldConfiguration))
                letterType.FieldConfiguration = request.FieldConfiguration;
            
            if (!string.IsNullOrEmpty(request.TableSchema))
                letterType.TableSchema = request.TableSchema;

            if (request.IsActive.HasValue)
                letterType.IsActive = request.IsActive.Value;

            letterType.UpdatedAt = DateTime.UtcNow;

            await _letterTypeRepository.UpdateAsync(letterType);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Letter type updated successfully: {Id}", id);
            return await MapToDto(letterType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating letter type: {Id}", id);
            throw;
        }
    }

    public async Task DeleteLetterTypeAsync(Guid id, string userId)
    {
        try
        {
            var letterType = await _letterTypeRepository.GetByIdAsync(id);
            if (letterType == null)
            {
                throw new ArgumentException("Letter type not found");
            }

            // Soft delete by setting IsActive to false
            letterType.IsActive = false;
            letterType.UpdatedAt = DateTime.UtcNow;

            await _letterTypeRepository.UpdateAsync(letterType);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Letter type deleted successfully: {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting letter type: {Id}", id);
            throw;
        }
    }

    public async Task<IEnumerable<LetterTypeDefinitionDto>> GetLetterTypesAsync()
    {
        try
        {
            var letterTypes = await _letterTypeRepository.GetAllAsync();
            var result = new List<LetterTypeDefinitionDto>();

            foreach (var letterType in letterTypes.Where(lt => lt.IsActive))
            {
                _logger.LogInformation("LetterType FieldConfiguration: {FieldConfiguration}", letterType.FieldConfiguration);
                result.Add(await MapToDto(letterType));
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting letter types");
            throw;
        }
    }

    public async Task<LetterTypeDefinitionDto> GetLetterTypeAsync(Guid id)
    {
        try
        {
            var letterType = await _letterTypeRepository.GetByIdAsync(id);
            if (letterType == null || !letterType.IsActive)
            {
                throw new ArgumentException("Letter type not found");
            }

            return await MapToDto(letterType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting letter type: {Id}", id);
            throw;
        }
    }

    public async Task<DynamicFieldDto> CreateFieldAsync(CreateDynamicFieldRequest request, string userId)
    {
        try
        {
            var field = new DynamicField
            {
                LetterTypeDefinitionId = request.LetterTypeDefinitionId,
                FieldKey = request.FieldKey,
                FieldName = request.FieldName,
                DisplayName = request.DisplayName,
                FieldType = request.FieldType,
                IsRequired = request.IsRequired,
                ValidationRules = request.ValidationRules,
                DefaultValue = request.DefaultValue,
                OrderIndex = request.OrderIndex
            };

            await _dynamicFieldRepository.AddAsync(field);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Dynamic field created successfully: {FieldKey}", field.FieldKey);
            return MapFieldToDto(field);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating dynamic field: {FieldKey}", request.FieldKey);
            throw;
        }
    }

    public async Task<DynamicFieldDto> UpdateFieldAsync(Guid id, UpdateDynamicFieldRequest request, string userId)
    {
        try
        {
            var field = await _dynamicFieldRepository.GetByIdAsync(id);
            if (field == null)
            {
                throw new ArgumentException("Dynamic field not found");
            }

            // Update properties
            if (!string.IsNullOrEmpty(request.FieldName))
                field.FieldName = request.FieldName;
            
            if (!string.IsNullOrEmpty(request.DisplayName))
                field.DisplayName = request.DisplayName;
            
            if (!string.IsNullOrEmpty(request.FieldType))
                field.FieldType = request.FieldType;
            
            if (request.IsRequired.HasValue)
                field.IsRequired = request.IsRequired.Value;
            
            if (!string.IsNullOrEmpty(request.ValidationRules))
                field.ValidationRules = request.ValidationRules;
            
            if (request.DefaultValue != null)
                field.DefaultValue = request.DefaultValue;
            
            if (request.OrderIndex.HasValue)
                field.OrderIndex = request.OrderIndex.Value;

            field.UpdatedAt = DateTime.UtcNow;

            await _dynamicFieldRepository.UpdateAsync(field);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Dynamic field updated successfully: {Id}", id);
            return MapFieldToDto(field);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating dynamic field: {Id}", id);
            throw;
        }
    }

    public async Task DeleteFieldAsync(Guid id, string userId)
    {
        try
        {
            var field = await _dynamicFieldRepository.GetByIdAsync(id);
            if (field == null)
            {
                throw new ArgumentException("Dynamic field not found");
            }

            await _dynamicFieldRepository.DeleteAsync(field);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Dynamic field deleted successfully: {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting dynamic field: {Id}", id);
            throw;
        }
    }

    public async Task<IEnumerable<DynamicFieldDto>> GetFieldsAsync(Guid letterTypeId)
    {
        try
        {
            var fields = await _dynamicFieldRepository.GetAllAsync();
            return fields
                .Where(f => f.LetterTypeDefinitionId == letterTypeId)
                .OrderBy(f => f.OrderIndex)
                .Select(MapFieldToDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting fields for letter type: {LetterTypeId}", letterTypeId);
            throw;
        }
    }


    private async Task<LetterTypeDefinitionDto> MapToDto(LetterTypeDefinition letterType)
    {
        var fields = await GetFieldsAsync(letterType.Id);

        var dto = new LetterTypeDefinitionDto
        {
            Id = letterType.Id,
            TypeKey = letterType.TypeKey,
            DisplayName = letterType.DisplayName,
            Description = letterType.Description,
            DataSourceType = letterType.DataSourceType,
            FieldConfiguration = letterType.FieldConfiguration,
            TableSchema = letterType.TableSchema,
            IsActive = letterType.IsActive,
            Fields = fields.ToList(),
            CreatedAt = letterType.CreatedAt,
            UpdatedAt = letterType.UpdatedAt
        };

        _logger.LogInformation("Mapped DTO FieldConfiguration: {FieldConfiguration}", dto.FieldConfiguration);
        return dto;
    }

    private static DynamicFieldDto MapFieldToDto(DynamicField field)
    {
        return new DynamicFieldDto
        {
            Id = field.Id,
            LetterTypeDefinitionId = field.LetterTypeDefinitionId,
            FieldKey = field.FieldKey,
            FieldName = field.FieldName,
            DisplayName = field.DisplayName,
            FieldType = field.FieldType,
            IsRequired = field.IsRequired,
            ValidationRules = field.ValidationRules,
            DefaultValue = field.DefaultValue,
            OrderIndex = field.OrderIndex,
            CreatedAt = field.CreatedAt,
            UpdatedAt = field.UpdatedAt
        };
    }

    private async Task CreateDynamicTableForTab(Guid letterTypeId, string tabName, List<CreateDynamicFieldRequest> fields)
    {
        try
        {
            _logger.LogInformation("🏗️ [TAB-SERVICE] Creating dynamic table for tab: {TabName}", tabName);

            // Convert fields to ColumnDefinitions
            var columns = fields.Select(field => new ColumnDefinition
            {
                ColumnName = field.FieldKey,
                DataType = MapFieldTypeToSqlType(field.FieldType),
                MaxLength = GetMaxLengthForFieldType(field.FieldType),
                IsNullable = !field.IsRequired,
                IsPrimaryKey = false,
                DefaultValue = field.DefaultValue
            }).ToList();

            // Create the dynamic table (without data initially)
            var tableName = await _dynamicTableService.CreateDynamicTableAsync(letterTypeId, Guid.Empty, columns, new List<Dictionary<string, object>>());

            _logger.LogInformation("✅ [TAB-SERVICE] Dynamic table created successfully: {TableName}", tableName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [TAB-SERVICE] Error creating dynamic table for tab: {TabName}", tabName);
            throw;
        }
    }

    private static string MapFieldTypeToSqlType(string fieldType)
    {
        // All columns are string type for simplicity
        return "string";
    }

    private static int GetMaxLengthForFieldType(string fieldType)
    {
        // All columns are string type with max length 255
        return 255;
    }
}

// DTOs for parsing field configuration JSON
public class FieldConfigurationDto
{
    [JsonPropertyName("fields")]
    public List<FieldConfigurationFieldDto> Fields { get; set; } = new List<FieldConfigurationFieldDto>();
}

public class FieldConfigurationFieldDto
{
    [JsonPropertyName("fieldKey")]
    public string FieldKey { get; set; } = string.Empty;
    
    [JsonPropertyName("fieldName")]
    public string FieldName { get; set; } = string.Empty;
    
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;
    
    [JsonPropertyName("fieldType")]
    public string FieldType { get; set; } = string.Empty;
    
    [JsonPropertyName("isRequired")]
    public bool IsRequired { get; set; }
    
    [JsonPropertyName("order")]
    public int Order { get; set; }
}