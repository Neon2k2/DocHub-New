using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DocHub.Core.Entities
{
    public class TableSchema
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(128)]
        public string TableName { get; set; } = string.Empty;

        [Required]
        public Guid LetterTypeDefinitionId { get; set; }

        public Guid? ExcelUploadId { get; set; } // Nullable - tables can be created without Excel upload

        [Required]
        public string ColumnDefinitions { get; set; } = string.Empty; // JSON array of column definitions

        [Required]
        public int TotalRows { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        [Required]
        public DateTime UpdatedAt { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual LetterTypeDefinition LetterTypeDefinition { get; set; } = null!;
        public virtual ExcelUpload? ExcelUpload { get; set; } // Nullable - tables can be created without Excel upload
    }

    public class ColumnDefinition
    {
        public string ColumnName { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public int MaxLength { get; set; }
        public bool IsNullable { get; set; } = true;
        public bool IsPrimaryKey { get; set; } = false;
        public string? DefaultValue { get; set; }
    }
}
