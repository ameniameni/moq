﻿using System;
using System.Linq.Expressions;
using Xunit;

namespace Moq.Tests
{
	public class MockFixture
	{
		[Fact]
		public void CreatesMockAndExposesInterface()
		{
			var mock = new Mock<ICloneable>();

			ICloneable cloneable = mock.Object;

			Assert.NotNull(cloneable);
		}

		[Fact]
		public void ThrowsIfNullExpectAction()
		{
			var mock = new Mock<ICloneable>();

			Assert.Throws<ArgumentNullException>(() => mock.Expect((Expression<Action<ICloneable>>)null));
		}

		[Fact]
		public void ThrowsIfNullExpectFunction()
		{
			var mock = new Mock<ICloneable>();

			Assert.Throws<ArgumentNullException>(() => mock.Expect((Expression<Func<ICloneable, string>>)null));
		}

		[Fact]
		public void ThrowsIfExpectationSetForField()
		{
			var mock = new Mock<FooBase>();

			Assert.Throws<ArgumentException>(() => mock.Expect(x => x.ValueField));
		}

		[Fact]
		public void CallParameterCanBeVariable()
		{
			int value = 5;
			var mock = new Mock<IFoo>();

			mock.Expect(x => x.Echo(value)).Returns(() => value * 2);

			Assert.Equal(value * 2, mock.Object.Echo(value));
		}

		[Fact]
		public void CallParameterCanBeMethodCall()
		{
			int value = 5;
			var mock = new Mock<IFoo>();

			mock.Expect(x => x.Echo(GetValue(value))).Returns(() => value * 2);

			Assert.Equal(value * 2, mock.Object.Echo(value * 2));
		}

		private int GetValue(int value)
		{
			return value * 2;
		}

		[Fact]
		public void ExpectsVoidCall()
		{
			var mock = new Mock<IFoo>();

			mock.Expect(x => x.Submit());

			mock.Object.Submit();
		}

		[Fact]
		public void ThrowsIfExpectationThrows()
		{
			var mock = new Mock<IFoo>();

			mock.Expect(x => x.Submit()).Throws(new FormatException());

			Assert.Throws<FormatException>(() => mock.Object.Submit());
		}

		[Fact]
		public void ThrowsIfExpectationThrowsWithGenericsExceptionType()
		{
			var mock = new Mock<IFoo>();

			mock.Expect(x => x.Submit()).Throws<FormatException>();

			Assert.Throws<FormatException>(() => mock.Object.Submit());
		}

		[Fact]
		public void ReturnsServiceFromServiceProvider()
		{
			var provider = new Mock<IServiceProvider>();

			provider.Expect(x => x.GetService(typeof(IFooService))).Returns(new FooService());

			Assert.True(provider.Object.GetService(typeof(IFooService)) is FooService);
		}

		[Fact]
		public void InvokesLastExpectationThatMatches()
		{
			var mock = new Mock<IFoo>();
			mock.Expect(x => x.Execute(It.IsAny<string>())).Throws(new ArgumentException());
			mock.Expect(x => x.Execute("ping")).Returns("I'm alive!");

			Assert.Equal("I'm alive!", mock.Object.Execute("ping"));

			Assert.Throws<ArgumentException>(() => mock.Object.Execute("asdf"));
		}

		[Fact]
		public void MockObjectIsAssignableToMockedInterface()
		{
			var mock = new Mock<IFoo>();
			Assert.True(typeof(IFoo).IsAssignableFrom(mock.Object.GetType()));
		}

		[Fact]
		public void MockObjectsEqualityIsReferenceEquals()
		{
			var mock1 = new Mock<IFoo>();
			var mock2 = new Mock<IFoo>();

			Assert.True(mock1.Object.Equals(mock1.Object));
			Assert.False(mock1.Object.Equals(mock2.Object));
		}

		[Fact]
		public void HashCodeIsDifferentForEachMock()
		{
			var mock1 = new Mock<IFoo>();
			var mock2 = new Mock<IFoo>();

			Assert.Equal(mock1.Object.GetHashCode(), mock1.Object.GetHashCode());
			Assert.Equal(mock2.Object.GetHashCode(), mock2.Object.GetHashCode());
			Assert.NotEqual(mock1.Object.GetHashCode(), mock2.Object.GetHashCode());
		}

		[Fact]
		public void ToStringIsNullOrEmpty()
		{
			var mock = new Mock<IFoo>();
			Assert.False(String.IsNullOrEmpty(mock.Object.ToString()));
		}

		[Fact(Skip = "Castle.DynamicProxy2 doesn't seem to call interceptors for ToString, GetHashCode")]
		public void OverridesObjectMethods()
		{
			var mock = new Mock<IFoo>();
			mock.Expect(x => x.GetHashCode()).Returns(1);
			mock.Expect(x => x.ToString()).Returns("foo");

			Assert.Equal("foo", mock.Object.ToString());
			Assert.Equal(1, mock.Object.GetHashCode());
		}

		[Fact]
		public void OverridesBehaviorFromAbstractClass()
		{
			var mock = new Mock<FooBase>();
			mock.CallBase = true;

			mock.Expect(x => x.Check("foo")).Returns(false);

			Assert.False(mock.Object.Check("foo"));
			Assert.True(mock.Object.Check("bar"));
		}

		[Fact]
		public void CallsUnderlyingClassEquals()
		{
			var mock = new Mock<FooOverrideEquals>();
			var mock2 = new Mock<FooOverrideEquals>();

			mock.CallBase = true;

			mock.Object.Name = "Foo";
			mock2.Object.Name = "Foo";

			Assert.True(mock.Object.Equals(mock2.Object));
		}

		[Fact]
		public void ThrowsIfSealedClass()
		{
			Assert.Throws<ArgumentException>(() => new Mock<FooSealed>());
		}

		[Fact]
		public void ThrowsIfExpectOnNonVirtual()
		{
			var mock = new Mock<FooBase>();

			Assert.Throws<ArgumentException>(() => mock.Expect(x => x.True()).Returns(false));
		}

		[Fact]
		public void OverridesPreviousExpectation()
		{
			var mock = new Mock<IFoo>();

			mock.Expect(x => x.Echo(1)).Returns(5);

			Assert.Equal(5, mock.Object.Echo(1));

			mock.Expect(x => x.Echo(1)).Returns(10);

			Assert.Equal(10, mock.Object.Echo(1));
		}

		[Fact]
		public void ConstructsObjectsWithCtorArguments()
		{
			var mock = new Mock<FooWithConstructors>(MockBehavior.Default, "Hello", 26);

			Assert.Equal("Hello", mock.Object.StringValue);
			Assert.Equal(26, mock.Object.IntValue);

			// Should also construct without args.
			mock = new Mock<FooWithConstructors>(MockBehavior.Default);

			Assert.Equal(null, mock.Object.StringValue);
			Assert.Equal(0, mock.Object.IntValue);
		}

		[Fact]
		public void ConstructsClassWithNoDefaultConstructor()
		{
			var mock = new Mock<ClassWithNoDefaultConstructor>(MockBehavior.Default, "Hello", 26);

			Assert.Equal("Hello", mock.Object.StringValue);
			Assert.Equal(26, mock.Object.IntValue);
		}

		[Fact]
		public void ConstructsClassWithNoDefaultConstructorAndNullValue()
		{
			var mock = new Mock<ClassWithNoDefaultConstructor>(MockBehavior.Default, null, 26);

			Assert.Equal(null, mock.Object.StringValue);
			Assert.Equal(26, mock.Object.IntValue);
		}

		[Fact]
		public void ThrowsIfNoMatchingConstructorFound()
		{
			Assert.Throws<ArgumentException>(() =>
			{
				Console.WriteLine(new Mock<ClassWithNoDefaultConstructor>(25, true).Object);
			});
		}

		[Fact]
		public void ThrowsIfArgumentsPassedForInterface()
		{
			Assert.Throws<ArgumentException>(() => new Mock<IFoo>(25, true));
		}

		[Fact]
		public void ThrowsOnStrictWithExpectButNoReturns()
		{
			var mock = new Mock<IFoo>(MockBehavior.Strict);

			mock.Expect(x => x.Execute("ping"));

			try
			{
				mock.Object.Execute("ping");
				Assert.True(false, "SHould throw");
			}
			catch (MockException mex)
			{
				Assert.Equal(MockException.ExceptionReason.ReturnValueRequired, mex.Reason);
			}
		}

		[Fact]
		public void ExpectsPropertySetter()
		{
			var mock = new Mock<IFoo>();

			int? value = 0;

			mock.ExpectSet(foo => foo.Value)
				.Callback(i => value = i);

			mock.Object.Value = 5;

			Assert.Equal(5, value);
		}

		[Fact]
		public void ExpectsPropertySetterWithValue()
		{
			var mock = new Mock<IFoo>();
			mock.ExpectSet(m => m.Value, 5);

			Assert.Throws<MockVerificationException>(() => mock.VerifyAll());

			mock.Object.Value = 5;

			mock.VerifyAll();
			mock.VerifySet(m => m.Value);
		}

		[Fact]
		public void ExpectsPropertySetterWithNullValue()
		{
			var mock = new Mock<IFoo>(MockBehavior.Strict);
			mock.ExpectSet(m => m.Value, null);

			Assert.Throws<MockException>(() => { mock.Object.Value = 5; });
			Assert.Throws<MockVerificationException>(() => mock.VerifyAll());

			mock.Object.Value = null;

			mock.VerifyAll();
			mock.VerifySet(m => m.Value);
		}

		[Fact]
		public void ThrowsIfPropertySetterWithWrongValue()
		{
			var mock = new Mock<IFoo>(MockBehavior.Strict);
			mock.ExpectSet(m => m.Value, 5);

			Assert.Throws<MockException>(() => { mock.Object.Value = 6; });
		}

		[Fact]
		public void ExpectsPropertyGetter()
		{
			var mock = new Mock<IFoo>();

			bool called = false;

			mock.ExpectGet(x => x.Value)
				.Callback(() => called = true)
				.Returns(25);

			Assert.Equal(25, mock.Object.Value);
			Assert.True(called);
		}

		[Fact]
		public void ThrowsIfExpectPropertySetterOnMethod()
		{
			var mock = new Mock<IFoo>();

			Assert.Throws<ArgumentException>(() => mock.ExpectSet(foo => foo.Echo(5)));
		}

		[Fact]
		public void ThrowsIfExpectPropertyGetterOnMethod()
		{
			var mock = new Mock<IFoo>();

			Assert.Throws<ArgumentException>(() => mock.ExpectGet(foo => foo.Echo(5)));
		}

		[Fact]
		public void DoesNotCallBaseClassVirtualImplementationByDefault()
		{
			var mock = new Mock<FooBase>();

			Assert.False(mock.Object.BaseCalled);
			mock.Object.BaseCall();

			Assert.False(mock.Object.BaseCalled);
		}

		[Fact]
		public void DoesNotCallBaseClassVirtualImplementationIfSpecified()
		{
			var mock = new Mock<FooBase>();

			mock.CallBase = false;

			Assert.False(mock.Object.BaseCalled);
			mock.Object.BaseCall();

			Assert.Equal(default(bool), mock.Object.BaseCall("foo"));
			Assert.False(mock.Object.BaseCalled);
		}

		[Fact]
		public void ExpectsGetIndexedProperty()
		{
			var mock = new Mock<IFoo>();

			mock.ExpectGet(foo => foo[0])
				.Returns(1);
			mock.ExpectGet(foo => foo[1])
				.Returns(2);

			Assert.Equal(1, mock.Object[0]);
			Assert.Equal(2, mock.Object[1]);
		}

		[Fact]
		public void ExpectAndExpectGetAreSynonyms()
		{
			var mock = new Mock<IFoo>();

			mock.ExpectGet(foo => foo.Value)
				.Returns(1);
			mock.Expect(foo => foo.Value)
				.Returns(2);

			Assert.Equal(2, mock.Object.Value);
		}

		[Fact]
		public void ThrowsIfExpectGetOnPropertyWithPrivateSetter()
		{
			var mock = new Mock<FooWithPrivateSetter>();

			Assert.Throws<ArgumentException>(() => mock.ExpectSet(m => m.Foo));
		}

		[Fact]
		public void ThrowsIfExpecationSetForNotOverridableMember()
		{
			var target = new Mock<Doer>();

			Assert.Throws<ArgumentException>(() => target.Expect(t => t.Do()));
		}

		[Fact]
		public void ExpectWithParamArrayEmptyMatchArguments()
		{
			string expected = "bar";
			string argument = "foo";

			var target = new Mock<IParams>();
			target.Expect(x => x.ExecuteByName(argument)).Returns(expected);

			string actual = target.Object.ExecuteByName(argument);
			Assert.Equal(expected, actual);
		}

		[Fact]
		public void ExpectWithParamArrayNotMatchDifferntLengthInArguments()
		{
			string notExpected = "bar";
			string argument = "foo";

			var target = new Mock<IParams>();
			target.Expect(x => x.ExecuteParams(argument, It.IsAny<string>())).Returns(notExpected);

			string actual = target.Object.ExecuteParams(argument);
			Assert.NotEqual(notExpected, actual);
		}

		[Fact]
		public void ExpectWithParamArrayMatchArguments()
		{
			string expected = "bar";
			string argument = "foo";

			var target = new Mock<IParams>();
			target.Expect(x => x.ExecuteParams(argument, It.IsAny<string>())).Returns(expected);

			string ret = target.Object.ExecuteParams(argument, "baz");
			Assert.Equal(expected, ret);
		}

		[Fact]
		public void ExpecteWithArrayNotMatchTwoDifferentArrayInstances()
		{
			string expected = "bar";
			string argument = "foo";

			var target = new Mock<IParams>();
			target.Expect(x => x.ExecuteArray(new string[] { argument, It.IsAny<string>() })).Returns(expected);

			string ret = target.Object.ExecuteArray(new string[] { argument, "baz" });
			Assert.Equal(null, ret);
		}

		[Fact]
		public void ExpectGetAndExpectSetMatchArguments()
		{
			var target = new Mock<IFoo>();
			target.ExpectGet(d => d.Value).Returns(1);
			target.ExpectSet(d => d.Value, 2);

			target.Object.Value = target.Object.Value + 1;

			target.VerifyAll();
		}

		// ShouldSupportByRefArguments?
		// ShouldSupportOutArguments?

		interface IDo { void Do(); }

		public class Doer : IDo
		{
			public void Do()
			{
			}
		}

		public sealed class FooSealed { }
		class FooService : IFooService { }
		interface IFooService { }

		public class FooWithPrivateSetter
		{
			public virtual string Foo { get; private set; }
		}

		public class ClassWithNoDefaultConstructor
		{
			public ClassWithNoDefaultConstructor(string stringValue, int intValue)
			{
				this.StringValue = stringValue;
				this.IntValue = intValue;
			}

			public string StringValue { get; set; }
			public int IntValue { get; set; }
		}

		public abstract class FooWithConstructors
		{
			public FooWithConstructors(string stringValue, int intValue)
			{
				this.StringValue = stringValue;
				this.IntValue = intValue;
			}

			public FooWithConstructors()
			{
			}

			public override string ToString()
			{
				return base.ToString();
			}

			public string StringValue { get; set; }
			public int IntValue { get; set; }
		}

		public class FooOverrideEquals
		{
			public string Name { get; set; }

			public override bool Equals(object obj)
			{
				return (obj is FooOverrideEquals) &&
					((FooOverrideEquals)obj).Name == this.Name;
			}

			public override int GetHashCode()
			{
				return Name.GetHashCode();
			}
		}

		public interface IFoo
		{
			int? Value { get; set; }
			int Echo(int value);
			void Submit();
			string Execute(string command);
			int this[int index] { get; set; }
		}

		public interface IParams
		{
			string ExecuteByName(string name, params object[] args);
			string ExecuteParams(params string[] args);
			string ExecuteArray(string[] args);
		}

		public abstract class FooBase
		{
			public int ValueField;
			public abstract void Do(int value);

			public virtual bool Check(string value)
			{
				return true;
			}

			public bool GetIsProtected()
			{
				return IsProtected();
			}

			protected virtual bool IsProtected()
			{
				return true;
			}

			public bool True()
			{
				return true;
			}

			public bool BaseCalled = false;

			public virtual void BaseCall()
			{
				BaseCalled = true;
			}

			public bool BaseReturnCalled = false;

			public virtual bool BaseCall(string value)
			{
				BaseReturnCalled = true;
				return default(bool);
			}
		}
	}
}