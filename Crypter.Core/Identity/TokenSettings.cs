﻿/*
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

using Crypter.Common.Exceptions;

namespace Crypter.Core.Identity;

public enum SigningKeyGenerationStrategy
{
    Never,
    WhenMissing,
    Always
}

public class TokenSettings
{
    public required string Audience { get; set; }
    public required string Issuer { get; set; }
    public required int AuthenticationTokenLifetimeMinutes { get; set; }
    public required int SessionTokenLifetimeMinutes { get; set; }
    public required int DeviceTokenLifetimeDays { get; set; }
    public required bool RequirePersistentSigningKey { get; set; }
    public string? PersistentSigningKeyLocation { get; set; }
    public string? PersistentSigningKeyPassword { get; set; }
    public SigningKeyGenerationStrategy SigningKeyGenerationStrategy { get; set; } = SigningKeyGenerationStrategy.Always;

    public void Validate()
    {
        if (RequirePersistentSigningKey && string.IsNullOrWhiteSpace(PersistentSigningKeyLocation))
        {
            throw new ConfigurationException("PersistentSigningKeyLocation must be set when persistent signing key is enabled.");
        }
        else if (!RequirePersistentSigningKey && SigningKeyGenerationStrategy == SigningKeyGenerationStrategy.Never)
        {
            throw new ConfigurationException("Signing key generation must be enabled when persistent signing key is disabled.");
        }
    }
}
