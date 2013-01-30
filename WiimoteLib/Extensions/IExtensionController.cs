using System;
using System.Collections.Generic;
using System.Text;

namespace WiimoteLib.Extensions
{
	public interface IExtensionController<T>
	{
		T State;
	}
}
