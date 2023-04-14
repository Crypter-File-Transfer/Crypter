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

using Crypter.Core;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Crypter.Test.Core_Tests
{
   internal class TestDataContext : DataContext
   {
      private readonly string _databaseName;

      public TestDataContext(string databaseName)
         : base()
      {
         _databaseName = databaseName;
      }

      protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
         => optionsBuilder.UseNpgsql($"Host=localhost;Database={_databaseName};Username=postgres;Password=DEFAULT_PASSWORD");

      public void EnsureCreated()
         => Database.EnsureCreated();

      public void Reset()
      {
         List<string> tableNames = Model.GetEntityTypes()
          .Select(x => $" {DataContext.SchemaName}.\"{x.GetTableName()}\"")
          .Distinct()
          .ToList();

         StringBuilder query = new StringBuilder("TRUNCATE TABLE");
         query.AppendJoin(',', tableNames)
              .Append(';');

         Database.ExecuteSqlRaw(query.ToString());
      }
   }
}
