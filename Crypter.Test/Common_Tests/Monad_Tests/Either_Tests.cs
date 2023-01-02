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

using Crypter.Common.Monads;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Crypter.Test.Common_Tests.Monad_Tests
{
   [TestFixture]
   [Parallelizable]
   internal class Either_Tests
   {
      [Test]
      public void Default_Constructor_Returns_Neither()
      {
         var sut = new Either<Unit, string>();

         sut.DoLeftOrNeither(
            _ => Assert.Fail(),
            () => Assert.IsTrue(true));
         sut.DoRight(_ => Assert.Fail());

         bool isNeither = sut.Match(
            left: _ => false,
            right: _ => false,
            neither: true);

         Assert.IsTrue(isNeither);

         Assert.IsTrue(sut.IsNeither);
         Assert.IsFalse(sut.IsRight);
         Assert.IsFalse(sut.IsLeft);
      }

      [Test]
      public void StaticNeither_Works()
      {
         var sut = Either<Unit, string>.Neither;

         sut.DoLeftOrNeither(
            _ => Assert.Fail(),
            () => Assert.True(true));
         sut.DoRight(_ => Assert.Fail());

         bool isNeither = sut.Match(
            left: _ => false,
            right: _ => false,
            neither: true);

         Assert.IsTrue(isNeither);

         Assert.IsTrue(sut.IsNeither);
         Assert.IsFalse(sut.IsRight);
         Assert.IsFalse(sut.IsLeft);
      }

      [Test]
      public void FromRight_Works()
      {
         string value = "test";
         bool doRightInvoked = false;

         var sut = Either<Unit, string>.FromRight(value);
         sut.DoLeftOrNeither(() => Assert.Fail());
         sut.DoRight(right =>
         {
            doRightInvoked = true;
            Assert.AreEqual(value, right);
         });

         Assert.IsTrue(doRightInvoked);

         Assert.IsTrue(sut.IsRight);
         Assert.IsFalse(sut.IsLeft);
         Assert.IsFalse(sut.IsNeither);

         Assert.AreEqual(value, sut.RightOrDefault("bar"));
         Assert.AreEqual(Unit.Default, sut.LeftOrDefault(Unit.Default));
      }

      [Test]
      public void FromRight_Returns_Neither_If_Null_Provided()
      {
         var sut = Either<Unit, string>.FromRight(null);
         sut.DoLeftOrNeither(
            _ => Assert.Fail(),
            () => Assert.IsTrue(true));

         sut.DoRight(_ => Assert.Fail());

         bool isNeither = sut.Match(
            left: _ => false,
            right: _ => false,
            neither: true);

         Assert.IsTrue(isNeither);

         Assert.IsTrue(sut.IsNeither);
         Assert.IsFalse(sut.IsRight);
         Assert.IsFalse(sut.IsLeft);

         Assert.AreEqual("bar", sut.RightOrDefault("bar"));
         Assert.AreEqual(Unit.Default, sut.LeftOrDefault(Unit.Default));
      }

      [Test]
      public void FromLeft_Works()
      {
         int value = 5;
         bool doLeftInvoked = false;

         var sut = Either<int, Unit>.FromLeft(value);
         sut.DoLeftOrNeither(left =>
         {
            doLeftInvoked = true;
            Assert.AreEqual(value, left);
         },
         () => Assert.Fail());

         sut.DoRight(_ => Assert.Fail());

         Assert.IsTrue(doLeftInvoked);

         Assert.IsTrue(sut.IsLeft);
         Assert.IsFalse(sut.IsRight);
         Assert.IsFalse(sut.IsNeither);

         Assert.AreEqual(5, sut.LeftOrDefault(123));
         Assert.AreEqual(Unit.Default, sut.RightOrDefault(Unit.Default));
      }

      [Test]
      public void FromLeft_Returns_Neither_If_Null_Provided()
      {
         var sut = Either<object, Unit>.FromLeft(null);
         sut.DoLeftOrNeither(
            _ => Assert.Fail(),
            () => Assert.IsTrue(true));
         sut.DoRight(_ => Assert.Fail());

         bool isNeither = sut.Match(
            left: _ => false,
            right: _ => false,
            neither: true);

         Assert.IsTrue(isNeither);

         Assert.IsTrue(sut.IsNeither);
         Assert.IsFalse(sut.IsRight);
         Assert.IsFalse(sut.IsLeft);

         object testObject = new object();
         Assert.AreEqual(testObject, sut.LeftOrDefault(testObject));
         Assert.AreEqual(Unit.Default, sut.RightOrDefault(Unit.Default));
      }

      [Test]
      public async Task FromRightAsync_Works()
      {
         string value = "test";
         Task<string> task = Task.FromResult(value);

         var eitherTask = Either<Unit, string>.FromRightAsync(task);
         var sut = await eitherTask;

         bool doRightInvoked = false;
         sut.DoLeftOrNeither(() => Assert.Fail());
         sut.DoRight(right =>
         {
            doRightInvoked = true;
            Assert.AreEqual(value, right);
         });

         Assert.IsTrue(doRightInvoked);

         Assert.IsTrue(sut.IsRight);
         Assert.IsFalse(sut.IsLeft);
         Assert.IsFalse(sut.IsNeither);

         Assert.AreEqual(value, sut.RightOrDefault("bar"));
         Assert.AreEqual(Unit.Default, sut.LeftOrDefault(Unit.Default));
      }

      [Test]
      public async Task FromRightAsync_Neithers_If_Null()
      {
         string value = null;
         Task<string> task = Task.FromResult(value);

         var eitherTask = Either<Unit, string>.FromRightAsync(task);
         var sut = await eitherTask;

         sut.DoLeftOrNeither(
            _ => Assert.Fail(),
            () => Assert.IsTrue(true));

         sut.DoRight(_ => Assert.Fail());

         bool isNeither = sut.Match(
           left: _ => false,
           right: _ => false,
           neither: true);

         Assert.IsTrue(isNeither);

         Assert.IsTrue(sut.IsNeither);
         Assert.IsFalse(sut.IsRight);
         Assert.IsFalse(sut.IsLeft);
      }

      [Test]
      public async Task FromRightAsync_Works_With_Default_Left()
      {
         string value = null;
         Task<string> task = Task.FromResult(value);

         var eitherTask = Either<int, string>.FromRightAsync(task, 3);
         var sut = await eitherTask;

         bool doLeftInvoked = true;
         sut.DoLeftOrNeither(left =>
         {
            doLeftInvoked = true;
            Assert.AreEqual(3, left);
         },
         () => Assert.Fail());

         sut.DoRight(_ => Assert.Fail());

         Assert.IsTrue(doLeftInvoked);

         Assert.IsTrue(sut.IsLeft);
         Assert.IsFalse(sut.IsRight);
         Assert.IsFalse(sut.IsNeither);

         Assert.AreEqual(3, sut.LeftOrDefault(123));
         Assert.AreEqual("three", sut.RightOrDefault("three"));
      }

      [Test]
      public async Task MatchAsync_Works_For_Left_Async()
      {
         var leftEither = Either<int, string>.FromLeft(4);
         string leftSut = await leftEither.MatchAsync(
            async left => await Task.FromResult("left"),
            right => "right",
            "neither");

         Assert.AreEqual("left", leftSut);

         var rightEither = Either<int, string>.FromRight("foo");
         string rightSut = await rightEither.MatchAsync(
            async left => await Task.FromResult("left"),
            right => "right",
            "neither");

         Assert.AreEqual("right", rightSut);

         var neitherEither = Either<int, string>.Neither;
         string neitherSut = await neitherEither.MatchAsync(
            async left => await Task.FromResult("left"),
            right => "right",
            "neither");

         Assert.AreEqual("neither", neitherSut);
      }

      [Test]
      public async Task MatchAsync_Works_For_Right_Async()
      {
         var leftEither = Either<int, string>.FromLeft(4);
         string leftSut = await leftEither.MatchAsync(
            left => "left",
            async right => await Task.FromResult("right"),
            "neither");

         Assert.AreEqual("left", leftSut);

         var rightEither = Either<int, string>.FromRight("foo");
         string rightSut = await rightEither.MatchAsync(
            left => "left",
            async right => await Task.FromResult("right"),
            "neither");

         Assert.AreEqual("right", rightSut);

         var neitherEither = Either<int, string>.Neither;
         string neitherSut = await neitherEither.MatchAsync(
            left => "left",
            async right => await Task.FromResult("right"),
            "neither");

         Assert.AreEqual("neither", neitherSut);
      }

      [Test]
      public async Task MatchAsync_Works_For_Left_And_Right_Async()
      {
         var leftEither = Either<int, string>.FromLeft(4);
         string leftSut = await leftEither.MatchAsync(
            async left => await Task.FromResult("left"),
            async right => await Task.FromResult("right"),
            "neither");

         Assert.AreEqual("left", leftSut);

         var rightEither = Either<int, string>.FromRight("foo");
         string rightSut = await rightEither.MatchAsync(
            async left => await Task.FromResult("left"),
            async right => await Task.FromResult("right"),
            "neither");

         Assert.AreEqual("right", rightSut);

         var neitherEither = Either<int, string>.Neither;
         string neitherSut = await neitherEither.MatchAsync(
            async left => await Task.FromResult("left"),
            async right => await Task.FromResult("right"),
            "neither");

         Assert.AreEqual("neither", neitherSut);
      }

      [Test]
      public void Select_Works_For_Right_Either()
      {
         string value = "test";
         Either<Unit, string> sut = value;

         var eitherRightUppercase = sut.Select(x => x.ToUpper());
         Assert.IsTrue(eitherRightUppercase.IsRight);
         eitherRightUppercase.DoRight(x => Assert.AreEqual(value.ToUpper(), x));
      }

      [Test]
      public void Select_Works_For_Left_Either()
      {
         string value = "test";
         Either<string, int> sut = value;

         var eitherLeft = sut.Select(x => x == 5);
         Assert.IsTrue(eitherLeft.IsLeft);
         eitherLeft.DoLeftOrNeither(
            left => Assert.AreEqual(value, left),
            () => Assert.Fail());
      }

      [Test]
      public void Select_Works_For_Neither_Either()
      {
         var sut = Either<Unit, string>.Neither;

         var eitherNeither = sut.Select(x => x == "foo");
         Assert.IsTrue(eitherNeither.IsNeither);
      }

      [Test]
      public void Where_Works_For_Right_Either()
      {
         string value = "test";
         Either<Unit, string> sut = value;

         var eitherRight = sut.Where(x => x == value);
         Assert.IsTrue(eitherRight.IsRight);
         eitherRight.DoRight(x => Assert.AreEqual(value, x));

         var eitherNeither = sut.Where(x => x == "foo");
         Assert.IsTrue(eitherNeither.IsNeither);
      }

      [Test]
      public void Where_Works_For_Left_Either()
      {
         string value = "test";
         Either<string, int> sut = value;

         var eitherNeither = sut.Where(x => x == 3);
         Assert.IsTrue(eitherNeither.IsNeither);
      }

      [Test]
      public void Where_Works_For_Neither_Either()
      {
         var sut = Either<int, string>.Neither;

         var eitherNeither = sut.Where(x => x == "test");
         Assert.IsTrue(eitherNeither.IsNeither);
      }

      [Test]
      public void Implicit_Right_Operator_Works()
      {
         string value = "test";
         Either<Unit, string> sut = value;
         Assert.IsTrue(sut.IsRight);
      }

      [Test]
      public void Implicit_Right_Operator_Neithers_If_Null()
      {
         string value = null;
         Either<Unit, string> sut = value;
         Assert.IsTrue(sut.IsNeither);
      }

      [Test]
      public void Implicit_Left_Operator_Works()
      {
         int value = 5;
         Either<int, Unit> sut = value;
         Assert.IsTrue(sut.IsLeft);
      }

      [Test]
      public void Implicit_Left_Operator_Neithers_If_Null()
      {
         string value = null;
         Either<string, Unit> sut = value;
         Assert.IsTrue(sut.IsNeither);
      }
   }
}
