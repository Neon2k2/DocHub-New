using DocHub.Core.Entities;
using DocHub.Core.Interfaces;
using DocHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DocHub.Infrastructure.Repositories;

public class TableSchemaRepository : Repository<TableSchema>, IRepository<TableSchema>
{
    public TableSchemaRepository(DocHubDbContext context) : base(context)
    {
    }
}
