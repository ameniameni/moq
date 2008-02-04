﻿using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Castle.DynamicProxy;
using Castle.Core.Interceptor;
using System.Collections.Generic;

namespace Moq
{
	/// <summary>
	/// Provides a mock implementation of <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">Type to mock, which can be an interface or a class.</typeparam>
	/// <remarks>
	/// If the mocked <typeparamref name="T"/> is a <see cref="MarshalByRefObject"/> (such as a 
	/// Windows Forms control or another <see cref="System.ComponentModel.Component"/>-derived class) 
	/// all members will be mockeable, even if they are not virtual or abstract.
	/// <para>
	/// For regular .NET classes ("POCOs" or Plain Old CLR Objects), only abstract and virtual 
	/// members can be mocked. 
	/// </para>
	/// <para>
	/// The behavior of the mock with regards to the expectations and the actual calls is determined 
	/// by the optional <see cref="MockBehavior"/> that can be passed to the <see cref="Mock{T}(MockBehavior)"/> 
	/// constructor.
	/// </para>
	/// </remarks>
	/// <example>
	/// The following example shows setting expectations with specific values 
	/// for method invocations:
	/// <code>
	/// //setup - data
	/// var order = new Order(TALISKER, 50);
	/// var mock = new Mock&lt;IWarehouse&gt;();
	/// 
	/// //setup - expectations
	/// mock.Expect(x => x.HasInventory(TALISKER, 50)).Returns(true);
	/// 
	/// //exercise
	/// order.Fill(mock.Object);
	/// 
	/// //verify
	/// Assert.IsTrue(order.IsFilled);
	/// </code>
	/// The following example shows how to use the <see cref="It"/> class 
	/// to specify conditions for arguments instead of specific values:
	/// <code>
	/// //setup - data
	/// var order = new Order(TALISKER, 50);
	/// var mock = new Mock&lt;IWarehouse&gt;();
	/// 
	/// //setup - expectations
	/// //shows how to expect a value within a range
	/// mock.Expect(x => x.HasInventory(It.IsAny&lt;string&gt;(), It.IsInRange(0, 100, Range.Inclusive))).Returns(false);
	/// 
	/// //shows how to throw for unexpected calls. contrast with the "verify" approach of other mock libraries.
	/// mock.Expect(x => x.Remove(It.IsAny&lt;string&gt;(), It.IsAny&lt;int&gt;())).Throws(new InvalidOperationException());
	/// 
	/// //exercise
	/// order.Fill(mock.Object);
	/// 
	/// //verify
	/// Assert.IsFalse(order.IsFilled);
	/// </code>
	/// </example>
	public class Mock<T> : IVerifiable
		where T : class
	{
		static readonly ProxyGenerator generator = new ProxyGenerator();
		Interceptor interceptor;
		T instance;
		RemotingProxy remotingProxy;
		MockBehavior behavior;

		/// <summary>
		/// Initializes an instance of the mock with a specific <see cref="MockBehavior">behavior</see> with 
		/// the given constructor arguments for the class.
		/// </summary>
		/// <remarks>
		/// The mock will try to find the best match constructor given the constructor arguments, and invoke that 
		/// to initialize the instance. This applies only to classes, not interfaces.
		/// <para>
		/// <b>Note:</b> For a <see cref="MarshalByRefObject"/> derived class, any calls done in the constructor itself 
		/// will not go through the proxied mock and will instead be direct invocations in the underlying 
		/// object. This is known limitation.
		/// </para>
		/// </remarks>
		/// <example>
		/// <code>var mock = new Mock&lt;MyProvider&gt;(someArgument, 25);</code>
		/// </example>
		public Mock(MockBehavior behavior, params object[] args)
		{
			if (args == null) args = new object[0];

			this.behavior = behavior;
			interceptor = new Interceptor(behavior, typeof(T));

			try
			{
				if (typeof(MarshalByRefObject).IsAssignableFrom(typeof(T)))
				{
					remotingProxy = new RemotingProxy(typeof(T), x => interceptor.Intercept(x), args);
					// TODO: invoke ctor explicitly?
					instance = (T)remotingProxy.GetTransparentProxy();
				}
				else if (typeof(T).IsInterface)
				{
					if (args.Length > 0)
						throw new ArgumentException(Properties.Resources.ConstructorArgsForInterface);

					instance = generator.CreateInterfaceProxyWithoutTarget<T>(interceptor);
				}
				else
				{
					try
					{
						if (args.Length > 0)
						{
							var generatedType = generator.ProxyBuilder.CreateClassProxy(typeof(T), new ProxyGenerationOptions());
							instance = (T)Activator.CreateInstance(generatedType,
								new object[] { new IInterceptor[] { interceptor } }.Concat(args).ToArray());
						}
						else
						{
							instance = generator.CreateClassProxy<T>(interceptor);
						}
					}
					catch (TypeLoadException tle)
					{
						throw new ArgumentException(Properties.Resources.InvalidMockClass, tle);
					}
				}

			}
			catch (MissingMethodException mme)
			{
				throw new ArgumentException(Properties.Resources.ConstructorNotFound, mme);
			}

			Mock.RegisterMock(this, instance);
		}

		/// <summary>
		/// Initializes an instance of the mock with <see cref="MockBehavior.Default">default behavior</see> and with 
		/// the given constructor arguments for the class. (Only valid when <typeparamref name="T"/> is a class)
		/// </summary>
		/// <remarks>
		/// The mock will try to find the best match constructor given the constructor arguments, and invoke that 
		/// to initialize the instance. This applies only for classes, not interfaces.
		/// <para>
		/// <b>Note:</b> For a <see cref="MarshalByRefObject"/> derived class, any calls done in the constructor itself 
		/// will not go through the proxied mock and will instead be direct invocations in the underlying 
		/// object. This is known limitation.
		/// </para>
		/// </remarks>
		/// <example>
		/// <code>var mock = new Mock&lt;MyProvider&gt;(someArgument, 25);</code>
		/// </example>
		public Mock(params object[] args) : this(MockBehavior.Default, args) { }

		/// <summary>
		/// Initializes an instance of the mock with <see cref="MockBehavior.Default">default behavior</see>.
		/// </summary>
		/// <example>
		/// <code>var mock = new Mock&lt;IFormatProvider&gt;();</code>
		/// </example>
		public Mock() : this(MockBehavior.Default) { }

		/// <summary>
		/// Initializes an instance of the mock with the specified <see cref="MockBehavior">behavior</see>.
		/// </summary>
		/// <example>
		/// <code>var mock = new Mock&lt;IFormatProvider&gt;(MockBehavior.Relaxed);</code>
		/// </example>
		public Mock(MockBehavior behavior) : this(behavior, new object[0]) {}

		/// <summary>
		/// Exposes the mocked object instance.
		/// </summary>
		public T Object
		{
			get
			{
				return instance;
			}
		}

		/// <devdoc>
		/// Used for testing the mock factory.
		/// </devdoc>
		internal MockBehavior Behavior { get { return behavior; } }

		/// <summary>
		/// Sets an expectation on the mocked type for a call to 
		/// to a void method.
		/// </summary>
		/// <remarks>
		/// If more than one expectation is set for the same method or property, 
		/// the latest one wins and is the one that will be executed.
		/// </remarks>
		/// <param name="expression">Lambda expression that specifies the expected method invocation.</param>
		/// <example>
		/// <code>
		/// var mock = new Mock&lt;IProcessor&gt;();
		/// mock.Expect(x =&gt; x.Execute("ping"));
		/// </code>
		/// </example>
		public ICall Expect(Expression<Action<T>> expression)
		{
			return (ICall)ExpectImpl(
				expression,
				(original, method, args) => new MethodCall(original, method, args));
		}

		/// <summary>
		/// Sets an expectation on the mocked type for a call to 
		/// to a value returning method.
		/// </summary>
		/// <remarks>
		/// If more than one expectation is set for the same method or property, 
		/// the latest one wins and is the one that will be executed.
		/// </remarks>
		/// <param name="expression">Lambda expression that specifies the expected method invocation.</param>
		/// <example>
		/// <code>
		/// mock.Expect(x =&gt; x.HasInventory("Talisker", 50)).Returns(true);
		/// </code>
		/// </example>
		/// <seealso cref="ICallReturn{TResult}.Returns(TResult)"/>
		/// <seealso cref="ICallReturn{TResult}.Returns(Func{TResult})"/>
		public ICallReturn<TResult> Expect<TResult>(Expression<Func<T, TResult>> expression)
		{
			return (ICallReturn<TResult>)ExpectImpl(
				expression,
				(original, method, args) => new MethodCallReturn<TResult>(original, method, args));
		}

		/// <summary>
		/// Verifies that all verifiable expectations have been met.
		/// </summary>
		/// <example>
		/// This example sets up an expectation and marks it as verifiable. After 
		/// the mock is used, a <see cref="Verify"/> call is issued on the mock 
		/// to ensure the method in the expectation was invoked:
		/// <code>
		/// var mock = new Mock&lt;IWarehouse&gt;();
		/// mock.Expect(x =&gt; x.HasInventory(TALISKER, 50)).Verifiable().Returns(true);
		/// ...
		/// // other test code
		/// ...
		/// // Will throw if the test code has didn't call HasInventory.
		/// mock.Verify();
		/// </code>
		/// </example>
		/// <exception cref="MockException">Not all verifiable expectations were met.</exception>
		public void Verify()
		{
			try
			{
				interceptor.Verify();
			}
			catch (Exception ex)
			{
				// Rethrow resetting the call-stack so that 
				// callers see the exception as happening at 
				// this call site.
				throw ex;
			}
		}

		/// <summary>
		/// Verifies all expectations regardless of whether they have 
		/// been flagged as verifiable.
		/// </summary>
		/// <example>
		/// This example sets up an expectation without marking it as verifiable. After 
		/// the mock is used, a <see cref="VerifyAll"/> call is issued on the mock 
		/// to ensure that all expectations are met:
		/// <code>
		/// var mock = new Mock&lt;IWarehouse&gt;();
		/// mock.Expect(x =&gt; x.HasInventory(TALISKER, 50)).Returns(true);
		/// ...
		/// // other test code
		/// ...
		/// // Will throw if the test code has didn't call HasInventory, even 
		/// // that expectation was not marked as verifiable.
		/// mock.VerifyAll();
		/// </code>
		/// </example>
		/// <exception cref="MockException">At least one expectation was not met.</exception>
		public void VerifyAll()
		{
			try
			{
				interceptor.VerifyAll();
			}
			catch (Exception ex)
			{
				// Rethrow resetting the call-stack so that 
				// callers see the exception as happening at 
				// this call site.
				throw ex;
			}
		}

		private IProxyCall ExpectImpl(
			Expression expression,
			Func<Expression, MethodInfo, Expression[], IProxyCall> factory)
		{
			Guard.ArgumentNotNull(expression, "expression");

			LambdaExpression lambda = (LambdaExpression)expression;
			MethodCallExpression methodCall = lambda.Body as MethodCallExpression;
			MemberExpression propField = lambda.Body as MemberExpression;

			IProxyCall result = null;

			if (methodCall != null)
			{
				VerifyCanOverride(expression, methodCall.Method);
				result = factory(expression, methodCall.Method, methodCall.Arguments.ToArray());
				interceptor.AddCall(result);
			}
			else if (propField != null)
			{
				PropertyInfo prop = propField.Member as PropertyInfo;
				FieldInfo field = propField.Member as FieldInfo;
				if (prop != null)
				{
					// If property is not readable, the compiler won't let 
					// the user to specify it in the lambda :)
					// This is just reassuring that in case they build the 
					// expression tree manually?
					if (!prop.CanRead)
					{
						throw new ArgumentException(String.Format(
							Properties.Resources.PropertyNotReadable,
							prop.DeclaringType.Name,
							prop.Name), "expression");
					}

					VerifyCanOverride(expression, prop.GetGetMethod());
					result = factory(expression, prop.GetGetMethod(), new Expression[0]);
					interceptor.AddCall(result);
				}
				else if (field != null)
				{
					throw new NotSupportedException(Properties.Resources.FieldsNotSupported);
				}
			}

			if (result == null)
			{
				throw new NotSupportedException(expression.ToString());
			}

			return result;
		}

		private void VerifyCanOverride(Expression expectation, MethodInfo methodInfo)
		{
			if (!methodInfo.IsVirtual && !methodInfo.DeclaringType.IsMarshalByRef)
				throw new ArgumentException(
					String.Format(Properties.Resources.ExpectationOnNonOverridableMember,
					expectation.ToString()));
		}

		// NOTE: known issue. See https://connect.microsoft.com/VisualStudio/feedback/ViewFeedback.aspx?FeedbackID=318122
		//public static implicit operator TInterface(Mock<TInterface> mock)
		//{
		//    // TODO: doesn't work as expected but ONLY with interfaces :S
		//    return mock.Object;
		//}

		//public static explicit operator TInterface(Mock<TInterface> mock)
		//{
		//    // TODO: doesn't work as expected but ONLY with interfaces :S
		//    throw new NotImplementedException();
		//}
	}

	/// <summary>
	/// Mock helper methods.
	/// </summary>
	public sealed class Mock
	{
		private static IDictionary<object, object> mocks = new Dictionary<object, object>(new ReferenceEqualsComparer());

		internal static void RegisterMock(object mock, object mocked)
		{
			mocks.Add(mocked, mock);
		}

		/// <summary>
		/// if mocked was instantiated from Mock&lt;T&gt;,
		/// it gets the Mock&lt;T&gt; that manage it.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="mocked"></param>
		/// <returns></returns>
		public static Mock<T> Get<T>(T mocked)
			where T : class
		{
			object mock = null;
			if (mocks.TryGetValue(mocked, out mock))
			{
				return mock as Mock<T>;
			}
			else
			{
				throw new ArgumentException("Not instantiated from MoQ.", "mocked");
			}
		}
	}
}