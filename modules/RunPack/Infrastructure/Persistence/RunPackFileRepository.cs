using Microsoft.EntityFrameworkCore;
using ShipMvp.Core.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RunPack.Infrastructure.Persistence;

/// <summary>
/// Entity Framework implementation of IRunPackFileRepository
/// </summary>
public class RunPackFileRepository : RunPack.Domain.IRunPackFileRepository
{
    private readonly IDbContext _db;
    private readonly DbSet<RunPack.Domain.RunPackFile> _set;

    public RunPackFileRepository(IDbContext db)
    {
        _db = db;
        _set = db.Set<RunPack.Domain.RunPackFile>();
    }

    public async Task<RunPack.Domain.RunPackFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _set
            .Include(rpf => rpf.File)
            .FirstOrDefaultAsync(rpf => rpf.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<RunPack.Domain.RunPackFile>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _set
            .Include(rpf => rpf.File)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<RunPack.Domain.RunPackFile>> GetByRunPackIdAsync(Guid runPackId, CancellationToken cancellationToken = default)
    {
        return await _set
            .Include(rpf => rpf.File)
            .Where(rpf => rpf.RunPackId == runPackId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<RunPack.Domain.RunPackFile>> GetByFileIdAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        return await _set
            .Include(rpf => rpf.File)
            .Where(rpf => rpf.FileId == fileId)
            .ToListAsync(cancellationToken);
    }

    public async Task<RunPack.Domain.RunPackFile> AddAsync(RunPack.Domain.RunPackFile entity, CancellationToken cancellationToken = default)
    {
        await _set.AddAsync(entity, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<IEnumerable<RunPack.Domain.RunPackFile>> InsertManyAsync(IEnumerable<RunPack.Domain.RunPackFile> runPackFiles, CancellationToken cancellationToken = default)
    {
        var runPackFilesList = runPackFiles.ToList();
        if (!runPackFilesList.Any())
        {
            return new List<RunPack.Domain.RunPackFile>();
        }

        await _set.AddRangeAsync(runPackFilesList, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return runPackFilesList;
    }

    public async Task<RunPack.Domain.RunPackFile> UpdateAsync(RunPack.Domain.RunPackFile entity, CancellationToken cancellationToken = default)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _set.Update(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _set.FindAsync(new object?[] { id }, cancellationToken).AsTask();
        if (entity != null)
        {
            _set.Remove(entity);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteByRunPackIdAsync(Guid runPackId, CancellationToken cancellationToken = default)
    {
        var entities = await _set
            .Where(rpf => rpf.RunPackId == runPackId)
            .ToListAsync(cancellationToken);

        if (entities.Any())
        {
            _set.RemoveRange(entities);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}