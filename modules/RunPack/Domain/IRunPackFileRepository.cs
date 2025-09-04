using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ShipMvp.Core.Abstractions;

namespace RunPack.Domain;

/// <summary>
/// Repository interface for RunPackFile entities
/// </summary>
public interface IRunPackFileRepository : IRepository<RunPackFile, Guid>
{
    /// <summary>
    /// Gets RunPackFiles by RunPack ID
    /// </summary>
    Task<IEnumerable<RunPackFile>> GetByRunPackIdAsync(Guid runPackId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets RunPackFiles by File ID
    /// </summary>
    Task<IEnumerable<RunPackFile>> GetByFileIdAsync(Guid fileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts multiple RunPackFiles in a single transaction
    /// </summary>
    Task<IEnumerable<RunPackFile>> InsertManyAsync(IEnumerable<RunPackFile> runPackFiles, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all RunPackFiles for a specific RunPack
    /// </summary>
    Task DeleteByRunPackIdAsync(Guid runPackId, CancellationToken cancellationToken = default);
}