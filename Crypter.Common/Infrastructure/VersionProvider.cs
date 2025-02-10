/*
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

using Crypter.Common.Attributes;
using System.Reflection;

namespace Crypter.Common.Infrastructure
{
    public record ProductVersion(string Version, string? Hash);

    public static class AssemblyVersionProvider
    {
        private const char HASH_SEPARATOR = '+';

        public static ProductVersion? GetEntryAssemblyVersionInfo()
        {
            Assembly asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            AssemblyInformationalVersionAttribute? versionInfo = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            string? productVersion = versionInfo?.InformationalVersion;
            if (productVersion != null)
            {
                string[] versionParts = productVersion.Split(HASH_SEPARATOR);
                return versionParts.Length > 1 ?
                    new ProductVersion(versionParts[0], versionParts[1]) :
                    new ProductVersion(versionParts[0], null);
            }
            return null;
        }
    }

    public static class VersionUrlProvider
    {
        private const string RELEASE_PATH = "releases/tag";
        private const string COMMIT_PATH = "commit";

        public static string GetVersionUrl(bool isRelease, string? hash, string productVersion)
        {
            Assembly asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            VersionControlMetadataAttribute? metaData = asm.GetCustomAttribute<VersionControlMetadataAttribute>();
            string? versionUrlBase = metaData?.BaseUrl;
            
            if (versionUrlBase != null)
            {
                string versionPath = isRelease ? RELEASE_PATH : COMMIT_PATH;
                string version = isRelease ? productVersion : (hash ?? string.Empty);
                return $"{versionUrlBase}/{versionPath}/{version}";
            }
            return string.Empty;
        }
    }
}
