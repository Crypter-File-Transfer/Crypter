/*
 * Copyright (C) 2023 Crypter File Transfer
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
using System.Text.Json.Serialization;

namespace Crypter.Common.Contracts;

public class ErrorResponse
{
    public string Message { get; } = "An error occurred.";
    public int Status { get; }
    public List<ErrorResponseItem> Errors { get; }

    public ErrorResponse(int status, List<ErrorResponseItem> errors)
    {
        Status = status;
        Errors = errors;
    }

    public ErrorResponse(int status, Enum errorCode)
    {
        Status = status;
        Errors = new List<ErrorResponseItem> { new ErrorResponseItem(errorCode) };
    }

    [JsonConstructor]
    public ErrorResponse(string message, int status, List<ErrorResponseItem> errors)
    {
        Message = message;
        Status = status;
        Errors = errors;
    }
}

public class ErrorResponseItem
{
    public int ErrorCode { get; }
    public string ErrorMessage { get; }

    public ErrorResponseItem(Enum errorCode)
    {
        ErrorCode = Convert.ToInt32(errorCode);
        ErrorMessage = errorCode.ToString();
    }

    [JsonConstructor]
    public ErrorResponseItem(int errorCode, string errorMessage)
    {
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }
}
