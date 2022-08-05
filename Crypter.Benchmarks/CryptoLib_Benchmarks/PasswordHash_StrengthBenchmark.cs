/*
 * Copyright (C) 2022 Crypter File Transfer
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

using BenchmarkDotNet.Attributes;
using Crypter.CryptoLib.Sodium;
using System.Text;

namespace Crypter.Benchmarks.CryptoLib_Benchmarks
{
   [MemoryDiagnoser]
   [Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
   [RankColumn]
   public class PasswordHash_StrengthBenchmark
   {
      private byte[] _passwordBytes;
      private byte[] _saltBytes;
      private int _hashLength = 32;

      [Params(PasswordHash.Strength.Interactive, PasswordHash.Strength.Medium, PasswordHash.Strength.Moderate, PasswordHash.Strength.Sensitive)]
      public PasswordHash.Strength HashStrength;

      [IterationSetup]
      public void IterationSetup()
      {
         _passwordBytes = Encoding.UTF8.GetBytes("password");
         _saltBytes = PasswordHash.ArgonDeriveSalt("username");
      }

      [Benchmark]
      public void HashWithVaryingStrength()
      {
         PasswordHash.ArgonHash(_passwordBytes, _saltBytes, _hashLength, HashStrength);
      }
   }
}
