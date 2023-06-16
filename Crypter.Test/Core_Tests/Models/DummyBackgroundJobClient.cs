

using System;
using System.Collections.Generic;
using Hangfire;
using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.States;

namespace Crypter.Test.Core_Tests.Models
{
   /// <summary>
   /// A dummy implementation of <see cref="IBackgroundJobClient"/> that can be used for testing.
   /// </summary>
   internal sealed class DummyBackgroundJobClient :
      IBackgroundJobClient
   {
      /// <summary>
      /// Gets a list of jobs that have been created.
      /// </summary>
      /// <remarks>This is used to help with assertions related to the Enqueue extension method.</remarks>
      public List<Job> Jobs { get; set; } = new();

      public bool ChangeState(
         [NotNull] string jobId,
         [NotNull] IState state,
         [CanBeNull] string expectedState) =>
         throw new NotImplementedException("This method is not currently needed to support testing of Crypter.");

      public string Create(
         [NotNull] Job job,
         [NotNull] IState state)
      {
         Jobs.Add(job);
         return Guid.NewGuid().ToString();
      }
   }
}
