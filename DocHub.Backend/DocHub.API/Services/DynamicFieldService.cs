using Microsoft.EntityFrameworkCore;
using DocHub.API.Data;
using DocHub.API.Models;
using DocHub.API.Services.Interfaces;
using DocHub.API.DTOs;
using System.Text.Json;

namespace DocHub.API.Services;

public class DynamicFieldService : IDynamicFieldService
{
    private readonly DocHubDbContext _context;
    private readonly ILogger<DynamicFieldService> _logger;

    public DynamicFieldService(DocHubDbContext context, ILogger<DynamicFieldService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<DynamicField>> GetAllAsync()
    {
        return await _context.DynamicFields
            .Include(f => f.LetterTypeDefinition)
            .OrderBy(f => f.LetterTypeDefinition.DisplayName)
            .ThenBy(f => f.Order)
            .ToListAsync();
    }

    public async Task<DynamicField?> GetByIdAsync(Guid id)
    {
        return await _context.DynamicFields
            .Include(f => f.LetterTypeDefinition)
            .FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task<IEnumerable<DynamicField>> GetByLetterTypeDefinitionIdAsync(Guid letterTypeDefinitionId)
    {
        return await _context.DynamicFields
            .Where(f => f.LetterTypeDefinitionId == letterTypeDefinitionId)
            .OrderBy(f => f.Order)
            .ToListAsync();
    }

    public async Task<DynamicField> CreateAsync(DynamicField field)
    {
        // Validate field key uniqueness
        if (await FieldKeyExistsAsync(field.LetterTypeDefinitionId, field.FieldKey))
        {
            throw new InvalidOperationException($"Field key '{field.FieldKey}' already exists for this letter type");
        }

        // Set order if not provided
        if (field.Order == 0)
        {
            var maxOrder = await _context.DynamicFields
                .Where(f => f.LetterTypeDefinitionId == field.LetterTypeDefinitionId)
                .MaxAsync(f => (int?)f.Order) ?? 0;
            field.Order = maxOrder + 1;
        }

        _context.DynamicFields.Add(field);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created dynamic field {FieldKey} for letter type {LetterTypeId}", 
            field.FieldKey, field.LetterTypeDefinitionId);

        return field;
    }

    public async Task<DynamicField> UpdateAsync(DynamicField field)
    {
        // Validate field key uniqueness (excluding current field)
        if (await FieldKeyExistsAsync(field.LetterTypeDefinitionId, field.FieldKey, field.Id))
        {
            throw new InvalidOperationException($"Field key '{field.FieldKey}' already exists for this letter type");
        }

        _context.DynamicFields.Update(field);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated dynamic field {FieldKey} for letter type {LetterTypeId}", 
            field.FieldKey, field.LetterTypeDefinitionId);

        return field;
    }

    public async Task DeleteAsync(Guid id)
    {
        var field = await _context.DynamicFields.FindAsync(id);
        if (field == null)
        {
            throw new ArgumentException("Dynamic field not found");
        }

        _context.DynamicFields.Remove(field);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted dynamic field {FieldKey} for letter type {LetterTypeId}", 
            field.FieldKey, field.LetterTypeDefinitionId);
    }

    public async Task ReorderFieldsAsync(Guid letterTypeDefinitionId, List<FieldOrder> fieldOrders)
    {
        var fields = await _context.DynamicFields
            .Where(f => f.LetterTypeDefinitionId == letterTypeDefinitionId)
            .ToListAsync();

        foreach (var fieldOrder in fieldOrders)
        {
            var field = fields.FirstOrDefault(f => f.Id == fieldOrder.FieldId);
            if (field != null)
            {
                field.Order = fieldOrder.Order;
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Reordered {Count} fields for letter type {LetterTypeId}", 
            fieldOrders.Count, letterTypeDefinitionId);
    }

    public async Task<FieldValidationResult> ValidateFieldAsync(ValidateFieldRequest request)
    {
        var result = new FieldValidationResult { IsValid = true };

        // Validate field key format
        if (string.IsNullOrWhiteSpace(request.FieldKey))
        {
            result.Errors.Add("Field key is required");
            result.IsValid = false;
        }
        else if (!IsValidFieldKey(request.FieldKey))
        {
            result.Errors.Add("Field key must contain only letters, numbers, and underscores");
            result.IsValid = false;
        }

        // Validate field key uniqueness
        if (await FieldKeyExistsAsync(request.LetterTypeDefinitionId, request.FieldKey))
        {
            result.Errors.Add($"Field key '{request.FieldKey}' already exists for this letter type");
            result.IsValid = false;
        }

        // Validate display name
        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            result.Errors.Add("Display name is required");
            result.IsValid = false;
        }

        // Validate field type
        var validFieldTypes = new[] { "Text", "Number", "Date", "Email", "PhoneNumber", "Currency", 
            "Percentage", "Boolean", "Dropdown", "TextArea", "Url", "Image", "File", "DateTime", "Time", "Json" };
        
        if (!validFieldTypes.Contains(request.FieldType))
        {
            result.Errors.Add($"Invalid field type '{request.FieldType}'. Valid types: {string.Join(", ", validFieldTypes)}");
            result.IsValid = false;
        }

        // Validate validation rules JSON
        if (!string.IsNullOrEmpty(request.ValidationRules))
        {
            try
            {
                JsonDocument.Parse(request.ValidationRules);
            }
            catch (JsonException)
            {
                result.Errors.Add("Validation rules must be valid JSON");
                result.IsValid = false;
            }
        }

        // Add suggestions
        if (request.FieldType == "Email" && !request.FieldKey.ToLower().Contains("email"))
        {
            result.Suggestions["fieldKey"] = "Consider using 'email' in the field key for email fields";
        }

        if (request.FieldType == "Date" && !request.FieldKey.ToLower().Contains("date"))
        {
            result.Suggestions["fieldKey"] = "Consider using 'date' in the field key for date fields";
        }

        return result;
    }

    public async Task<bool> FieldKeyExistsAsync(Guid letterTypeDefinitionId, string fieldKey, Guid? excludeId = null)
    {
        var query = _context.DynamicFields
            .Where(f => f.LetterTypeDefinitionId == letterTypeDefinitionId && f.FieldKey == fieldKey);

        if (excludeId.HasValue)
        {
            query = query.Where(f => f.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<IEnumerable<DynamicField>> GetRequiredFieldsAsync(Guid letterTypeDefinitionId)
    {
        return await _context.DynamicFields
            .Where(f => f.LetterTypeDefinitionId == letterTypeDefinitionId && f.IsRequired)
            .OrderBy(f => f.Order)
            .ToListAsync();
    }

    public async Task<Dictionary<string, object>> GetFieldDefaultsAsync(Guid letterTypeDefinitionId)
    {
        var fields = await _context.DynamicFields
            .Where(f => f.LetterTypeDefinitionId == letterTypeDefinitionId && !string.IsNullOrEmpty(f.DefaultValue))
            .ToListAsync();

        var defaults = new Dictionary<string, object>();
        foreach (var field in fields)
        {
            defaults[field.FieldKey] = ConvertDefaultValue(field.DefaultValue ?? string.Empty, field.FieldType);
        }

        return defaults;
    }

    private static bool IsValidFieldKey(string fieldKey)
    {
        return !string.IsNullOrWhiteSpace(fieldKey) && 
               fieldKey.All(c => char.IsLetterOrDigit(c) || c == '_') &&
               char.IsLetter(fieldKey[0]);
    }

    private static object ConvertDefaultValue(string defaultValue, string fieldType)
    {
        return fieldType switch
        {
            "Number" or "Currency" or "Percentage" => decimal.TryParse(defaultValue, out var num) ? num : 0,
            "Boolean" => bool.TryParse(defaultValue, out var boolVal) && boolVal,
            "Date" or "DateTime" => DateTime.TryParse(defaultValue, out var dateVal) ? dateVal : DateTime.Now,
            "Time" => TimeSpan.TryParse(defaultValue, out var timeVal) ? timeVal : TimeSpan.Zero,
            _ => defaultValue
        };
    }
}
