#region Copyright & License
//
// Copyright 2001-2004 The Apache Software Foundation
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion

using System;
using System.Collections;

namespace log4net.Util
{
	/// <summary>
	/// An always empty <see cref="IEnumerator"/>.
	/// </summary>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	public sealed class NullEnumerator : IEnumerator
	{
		#region Private Instance Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="NullEnumerator" /> class. 
		/// </summary>
		/// <remarks>
		/// Uses a private access modifier to enforce the singleton pattern.
		/// </remarks>
		private NullEnumerator()
		{
		}

		#endregion Private Instance Constructors
  
		#region Public Static Properties

		/// <summary>
		/// Get the singleton instance of the <see cref="NullEnumerator" />.
		/// </summary>
		/// <returns>The singleton instance of the <see cref="NullEnumerator" />.</returns>
		public static NullEnumerator Instance
		{
			get { return s_instance; }
		}

		#endregion Public Static Properties

		#region Implementation of IEnumerator

		/// <summary>
		/// Gets the current object from the enumerator.
		/// </summary>
		/// <remarks>
		/// Throws an <see cref="InvalidOperationException" /> because the 
		/// <see cref="NullDictionaryEnumerator" /> never has a current value.
		/// </remarks>
		public object Current 
		{
			get	{ throw new InvalidOperationException(); }
		}
  
		/// <summary>
		/// Test if the enumerator can advance, if so advance
		/// </summary>
		/// <returns><c>false</c> as the <see cref="NullEnumerator" /> cannot advance.</returns>
		public bool MoveNext()
		{
			return false;
		}
  
		/// <summary>
		/// Resets the enumerator back to the start.
		/// </summary>
		public void Reset() 
		{
		}

		#endregion Implementation of IEnumerator

		#region Private Static Fields

		/// <summary>
		/// The singleton instance of the <see cref="NullEnumerator" />.
		/// </summary>
		private readonly static NullEnumerator s_instance = new NullEnumerator();
  
		#endregion Private Static Fields
	}
}
