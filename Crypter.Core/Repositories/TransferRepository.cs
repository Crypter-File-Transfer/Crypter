/*
 * Copyright (C) 2024 Crypter File Transfer
 *
 * This file is part of the Crypter file transfer project.
 *
 * Crypter is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * The Crypter source code is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 *
 * You can be released from the requirements of the aforementioned license
 * by purchasing a commercial license. Buying such a license is mandatory
 * as soon as you develop commercial activities involving the Crypter source
 * code without disclosing the source code of your own applications.
 *
 * Contact the current copyright holder to discuss commercial license options.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Crypter.Common.Enums;
using Crypter.Core.Settings;
using EasyMonads;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Crypter.Core.Repositories;

public interface ITransferRepository
{
    bool TransferExists(Guid id, TransferItemType itemType, TransferUserType userType);

    Maybe<FileStream> GetTransfer(Guid id, TransferItemType itemType, TransferUserType userType,
        bool deleteOnReadCompletion);

    long GetTransferSize(Guid id, TransferItemType itemType, TransferUserType userType);
    
    long GetTransferPartsSize(Guid id, TransferItemType itemType, TransferUserType userType);
    
    Task<bool> SaveTransferAsync(Guid id, TransferItemType itemType, TransferUserType userType, Stream stream);
    
    Task<bool> SaveTransferPartAsync(Guid id, TransferItemType itemType, TransferUserType userType, Stream partStream, int partPosition);

    Task<bool> JoinTransferPartsAsync(Guid id, TransferItemType itemType, TransferUserType userType);
    
    void DeleteTransfer(Guid id, TransferItemType itemType, TransferUserType userType);

    void DeleteTransferParts(Guid id, TransferItemType itemType, TransferUserType userType);
}

public static class TransferRepositoryExtensions
{
    public static void AddTransferRepository(this IServiceCollection services, Action<TransferStorageSettings> settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        services.Configure(settings);
        services.TryAddSingleton<ITransferRepository, TransferRepository>();
    }
}

public class TransferRepository : ITransferRepository
{
    private const string PartialDirectoryName = "partials";
    private readonly TransferStorageSettings _transferStorageSettings;
    private readonly ILogger<TransferRepository> _logger;

    public TransferRepository(IOptions<TransferStorageSettings> transferStorageSettings, ILogger<TransferRepository> logger)
    {
        _transferStorageSettings = transferStorageSettings.Value;
        _logger = logger;
    }

    public bool TransferExists(Guid id, TransferItemType itemType, TransferUserType userType)
    {
        string filepath = GetTransferPath(id, itemType, userType);
        return File.Exists(filepath);
    }

    public Maybe<FileStream> GetTransfer(Guid id, TransferItemType itemType, TransferUserType userType,
        bool deleteOnReadCompletion)
    {
        string filepath = GetTransferPath(id, itemType, userType);

        FileOptions fileOption = deleteOnReadCompletion
            ? FileOptions.DeleteOnClose
            : FileOptions.None;

        return File.Exists(filepath)
            ? new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, fileOption)
            : Maybe<FileStream>.None;
    }

    public long GetTransferSize(Guid id, TransferItemType itemType, TransferUserType userType)
    {
        string filepath = GetTransferPath(id, itemType, userType);
        FileInfo fileInfo = new FileInfo(filepath);
        return fileInfo.Length;
    }
    
    public long GetTransferPartsSize(Guid id, TransferItemType itemType, TransferUserType userType)
    {
        string directory = GetTransferPartsDirectory(itemType, userType, id);
        DirectoryInfo directoryInfo = new DirectoryInfo(directory);
        if (directoryInfo.Exists)
        {
            foreach (var foo in directoryInfo.EnumerateFiles())
            {
                _logger.LogError($"Logging file attributes: {foo.Name}, {foo.Length}");
            }
            
            long partSize = directoryInfo
                .EnumerateFiles()
                .Select(x => x.Length)
                .DefaultIfEmpty(0)
                .Sum(x => Convert.ToInt64(x / Math.Pow(10, 6)));
            _logger.LogError($"Part size: {partSize}");
            return partSize;
        }

        _logger.LogError("Directory not found");
        return 0;
    }
    
    public async Task<bool> SaveTransferAsync(Guid id, TransferItemType itemType, TransferUserType userType,
        Stream stream)
    {
        string directory = GetTransferDirectory(itemType, userType);
        string filepath = GetTransferPath(id, itemType, userType);

        try
        {
            EnsureDirectoryExists(directory);
            await using FileStream fileStream = File.OpenWrite(filepath);
            await stream.CopyToAsync(fileStream);
            await fileStream.FlushAsync();
        }
        catch (OperationCanceledException)
        {
            DeleteTransfer(id, itemType, userType);
            throw;
        }
        catch (Exception)
        {
            // todo - log something
            DeleteTransfer(id, itemType, userType);
            return false;
        }

        return true;
    }

    public async Task<bool> SaveTransferPartAsync(Guid id, TransferItemType itemType, TransferUserType userType, Stream partStream, int partPosition)
    {
        string directory = GetTransferPartsDirectory(itemType, userType, id);
        string filepath = Path.Join(directory, $"{partPosition}.part");
        
        try
        {
            EnsureDirectoryExists(directory);
            await using FileStream fileStream = File.OpenWrite(filepath);
            await partStream.CopyToAsync(fileStream);
            await fileStream.FlushAsync();
        }
        catch (OperationCanceledException)
        {
            _logger.LogError("Failed to save transfer part. Operation canceled.");
            DeleteTransferParts(id, itemType, userType);
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to save transfer part. Unknown exception.");
            DeleteTransferParts(id, itemType, userType);
            return false;
        }

        return true;
    }

    public async Task<bool> JoinTransferPartsAsync(Guid id, TransferItemType itemType, TransferUserType userType)
    {
        string partsDirectory = GetTransferPartsDirectory(itemType, userType, id);

        if (!Directory.Exists(partsDirectory))
        {
            return false;
        }
        
        List<string> filenames = Directory
            .EnumerateFiles(partsDirectory)
            .OrderBy(x => int.Parse(Path.GetFileNameWithoutExtension(x)))
            .ToList();

        bool nonSequentialFilenames = filenames
            .Select(x => int.Parse(Path.GetFileNameWithoutExtension(x)))
            .Where((name, index) => name != index)
            .Any();

        if (nonSequentialFilenames)
        {
            return false;
        }
        
        string filepath = GetTransferPath(id, itemType, userType);
        if (File.Exists(filepath))
        {
            return false;
        }

        try
        {
            await using FileStream fileStream = File.OpenWrite(filepath);
            foreach (string filename in filenames)
            {
                await using FileStream filePart = File.OpenRead(filename);
                await filePart.CopyToAsync(fileStream);
            }
            
            DeleteTransferParts(id, itemType, userType);
        }
        catch (OperationCanceledException)
        {
            DeleteTransfer(id, itemType, userType);
            throw;
        }
        catch (Exception)
        {
            // todo - log something
            DeleteTransfer(id, itemType, userType);
            return false;
        }

        return true;
    }
    
    public void DeleteTransfer(Guid id, TransferItemType itemType, TransferUserType userType)
    {
        string filepath = GetTransferPath(id, itemType, userType);
        if (File.Exists(filepath))
        {
            File.Delete(filepath);
        }
    }

    public void DeleteTransferParts(Guid id, TransferItemType itemType, TransferUserType userType)
    {
        string directory = GetTransferPartsDirectory(itemType, userType, id);
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private string GetTransferPath(Guid id, TransferItemType itemType, TransferUserType userType)
    {
        string directory = GetTransferDirectory(itemType, userType);
        return Path.Join(directory, id.ToString());
    }
    
    private string GetTransferDirectory(TransferItemType itemType, TransferUserType userType)
    {
        return GetBaseTransferDirectory(itemType, userType);
    }

    private string GetTransferPartsDirectory(TransferItemType itemType, TransferUserType userType, Guid itemId)
    {
        string[] directoryParts =
        [
            GetBaseTransferDirectory(itemType, userType),
            PartialDirectoryName,
            itemId.ToString()
        ];
        return Path.Join(directoryParts);
    }

    private string GetBaseTransferDirectory(TransferItemType itemType, TransferUserType userType)
    {
        string[] directoryParts =
        [
            _transferStorageSettings.Location,
            userType.ToString().ToLower(),
            itemType.ToString().ToLower()
        ];
        return Path.Join(directoryParts);
    }

    private static void EnsureDirectoryExists(string directory)
    {
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
