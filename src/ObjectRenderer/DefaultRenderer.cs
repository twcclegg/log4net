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
using System.Text;
using System.IO;
using System.Collections;

using log4net.Util;

namespace log4net.ObjectRenderer
{
	/// <summary>
	/// The default object Renderer.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The default renderer supports rendering objects and collections to strings.
	/// </para>
	/// <para>
	/// See the <see cref="RenderObject"/> method for details of the output.
	/// </para>
	/// </remarks>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	public sealed class DefaultRenderer : IObjectRenderer
	{
		private static readonly string NewLine = SystemInfo.NewLine;

		#region Constructors

		/// <summary>
		/// Default constructor
		/// </summary>
		/// <remarks>
		/// Default constructor
		/// </remarks>
		public DefaultRenderer()
		{
		}

		#endregion

		#region Implementation of IObjectRenderer

		/// <summary>
		/// Render the object <paramref name="obj"/> to a string
		/// </summary>
		/// <param name="rendererMap">The map used to lookup renderers</param>
		/// <param name="obj">The object to render</param>
		/// <param name="writer">The writer to render to</param>
		/// <remarks>
		/// <para>
		/// Render the object <paramref name="obj"/> to a string.
		/// </para>
		/// 
		/// <para>
		/// The <paramref name="rendererMap"/> parameter is
		/// provided to lookup and render other objects. This is
		/// very useful where <paramref name="obj"/> contains
		/// nested objects of unknown type. The <see cref="RendererMap.FindAndRender"/>
		/// method can be used to render these objects.
		/// </para>
		/// 
		/// <para>The default renderer supports rendering objects to strings as follows:</para>
		/// 
		/// <list type="table">
		///		<listheader>
		///			<term>Value</term>
		///			<description>Rendered String</description>
		///		</listheader>
		///		<item>
		///			<term><c>null</c></term>
		///			<description>
		///			<para>"(null)"</para>
		///			</description>
		///		</item>
		///		<item>
		///			<term><see cref="Array"/></term>
		///			<description>
		///			<para>
		///			For a one dimensional array this is the
		///			array type name, an open brace, followed by a comma
		///			separated list of the elements (using the appropriate
		///			renderer), followed by a close brace. For example:
		///			<c>int[] {1, 2, 3}</c>.
		///			</para>
		///			<para>
		///			If the array is not one dimensional the 
		///			<c>Array.ToString()</c> is returned.
		///			</para>
		///			</description>
		///		</item>
		///		<item>
		///			<term><see cref="ICollection"/></term>
		///			<description>
		///			<para>
		///			Rendered as an open brace, followed by a comma
		///			separated list of the elements (using the appropriate
		///			renderer), followed by a close brace. For example:
		///			<c>{a, b, c}</c>.
		///			</para>
		///			</description>
		///		</item>		
		///		<item>
		///			<term><see cref="DictionaryEntry"/></term>
		///			<description>
		///			<para>
		///			Rendered as the key, an equals sign ('='), and the value (using the appropriate
		///			renderer). For example: <c>key=value</c>.
		///			</para>
		///			</description>
		///		</item>		
		///		<item>
		///			<term>other</term>
		///			<description>
		///			<para><c>Object.ToString()</c></para>
		///			</description>
		///		</item>
		/// </list>
		/// </remarks>
		public void RenderObject(RendererMap rendererMap, object obj, TextWriter writer)
		{
			if (rendererMap == null)
			{
				throw new ArgumentNullException("rendererMap");
			}

			if (obj == null)
			{
				writer.Write("(null)");
			}
			else if (obj is Array)
			{
				RenderArray(rendererMap, (Array)obj, writer);
			}
			else if (obj is ICollection)
			{
				RenderCollection(rendererMap, (ICollection)obj, writer);
			}
			else if (obj is DictionaryEntry)
			{
				RenderDictionaryEntry(rendererMap, (DictionaryEntry)obj, writer);
			}
			else
			{
				string str = obj.ToString();
				writer.Write( (str==null) ? "(null)" : str );
			}
		}

		#endregion

		/// <summary>
		/// Render the array argument into a string
		/// </summary>
		/// <param name="rendererMap">The map used to lookup renderers</param>
		/// <param name="array">the array to render</param>
		/// <param name="writer">The writer to render to</param>
		/// <remarks>
		/// <para>
		/// For a one dimensional array this is the
		///	array type name, an open brace, followed by a comma
		///	separated list of the elements (using the appropriate
		///	renderer), followed by a close brace. For example:
		///	<c>int[] {1, 2, 3}</c>.
		///	</para>
		///	<para>
		///	If the array is not one dimensional the 
		///	<c>Array.ToString()</c> is returned.
		///	</para>
		/// </remarks>
		private void RenderArray(RendererMap rendererMap, Array array, TextWriter writer)
		{
			if (array.Rank != 1)
			{
				writer.Write(array.ToString());
			}
			else
			{
				writer.Write(array.GetType().Name + " {");
				int len = array.Length;

				if (len > 0)
				{
					rendererMap.FindAndRender(array.GetValue(0), writer);
					for(int i=1; i<len; i++)
					{
						writer.Write(", ");
						rendererMap.FindAndRender(array.GetValue(i), writer);
					}
				}
				writer.Write("}");
			}
		}

		/// <summary>
		/// Render the collection argument into a string
		/// </summary>
		/// <param name="rendererMap">The map used to lookup renderers</param>
		/// <param name="collection">the collection to render</param>
		/// <param name="writer">The writer to render to</param>
		/// <remarks>
		/// <para>
		/// Rendered as an open brace, followed by a comma
		///	separated list of the elements (using the appropriate
		///	renderer), followed by a close brace. For example:
		///	<c>{a, b, c}</c>.
		///	</para>
		/// </remarks>
		private void RenderCollection(RendererMap rendererMap, ICollection collection, TextWriter writer)
		{
			writer.Write("{");

			if (collection.Count > 0)
			{
				IEnumerator enumerator = collection.GetEnumerator();
				if (enumerator != null && enumerator.MoveNext())
				{
					rendererMap.FindAndRender(enumerator.Current, writer);

					while(enumerator.MoveNext())
					{
						writer.Write(", ");
						rendererMap.FindAndRender(enumerator.Current, writer);
					}
				}
			}

			writer.Write("}");
		}

		/// <summary>
		/// Render the DictionaryEntry argument into a string
		/// </summary>
		/// <param name="rendererMap">The map used to lookup renderers</param>
		/// <param name="entry">the DictionaryEntry to render</param>
		/// <param name="writer">The writer to render to</param>
		/// <remarks>
		/// <para>
		/// Render the key, an equals sign ('='), and the value (using the appropriate
		///	renderer). For example: <c>key=value</c>.
		///	</para>
		/// </remarks>
		private void RenderDictionaryEntry(RendererMap rendererMap, DictionaryEntry entry, TextWriter writer)
		{
			rendererMap.FindAndRender(entry.Key, writer);
			writer.Write("=");
			rendererMap.FindAndRender(entry.Value, writer);
		}	
	}
}
