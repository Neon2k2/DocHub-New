using DocHub.Shared.DTOs.Tabs;
using DocHub.Shared.DTOs.Common;
using DocHub.Core.Entities;

namespace DocHub.Core.Interfaces;

public interface IDynamicLetterGenerationService
{
    Task<byte[]> GenerateLetterAsync(DynamicTabDto tab, EmployeeDto employee, DocumentTemplate template, string? signaturePath = null, Dictionary<string, object>? employeeData = null);
    Task<byte[]> GenerateLetterZipAsync(DynamicTabDto tab, List<(EmployeeDto employee, DocumentTemplate template)> employees, string? signaturePath = null);
    Task<byte[]?> GeneratePdfPreviewAsync(DynamicTabDto tab, EmployeeDto employee, DocumentTemplate template, string? signaturePath = null, Dictionary<string, object>? employeeData = null);
    byte[] ConvertDocxToPdf(byte[] docxBytes);
}
