﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Moq.Language.Primitives;

namespace Moq.Language.Flow
{
	/// <summary>
	/// 
	/// </summary>
	public interface IOnceVerifies : IOccurrence, IVerifies, IHideObjectMembers
	{
	}
}
