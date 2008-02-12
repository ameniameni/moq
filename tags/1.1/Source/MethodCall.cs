﻿using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using Castle.Core.Interceptor;

namespace Moq
{
	internal class MethodCall : ICall, IProxyCall
	{
		MethodInfo method;
		Exception exception;
		Action callback;
		IMatcher[] argumentMatchers;

		public MethodCall(MethodInfo method, params Expression[] arguments)
		{
			this.method = method;
			argumentMatchers = (from expr in arguments
							   select MatcherFactory.CreateMatcher(expr)).ToArray();
		}

		public void Throws(Exception exception)
		{
			this.exception = exception;
		}

		public ICall Callback(Action callback)
		{
			this.callback = callback;
			return this;
		}

		public bool Matches(IInvocation call)
		{
			if (call.Method == method &&
				argumentMatchers.Length == call.Arguments.Length)
			{
				for (int i = 0; i < argumentMatchers.Length; i++)
				{
					if (!argumentMatchers[i].Matches(call.Arguments[i]))
						return false;
				}

				return true;
			}

			return false;
		}

		public virtual void Execute(IInvocation call)
		{
			if (callback != null)
				callback();

			if (exception != null)
				throw exception;
		}
	}
}