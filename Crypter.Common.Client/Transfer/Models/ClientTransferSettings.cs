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

namespace Crypter.Common.Client.Transfer.Models;

public class ClientTransferSettings
{
    /// <summary>
    /// Set the limit for the maximum request body size when uploading files using a buffer.
    /// </summary>
    public short MaximumUploadBufferSizeMB { get; init; }
    
    /// <summary>
    /// Set the limit for the number of blocks in a single multipart request body.
    /// </summary>
    public short MaximumMultipartReadBlocks { get; init; }
    
    /// <summary>
    /// Set the initial number of read blocks in a single multipart request body.
    /// </summary>
    public short InitialMultipartReadBlocks { get; init; }
    
    /// <summary>
    /// Set the maximum degrees of parallelism for multipart uploads.
    /// </summary>
    public int MaximumMultipartParallelism { get; init; }
    
    /// <summary>
    /// Set the number of seconds the adaptive multipart upload algorithm will target for individual upload requests.
    /// </summary>
    public int TargetMultipartUploadMilliseconds { get; init; }
    
    /// <summary>
    /// Set the number of bytes of plaintext to read at one time when encrypting a file or message.
    /// </summary>
    public int MaxReadSize { get; init; }
    
    /// <summary>
    /// Set the maximum number of bytes to use as padding when encrypting a file or message.
    /// </summary>
    public int PadSize { get; init; }
}
