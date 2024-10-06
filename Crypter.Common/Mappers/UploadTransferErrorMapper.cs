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

using Crypter.Common.Contracts.Features.Transfer;

namespace Crypter.Common.Mappers;

public static class UploadTransferErrorMapper
{
    public static UploadTransferError ConvertToUploadTransferError(this UploadMultipartFileTransferError error)
    {
#pragma warning disable CS8524
        return error switch
        {
            UploadMultipartFileTransferError.UnknownError => UploadTransferError.UnknownError,
            UploadMultipartFileTransferError.OutOfSpace => UploadTransferError.OutOfSpace,
            UploadMultipartFileTransferError.NotFound => UploadTransferError.UnknownError,
            UploadMultipartFileTransferError.AggregateTooLarge => UploadTransferError.TooLarge,
        };
#pragma warning restore CS8524
    }

    public static UploadTransferError ConvertToUploadTransferError(this FinalizeMultipartFileTransferError error)
    {
#pragma warning disable CS8524
        return error switch
        {
            FinalizeMultipartFileTransferError.UnknownError => UploadTransferError.UnknownError,
            FinalizeMultipartFileTransferError.NotFound => UploadTransferError.UnknownError,
        };
#pragma warning restore CS8524
    }
}
