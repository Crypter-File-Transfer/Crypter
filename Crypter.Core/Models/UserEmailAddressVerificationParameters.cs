﻿/*
 * Copyright (C) 2025 Crypter File Transfer
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

namespace Crypter.Core.Models;

public class UserEmailAddressVerificationParameters
{
    public Guid UserId { get; init; }
    public Guid VerificationCode { get; init; }
    public byte[] Signature { get; init; }
    public byte[] VerificationKey { get; init; }

    public UserEmailAddressVerificationParameters(Guid userId, Guid verificationCode, byte[] signature, byte[] verificationKey)
    {
        UserId = userId;
        VerificationCode = verificationCode;
        Signature = signature;
        VerificationKey = verificationKey;
    }
}
