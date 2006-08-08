#region Copyright & License
//
// Copyright 2001-2006 The Apache Software Foundation
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

// .NET Compact Framework 1.0 has no support for ASP.NET
// SSCLI 1.0 has no support for ASP.NET
#if !NETCF && !SSCLI 

using System.IO;
using System.Web;
using log4net.Core;
using log4net.Util;

namespace log4net.Layout.Pattern
{
	internal sealed class AspNetRequestPatternConverter : AspNetPatternConverter
	{
		protected override void Convert(TextWriter writer, LoggingEvent loggingEvent, HttpContext httpContext)
		{
			if (httpContext.Request != null)
			{
				if (Option != null)
				{
					WriteObject(writer, loggingEvent.Repository, httpContext.Request.Params[Option]);
				}
				else
				{
					WriteObject(writer, loggingEvent.Repository, httpContext.Request);
				}
			}
			else
			{
				writer.Write(SystemInfo.NotAvailableText);
			}
		}
	}
}

#endif