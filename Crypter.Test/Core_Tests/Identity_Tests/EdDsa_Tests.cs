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

using Crypter.Core.Identity.Tokens;
using Crypter.Crypto.Common;
using Crypter.Crypto.Providers.Default;
using NUnit.Framework;
using System.IO;
using System.Linq;
using System.Text;

namespace Crypter.Test.Core_Tests.Identity_Tests
{
    [TestFixture]
    public class EdDsa_Tests
    {
        private const string KeyPathSimple = "./simple.private";
        private const string KeyPathEncrypted = "./encrypted.private";
        private const string KeyEncriptionPassword = "Pass123";

        [Test]
        public void Can_Persist_Simple_EdDsa_Private_Key()
        {
            EdDsaAlgorithm alg = EdDsaAlgorithm.Create(new DefaultCryptoProvider());
            Assert.That(alg, Is.Not.Null);
            
            alg.TryExportPrivateKey(KeyPathSimple, null);

            Assert.That(File.Exists(KeyPathSimple));
            Assert.That(new FileInfo(KeyPathSimple).Length > 0); 
            File.Delete(KeyPathSimple);
        }

        [Test]
        public void Can_Persist_Encrypted_EdDsa_Private_Key()
        {
            EdDsaAlgorithm alg = EdDsaAlgorithm.Create(new DefaultCryptoProvider());
            Assert.That(alg, Is.Not.Null);

            alg.TryExportPrivateKey(KeyPathEncrypted, KeyEncriptionPassword);

            Assert.That(File.Exists(KeyPathEncrypted));
            Assert.That(new FileInfo(KeyPathEncrypted).Length > 0);
            File.Delete(KeyPathEncrypted);
        }

        [Test]
        public void Can_Decrypt_EdDsa_Private_Key()
        {
            ICryptoProvider cryptoProvider = new DefaultCryptoProvider();
            EdDsaAlgorithm alg = EdDsaAlgorithm.Create(cryptoProvider);
            Assert.That(alg, Is.Not.Null);

            alg.TryExportPrivateKey(KeyPathEncrypted, KeyEncriptionPassword);

            Assert.That(File.Exists(KeyPathEncrypted));

            EdDsaAlgorithm algImported = EdDsaAlgorithm.FromPrivateKeyFile(KeyPathEncrypted, KeyEncriptionPassword, cryptoProvider);

            Assert.That(algImported.KeyPair.PrivateKey.Length > 0);
            Assert.That(Enumerable.SequenceEqual(alg.KeyPair.PublicKey, algImported.KeyPair.PublicKey));
            Assert.That(Enumerable.SequenceEqual(alg.KeyPair.PrivateKey, algImported.KeyPair.PrivateKey));

            File.Delete(KeyPathEncrypted);
        }

        [Test]
        public void Can_Generate_And_Verify_EdDsa_Siganture()
        {
            string signableString = "Crypter.dev";
            byte[] toSignBytes = Encoding.UTF8.GetBytes(signableString);

            ICryptoProvider cryptoProvider = new DefaultCryptoProvider();
            EdDsaAlgorithm alg = EdDsaAlgorithm.Create(cryptoProvider);
            Assert.That(alg, Is.Not.Null);

            byte[] signature = alg.Sign(toSignBytes);

            Assert.That(alg.Verify(toSignBytes, signature));
        }
    }
}
