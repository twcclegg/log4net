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
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

using log4net.Util;
using log4net.Layout;
using log4net.Core;

namespace log4net.Appender
{
	/// <summary>
	/// Appender that rolls log files based on size or date or both.
	/// </summary>
	/// <remarks>
	/// <para>
	/// RollingFileAppender can function as either or and do both
	/// at the same time (making size based rolling files until a data/time 
	/// boundary is crossed at which time it rolls all of those files
	/// based on the setting for <see cref="RollingStyle"/>.
	/// </para>
	/// <para>
	/// A of few additional optional features have been added:<br/>
	/// -- Attach date pattern for current log file <see cref="StaticLogFileName"/><br/>
	/// -- Backup number increments for newer files <see cref="CountDirection"/><br/>
	/// -- Infinite number of backups by file size <see cref="MaxSizeRollBackups"/>
	/// </para>
	/// <para>
	/// A few notes and warnings:  For large or infinite number of backups
	/// countDirection &gt; 0 is highly recommended, with staticLogFileName = false if
	/// time based rolling is also used -- this will reduce the number of file renamings
	/// to few or none.  Changing staticLogFileName or countDirection without clearing
	/// the directory could have nasty side effects.  If Date/Time based rolling
	/// is enabled, CompositeRollingAppender will attempt to roll existing files
	/// in the directory without a date/time tag based on the last modified date
	/// of the base log files last modification.
	/// </para>
	/// <para>
	/// A maximum number of backups based on date/time boundaries would be nice
	/// but is not yet implemented.
	/// </para>
	/// </remarks>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	/// <author>Aspi Havewala</author>
	/// <author>Douglas de la Torre</author>
	/// <author>Edward Smit</author>
	public class RollingFileAppender : FileAppender
	{
		#region Public Enums

		/// <summary>
		/// Style of rolling to use
		/// </summary>
		public enum RollingMode
		{
			/// <summary>
			/// Roll files based only on the size of the file
			/// </summary>
			Size		= 1,

			/// <summary>
			/// Roll files based only on the date
			/// </summary>
			Date		= 2,

			/// <summary>
			/// Roll files based on both the size and date of the file
			/// </summary>
			Composite	= 3
		}

		#endregion

		#region Protected Enums

		/// <summary>
		/// The code assumes that the following 'time' constants are in a increasing sequence.
		/// </summary>
		protected enum RollPoint
		{
			/// <summary>
			/// Roll the log not based on the date
			/// </summary>
			InvalidRollPoint	=-1,

			/// <summary>
			/// Roll the log for each minute
			/// </summary>
			TopOfMinute			= 0,

			/// <summary>
			/// Roll the log for each hour
			/// </summary>
			TopOfHour			= 1,

			/// <summary>
			/// Roll the log twice a day (midday and midnight)
			/// </summary>
			HalfDay				= 2,

			/// <summary>
			/// Roll the log each day (midnight)
			/// </summary>
			TopOfDay			= 3,

			/// <summary>
			/// Roll the log each week
			/// </summary>
			TopOfWeek			= 4,

			/// <summary>
			/// Roll the log each month
			/// </summary>
			TopOfMonth			= 5
		}

		#endregion Protected Enums

		#region Public Instance Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="RollingFileAppender" /> class.
		/// </summary>
		public RollingFileAppender() 
		{
			m_dateTime = new DefaultDateTime();
		}

		#endregion Public Instance Constructors

		#region Public Instance Properties

		/// <summary>
		/// Gets or sets the date pattern to be used for generating file names
		/// when rolling over on date.
		/// </summary>
		/// <value>
		/// The date pattern to be used for generating file names when rolling 
		/// over on date.
		/// </value>
		/// <remarks>
		/// <para>
		/// Takes a string in the same format as expected by 
		/// <see cref="log4net.DateFormatter.SimpleDateFormatter" />.
		/// </para>
		/// <para>
		/// This property determines the rollover schedule when rolling over
		/// on date.
		/// </para>
		/// </remarks>
		public string DatePattern
		{
			get { return m_datePattern; }
			set { m_datePattern = value; }
		}
  
		/// <summary>
		/// Gets or sets the maximum number of backup files that are kept before
		/// the oldest is erased.
		/// </summary>
		/// <value>
		/// The maximum number of backup files that are kept before the oldest is
		/// erased.
		/// </value>
		/// <remarks>
		/// <para>
		/// If set to zero, then there will be no backup files and the log file 
		/// will be truncated when it reaches <see cref="MaxFileSize"/>.  
		/// </para>
		/// <para>
		/// If a negative number is supplied then no deletions will be made.  Note 
		/// that this could result in very slow performance as a large number of 
		/// files are rolled over unless <see cref="CountDirection"/> is used.
		/// </para>
		/// <para>
		/// The maximum applies to <b>each</b> time based group of files and 
		/// <b>not</b> the total.
		/// </para>
		/// <para>
		/// Using a daily roll the maximum total files would be 
		/// <c>(#days run) * (maxSizeRollBackups)</c>.
		/// </para>
		/// </remarks>
		public int MaxSizeRollBackups
		{
			get { return m_maxSizeRollBackups; }
			set { m_maxSizeRollBackups = value; }
		}
  
		/// <summary>
		/// Gets or sets the maximum size that the output file is allowed to reach
		/// before being rolled over to backup files.
		/// </summary>
		/// <value>
		/// The maximum size that the output file is allowed to reach before being 
		/// rolled over to backup files.
		/// </value>
		/// <remarks>
		/// <para>
		/// This property is equivalent to <see cref="MaximumFileSize"/> except
		/// that it is required for differentiating the setter taking a
		/// <see cref="long"/> argument from the setter taking a <see cref="string"/> 
		/// argument.
		/// </para>
		/// <para>
		/// The default maximum file size is 10MB.
		/// </para>
		/// </remarks>
		public long MaxFileSize
		{
			get { return m_maxFileSize; }
			set { m_maxFileSize = value; }
		}
  
		/// <summary>
		/// Gets or sets the maximum size that the output file is allowed to reach
		/// before being rolled over to backup files.
		/// </summary>
		/// <value>
		/// The maximum size that the output file is allowed to reach before being 
		/// rolled over to backup files.
		/// </value>
		/// <remarks>
		/// <para>
		/// This property allows you to specify the maximum size with the
		/// suffixes "KB", "MB" or "GB" so that the size is interpreted being 
		/// expressed respectively in kilobytes, megabytes or gigabytes. 
		/// </para>
		/// <para>
		/// For example, the value "10KB" will be interpreted as 10240.
		/// </para>
		/// <para>
		/// The default maximum file size is 10MB.
		/// </para>
		/// </remarks>
		public string MaximumFileSize
		{
			get { return m_maxFileSize.ToString(NumberFormatInfo.InvariantInfo); }
			set { m_maxFileSize = OptionConverter.ToFileSize(value, m_maxFileSize + 1); }
		}

		/// <summary>
		/// Gets or sets the path to the file that logging will be written to.
		/// </summary>
		/// <value>
		/// The path to the file that logging will be written to.
		/// </value>
		/// <remarks>
		/// <para>
		/// If the path is relative it is taken as relative from 
		/// the application base directory.
		/// </para>
		/// </remarks>
		override public string File
		{
			get { return base.File; }
			set 
			{ 
				base.File = value; 
				m_baseFileName = base.File;
			}
		}

		/// <summary>
		/// Gets or sets the rolling file count direction. 
		/// </summary>
		/// <value>
		/// The rolling file count direction.
		/// </value>
		/// <remarks>
		/// <para>
		/// Indicates if the current file is the lowest numbered file or the
		/// highest numbered file.
		/// </para>
		/// <para>
		/// By default newer files have lower numbers (<see cref="CountDirection" /> &lt; 0),
		/// i.e. log.1 is most recent, log.5 is the 5th backup, etc...
		/// </para>
		/// <para>
		/// <see cref="CountDirection" /> &gt; 0 does the opposite i.e.
		/// log.1 is the first backup made, log.5 is the 5th backup made, etc.
		/// For infinite backups use <see cref="CountDirection" /> &gt; 0 to reduce 
		/// rollover costs.
		/// </para>
		/// <para>The default file count direction is -1.</para>
		/// </remarks>
		public int CountDirection
		{
			get { return m_countDirection; }
			set { m_countDirection = value; }
		}
  
		/// <summary>
		/// Gets or sets the rolling style.
		/// </summary>
		/// <value>The rolling style.</value>
		/// <remarks>
		/// The default rolling style is <see cref="RollingMode.Composite" />.
		/// </remarks>
		public RollingMode RollingStyle
		{
			get { return m_rollingStyle; }
			set
			{
				m_rollingStyle = value;
				switch (m_rollingStyle) 
				{
					case RollingMode.Size:
						m_rollDate = false;
						m_rollSize = true;
						break;

					case RollingMode.Date:
						m_rollDate = true;
						m_rollSize = false;
						break;

					case RollingMode.Composite:
						m_rollDate = true;
						m_rollSize = true;
						break;	  
				}
			}
		}
  
		/// <summary>
		/// Gets or sets a value indicating whether to always log to
		/// the same file.
		/// </summary>
		/// <value>
		/// <c>true</c> if always should be logged to the same file, otherwise <c>false</c>.
		/// </value>
		/// <remarks>
		/// <para>
		/// By default file.log is always the current file.  Optionally
		/// file.log.yyyy-mm-dd for current formatted datePattern can by the currently
		/// logging file (or file.log.curSizeRollBackup or even
		/// file.log.yyyy-mm-dd.curSizeRollBackup).
		/// </para>
		/// <para>
		/// This will make time based rollovers with a large number of backups 
		/// much faster -- it won't have to
		/// rename all the backups!
		/// </para>
		/// </remarks>
		public bool StaticLogFileName
		{
			get { return m_staticLogFileName; }
			set { m_staticLogFileName = value; }
		}

		#endregion Public Instance Properties

		#region Override implementation of FileAppender 
  
		/// <summary>
		/// Sets the quiet writer being used.
		/// </summary>
		/// <remarks>
		/// This method can be overridden by sub classes.
		/// </remarks>
		/// <param name="writer">the writer to set</param>
		override protected void SetQWForFiles(TextWriter writer) 
		{
			QuietWriter = new CountingQuietTextWriter(writer, ErrorHandler);
		}

		/// <summary>
		/// Handles append time behavior for CompositeRollingAppender.  This checks
		/// if a roll over either by date (checked first) or time (checked second)
		/// is need and then appends to the file last.
		/// </summary>
		/// <param name="loggingEvent"></param>
		override protected void Append(LoggingEvent loggingEvent) 
		{
			if (m_rollDate) 
			{
				DateTime n = m_dateTime.Now;
				if (n >= m_nextCheck) 
				{
					m_now = n;
					m_nextCheck = NextCheckDate(m_now, m_rollPoint);
	
					RollOverTime();
				}
			}
	
			if (m_rollSize) 
			{
				if ((File != null) && ((CountingQuietTextWriter)QuietWriter).Count >= m_maxFileSize) 
				{
					RollOverSize();
				}
			}

			base.Append(loggingEvent);
		}
  
  
		/// <summary>
		/// Creates and opens the file for logging.  If <see cref="StaticLogFileName"/>
		/// is false then the fully qualified name is determined and used.
		/// </summary>
		/// <param name="fileName">the name of the file to open</param>
		/// <param name="append">true to append to existing file</param>
		/// <remarks>
		/// <para>This method will ensure that the directory structure
		/// for the <paramref name="fileName"/> specified exists.</para>
		/// </remarks>
		override protected void OpenFile(string fileName, bool append)
		{
			lock(this)
			{
				if (!m_staticLogFileName) 
				{
					m_scheduledFilename = fileName = fileName.Trim();

					if (m_rollDate)
					{
						m_scheduledFilename = fileName = fileName + m_now.ToString(m_datePattern, System.Globalization.DateTimeFormatInfo.InvariantInfo);
					}

					if (m_countDirection > 0) 
					{
						m_scheduledFilename = fileName = fileName + '.' + (++m_curSizeRollBackups);
					}
				}
	
				// Calculate the current size of the file
				long currentCount = 0;
				if (append) 
				{
					FileInfo fileInfo = new FileInfo(fileName);
					if (fileInfo.Exists)
					{
						currentCount = fileInfo.Length;
					}
				}

				// Open the file (call the base class to do it)
				base.OpenFile(fileName, append);

				// Set the file size onto the counting writer
				((CountingQuietTextWriter)QuietWriter).Count = currentCount;
			}
		}

		#endregion

		#region Initialize Options

		/// <summary>
		///	Determines curSizeRollBackups (only within the current roll point)
		/// </summary>
		private void DetermineCurSizeRollBackups()
		{
			m_curSizeRollBackups = 0;
	
			string sName = null;
			if (m_staticLogFileName || !m_rollDate) 
			{
				sName = m_baseFileName;
			} 
			else 
			{
				sName = m_scheduledFilename;
			}

			FileInfo fileInfo = new FileInfo(sName);
			if (null != fileInfo)
			{
				ArrayList arrayFiles = GetExistingFiles(fileInfo.FullName);
				InitializeRollBackups((new FileInfo(m_baseFileName)).Name, arrayFiles);

			}

			LogLog.Debug("RollingFileAppender: curSizeRollBackups starts at ["+m_curSizeRollBackups+"]");
		}

		/// <summary>
		/// Generates a wildcard pattern that can be used to find all files
		/// that are similar to the base file name.
		/// </summary>
		/// <param name="baseFileName"></param>
		/// <returns></returns>
		private static string GetWildcardPatternForFile(string baseFileName)
		{
			return baseFileName + "*";
		}

		/// <summary>
		/// Builds a list of filenames for all files matching the base filename plus a file
		/// pattern.
		/// </summary>
		/// <param name="baseFilePath"></param>
		/// <returns></returns>
		private static ArrayList GetExistingFiles(string baseFilePath)
		{
			ArrayList alFiles = new ArrayList();

			FileInfo fileInfo = new FileInfo(baseFilePath);
			DirectoryInfo dirInfo = fileInfo.Directory;
			LogLog.Debug("RollingFileAppender: Searching for existing files in ["+dirInfo+"]");

			if (dirInfo.Exists)
			{
				string baseFileName = fileInfo.Name;

				FileInfo[] files = dirInfo.GetFiles(GetWildcardPatternForFile(baseFileName));
	
				if (files != null)
				{
					for (int i = 0; i < files.Length; i++) 
					{
						string curFileName = files[i].Name;
						if (curFileName.StartsWith(baseFileName))
						{
							alFiles.Add(curFileName);
						}
					}
				}
			}
			return alFiles;
		}

		/// <summary>
		/// Initiates a roll over if needed for crossing a date boundary since the last run.
		/// </summary>
		private void RollOverIfDateBoundaryCrossing()
		{
			if (m_staticLogFileName && m_rollDate) 
			{
				FileInfo old = new FileInfo(m_baseFileName);
				if (old.Exists) 
				{
					DateTime last = old.LastWriteTime;
					LogLog.Debug("RollingFileAppender: ["+last.ToString(m_datePattern,System.Globalization.DateTimeFormatInfo.InvariantInfo)+"] vs. ["+m_now.ToString(m_datePattern,System.Globalization.DateTimeFormatInfo.InvariantInfo)+"]");

					if (!(last.ToString(m_datePattern,System.Globalization.DateTimeFormatInfo.InvariantInfo).Equals(m_now.ToString(m_datePattern, System.Globalization.DateTimeFormatInfo.InvariantInfo)))) 
					{
						m_scheduledFilename = m_baseFileName + last.ToString(m_datePattern, System.Globalization.DateTimeFormatInfo.InvariantInfo);
						LogLog.Debug("RollingFileAppender: Initial roll over to ["+m_scheduledFilename+"]");
						RollOverTime();
						LogLog.Debug("RollingFileAppender: curSizeRollBackups after rollOver at ["+m_curSizeRollBackups+"]");
					}
				}
			}
		}

		/// <summary>
		/// <para>Initializes based on existing conditions at time of <see cref="ActivateOptions"/>.
		/// The following is done:</para>
		///		A) determine curSizeRollBackups (only within the current roll point)
		///		B) initiates a roll over if needed for crossing a date boundary since the last run.
		/// </summary>
		protected void ExistingInit() 
		{
			DetermineCurSizeRollBackups();
			RollOverIfDateBoundaryCrossing();
		}

		/// <summary>
		/// Does the work of bumping the 'current' file counter higher
		/// to the highest count when an incremental file name is seen.
		/// The highest count is either the first file (when count direction
		/// is greater than 0) or the last file (when count direction less than 0).
		/// In either case, we want to know the highest count that is present.
		/// </summary>
		/// <param name="baseFile"></param>
		/// <param name="curFileName"></param>
		private void InitializeFromOneFile(string baseFile, string curFileName)
		{
			if (! curFileName.StartsWith(baseFile) )
			{
				// This is not a log file, so ignore
				return;
			}
			if (curFileName.Equals(baseFile)) 
			{
				// Base log file is not an incremented logfile (.1 or .2, etc)
				return;
			}
	
			int index = curFileName.LastIndexOf(".");
			if (-1 == index) 
			{
				// This is not an incremented logfile (.1 or .2)
				return;
			}
	
			if (m_staticLogFileName) 
			{
				int endLength = curFileName.Length - index;
				if (baseFile.Length + endLength != curFileName.Length) 
				{
					// file is probably scheduledFilename + .x so I don't care
					return;
				}
			}
	
			// Only look for files in the current roll point
			if (m_rollDate)
			{
				if (! curFileName.StartsWith(baseFile + m_dateTime.Now.ToString(m_datePattern, System.Globalization.DateTimeFormatInfo.InvariantInfo)))
				{
					LogLog.Debug("RollingFileAppender: Ignoring file ["+curFileName+"] because it is from a different date period");
					return;
				}
			}

			try 
			{
				// Bump the counter up to the highest count seen so far
				int backup = int.Parse(curFileName.Substring(index + 1), System.Globalization.NumberFormatInfo.InvariantInfo);
				if (backup > m_curSizeRollBackups)
				{
					if (0 == m_maxSizeRollBackups)
					{
						// Stay at zero when zero backups are desired
					}
					else if (-1 == m_maxSizeRollBackups)
					{
						// Infinite backups, so go as high as the highest value
						m_curSizeRollBackups = backup;
					}
					else
					{
						// Backups limited to a finite number
						if (m_countDirection > 0) 
						{
							// Go with the highest file when counting up
							m_curSizeRollBackups = backup;
						} 
						else
						{
							// Clip to the limit when counting down
							if (backup <= m_maxSizeRollBackups)
							{
								m_curSizeRollBackups = backup;
							}
						}
					}
					LogLog.Debug("RollingFileAppender: File name ["+curFileName+"] moves current count to ["+m_curSizeRollBackups+"]");
				}
			} 
			catch (FormatException /*e*/) 
			{
				//this happens when file.log -> file.log.yyyy-mm-dd which is normal
				//when staticLogFileName == false
				LogLog.Debug("RollingFileAppender: Encountered a backup file not ending in .x ["+curFileName+"]");
			}
		}

		/// <summary>
		/// Takes a list of files and a base file name, and looks for 
		/// 'incremented' versions of the base file.  Bumps the max
		/// count up to the highest count seen.
		/// </summary>
		/// <param name="baseFile"></param>
		/// <param name="arrayFiles"></param>
		private void InitializeRollBackups(string baseFile, ArrayList arrayFiles)
		{
			if (null != arrayFiles)
			{
				string baseFileLower = baseFile.ToLower(System.Globalization.CultureInfo.InvariantCulture);

				foreach(string curFileName in arrayFiles)
				{
					InitializeFromOneFile(baseFileLower, curFileName.ToLower(System.Globalization.CultureInfo.InvariantCulture));
				}
			}
		}

		/// <summary>
		/// Calculates the RollPoint for the datePattern supplied.
		/// </summary>
		/// <param name="datePattern">the date pattern to caluculate the check period for</param>
		/// <returns>The RollPoint that is most accurate for the date pattern supplied</returns>
		/// <remarks>
		/// Essentially the date pattern is examined to determine what the
		/// most suitable roll point is. The roll point chosen is the roll point
		/// with the smallest period that can be detected using the date pattern
		/// supplied. i.e. if the date pattern only outputs the year, month, day 
		/// and hour then the smallest roll point that can be detected would be
		/// and hourly roll point as minutes could not be detected.
		/// </remarks>
		private RollPoint ComputeCheckPeriod(string datePattern) 
		{
			// set date to 1970-01-01 00:00:00 this is UniversalSortableDateTimePattern 
			// (based on ISO 8601) using universal time. This date is used for reference
			// purposes to calculate the resolution of the date pattern.
			DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);

			// Get string representation of base line date
			string r0 = epoch.ToString(datePattern, System.Globalization.DateTimeFormatInfo.InvariantInfo);

			// Check each type of rolling mode starting with the smallest increment.
			for(int i = (int)RollPoint.TopOfMinute; i <= (int)RollPoint.TopOfMonth; i++) 
			{
				// Get string representation of next pattern
				string r1 = NextCheckDate(epoch, (RollPoint)i).ToString(datePattern, System.Globalization.DateTimeFormatInfo.InvariantInfo);

				LogLog.Debug("RollingFileAppender: Type = ["+i+"], r0 = ["+r0+"], r1 = ["+r1+"]");

				// Check if the string representations are different
				if (r0 != null && r1 != null && !r0.Equals(r1)) 
				{
					// Found highest precision roll point
					return (RollPoint)i;
				}
			}

			return RollPoint.InvalidRollPoint; // Deliberately head for trouble...
		}

		/// <summary>
		/// Initialize the appender based on the options set
		/// </summary>
		/// <remarks>
		/// <para>
		/// This is part of the <see cref="IOptionHandler"/> delayed object
		/// activation scheme. The <see cref="ActivateOptions"/> method must 
		/// be called on this object after the configuration properties have
		/// been set. Until <see cref="ActivateOptions"/> is called this
		/// object is in an undefined state and must not be used. 
		/// </para>
		/// <para>
		/// If any of the configuration properties are modified then 
		/// <see cref="ActivateOptions"/> must be called again.
		/// </para>
		/// <para>
		/// Sets initial conditions including date/time roll over information, first check,
		/// scheduledFilename, and calls <see cref="ExistingInit"/> to initialize
		/// the current number of backups.
		/// </para>
		/// </remarks>
		override public void ActivateOptions() 
		{
			if (m_rollDate && m_datePattern != null) 
			{
				m_now = m_dateTime.Now;
				m_rollPoint = ComputeCheckPeriod(m_datePattern);

				if (m_rollPoint == RollPoint.InvalidRollPoint)
				{
					throw new ArgumentException("Invalid RollPoint, unable to parse ["+m_datePattern+"]");
				}

				// next line added as this removes the name check in rollOver
				m_nextCheck = NextCheckDate(m_now, m_rollPoint);
			} 
			else 
			{
				if (m_rollDate)
				{
					ErrorHandler.Error("Either DatePattern or rollingStyle options are not set for ["+Name+"].");
				}
			}
	
			if (m_rollDate && File != null && m_scheduledFilename == null)
			{
				m_scheduledFilename = File + m_now.ToString(m_datePattern, System.Globalization.DateTimeFormatInfo.InvariantInfo);
			}

			ExistingInit();
	
			base.ActivateOptions();
		}

		#endregion
  
		#region Roll File

		/// <summary>
		/// Rollover the file(s) to date/time tagged file(s).
		/// Opens the new file (through setFile) and resets curSizeRollBackups.
		/// </summary>
		protected void RollOverTime() 
		{
			if (m_staticLogFileName) 
			{
				/* Compute filename, but only if datePattern is specified */
				if (m_datePattern == null) 
				{
					ErrorHandler.Error("Missing DatePattern option in rollOver().");
					return;
				}
	  
				//is the new file name equivalent to the 'current' one
				//something has gone wrong if we hit this -- we should only
				//roll over if the new file will be different from the old
				string dateFormat = m_now.ToString(m_datePattern, System.Globalization.DateTimeFormatInfo.InvariantInfo);
				if (m_scheduledFilename.Equals(File + dateFormat)) 
				{
					ErrorHandler.Error("Compare " + m_scheduledFilename + " : " + File + dateFormat);
					return;
				}
	  
				// close current file, and rename it to datedFilename
				this.CloseFile();
	  
				//we may have to roll over a large number of backups here
				string from, to;
				for (int i = 1; i <= m_curSizeRollBackups; i++) 
				{
					from = File + '.' + i;
					to = m_scheduledFilename + '.' + i;
					RollFile(from, to);
				}
	  
				RollFile(File, m_scheduledFilename);
			}
	
			try 
			{
				//We've cleared out the old date and are ready for the new
				m_curSizeRollBackups = 0; 
	  
				//new scheduled name
				m_scheduledFilename = File + m_now.ToString(m_datePattern, System.Globalization.DateTimeFormatInfo.InvariantInfo);

				// This will also close the file. This is OK since multiple
				// close operations are safe.
				this.OpenFile(m_baseFileName, false);
			}
			catch(Exception e) 
			{
				ErrorHandler.Error("setFile(" + File + ", false) call failed.", e, ErrorCode.FileOpenFailure);
			}
		}
  
		/// <summary>
		/// Renames file <paramref name="fromFile"/> to file <paramref name="toFile"/>.  It
		/// also checks for existence of target file and deletes if it does.
		/// </summary>
		/// <param name="fromFile">Name of existing file to roll.</param>
		/// <param name="toFile">New name for file.</param>
		protected void RollFile(string fromFile, string toFile) 
		{
			FileInfo target = new FileInfo(toFile);
			if (target.Exists) 
			{
				LogLog.Debug("RollingFileAppender: Deleting existing target file ["+target+"]");
				target.Delete();
			}
	
			FileInfo file = new FileInfo(fromFile);
			if (file.Exists)
			{
				// We may not have permission to move the file, or the file may be locked
				try
				{
					file.MoveTo(toFile);
					LogLog.Debug("RollingFileAppender: Moved [" + fromFile + "] -> [" + toFile + "]");
				}
				catch(Exception ex)
				{
					ErrorHandler.Error("Exception while rolling file [" + fromFile + "] -> [" + toFile + "]", ex, ErrorCode.GenericFailure);
				}
			}
			else
			{
				LogLog.Warn("RollingFileAppender: Cannot RollFile [" + fromFile + "] -> [" + toFile + "]. Source does not exist");
			}
		}
  
		/// <summary>
		/// Deletes the specified file if it exists.
		/// </summary>
		/// <param name="fileName">The file to delete.</param>
		protected void DeleteFile(string fileName) 
		{
			FileInfo file = new FileInfo(fileName);
			if (file.Exists) 
			{
				// We may not have permission to delete the file, or the file may be locked
				try
				{
					file.Delete();
					LogLog.Debug("RollingFileAppender: Deleted file [" + fileName + "]");
				}
				catch(Exception ex)
				{
					ErrorHandler.Error("Exception while deleting file [" + fileName + "]", ex, ErrorCode.GenericFailure);
				}
			}
		}
  
		/// <summary>
		/// Implements file roll base on file size.
		/// </summary>
		/// <remarks>
		/// <para>If the maximum number of size based backups is reached
		/// (<c>curSizeRollBackups == maxSizeRollBackups</c>) then the oldest
		/// file is deleted -- it's index determined by the sign of countDirection.
		/// If <c>countDirection</c> &lt; 0, then files
		/// {<c>File.1</c>, ..., <c>File.curSizeRollBackups -1</c>}
		/// are renamed to {<c>File.2</c>, ...,
		/// <c>File.curSizeRollBackups</c>}.	 Moreover, <c>File</c> is
		/// renamed <c>File.1</c> and closed.</para>
		/// 
		/// A new file is created to receive further log output.
		/// 
		/// <para>If <c>maxSizeRollBackups</c> is equal to zero, then the
		/// <c>File</c> is truncated with no backup files created.</para>
		/// 
		/// <para>If <c>maxSizeRollBackups</c> &lt; 0, then <c>File</c> is
		/// renamed if needed and no files are deleted.</para>
		/// </remarks>
		protected void RollOverSize() 
		{
			this.CloseFile(); // keep windows happy.
	
			LogLog.Debug("RollingFileAppender: rolling over count ["+((CountingQuietTextWriter)QuietWriter).Count+"]");
			LogLog.Debug("RollingFileAppender: maxSizeRollBackups ["+m_maxSizeRollBackups+"]");
			LogLog.Debug("RollingFileAppender: curSizeRollBackups ["+m_curSizeRollBackups+"]");
			LogLog.Debug("RollingFileAppender: countDirection ["+m_countDirection+"]");
	
			// If maxBackups <= 0, then there is no file renaming to be done.
			if (m_maxSizeRollBackups != 0) 
			{
				if (m_countDirection < 0) 
				{
					// Delete the oldest file, to keep Windows happy.
					if (m_curSizeRollBackups == m_maxSizeRollBackups) 
					{
						DeleteFile(File + '.' + m_maxSizeRollBackups);
						m_curSizeRollBackups--;
					}
	
					// Map {(maxBackupIndex - 1), ..., 2, 1} to {maxBackupIndex, ..., 3, 2}
					for (int i = m_curSizeRollBackups; i >= 1; i--) 
					{
						RollFile((File + "." + i), (File + '.' + (i + 1)));
					}
	
					m_curSizeRollBackups++;

					// Rename fileName to fileName.1
					RollFile(File, File + ".1");
				} 
				else 
				{	//countDirection > 0
					if (m_curSizeRollBackups >= m_maxSizeRollBackups && m_maxSizeRollBackups > 0) 
					{
						//delete the first and keep counting up.
						int oldestFileIndex = m_curSizeRollBackups - m_maxSizeRollBackups + 1;
						DeleteFile(File + '.' + oldestFileIndex);
					}
	
					if (m_staticLogFileName) 
					{
						m_curSizeRollBackups++;
						RollFile(File, File + '.' + m_curSizeRollBackups);
					}
				}
			}
	
			try 
			{
				// This will also close the file. This is OK since multiple
				// close operations are safe.
				this.OpenFile(m_baseFileName, false);
			} 
			catch(Exception e) 
			{
				ErrorHandler.Error("OpenFile ["+m_baseFileName+"] call failed.", e);
			}
		}

		#endregion

		#region NextCheckDate

		/// <summary>
		/// Roll on to the next interval after the date passed
		/// </summary>
		/// <param name="currentDateTime">the current date</param>
		/// <param name="rollPoint">the type of roll point we are working with</param>
		/// <returns>the next roll point an interval after the currentDateTime date</returns>
		/// <remarks>
		/// Advances the date to the next roll point after the 
		/// currentDateTime date passed to the method.
		/// </remarks>
		protected DateTime NextCheckDate(DateTime currentDateTime, RollPoint rollPoint) 
		{
			// Local variable to work on (this does not look very efficient)
			DateTime current = currentDateTime;

			// Do different things depending on what the type of roll point we are going for is
			switch(rollPoint) 
			{
				case RollPoint.TopOfMinute:
					current = current.AddMilliseconds(-current.Millisecond);
					current = current.AddSeconds(-current.Second);
					current = current.AddMinutes(1);
					break;

				case RollPoint.TopOfHour:
					current = current.AddMilliseconds(-current.Millisecond);
					current = current.AddSeconds(-current.Second);
					current = current.AddMinutes(-current.Minute);
					current = current.AddHours(1);
					break;

				case RollPoint.HalfDay:
					current = current.AddMilliseconds(-current.Millisecond);
					current = current.AddSeconds(-current.Second);
					current = current.AddMinutes(-current.Minute);

					if (current.Hour < 12) 
					{
						current = current.AddHours(12 - current.Hour);
					} 
					else 
					{
						current = current.AddHours(-current.Hour);
						current = current.AddDays(1);
					}
					break;

				case RollPoint.TopOfDay:
					current = current.AddMilliseconds(-current.Millisecond);
					current = current.AddSeconds(-current.Second);
					current = current.AddMinutes(-current.Minute);
					current = current.AddHours(-current.Hour);
					current = current.AddDays(1);
					break;

				case RollPoint.TopOfWeek:
					current = current.AddMilliseconds(-current.Millisecond);
					current = current.AddSeconds(-current.Second);
					current = current.AddMinutes(-current.Minute);
					current = current.AddHours(-current.Hour);
					current = current.AddDays(7 - (int)current.DayOfWeek);
					break;

				case RollPoint.TopOfMonth:
					current = current.AddMilliseconds(-current.Millisecond);
					current = current.AddSeconds(-current.Second);
					current = current.AddMinutes(-current.Minute);
					current = current.AddHours(-current.Hour);
					current = current.AddMonths(1);
					break;
			}	  
			return current;
		}

		#endregion

		#region Private Instance Fields

		/// <summary>
		/// This object supplies the current date/time.  Allows test code to plug in
		/// a method to control this class when testing date/time based rolling.
		/// </summary>
		private IDateTime m_dateTime = null;

		/// <summary>
		/// The date pattern. By default, the pattern is set to <c>".yyyy-MM-dd"</c> 
		/// meaning daily rollover.
		/// </summary>
		private string m_datePattern = ".yyyy-MM-dd";
  
		/// <summary>
		/// The actual formatted filename that is currently being written to
		/// or will be the file transferred to on roll over
		/// (based on staticLogFileName).
		/// </summary>
		private string m_scheduledFilename = null;
  
		/// <summary>
		/// The timestamp when we shall next recompute the filename.
		/// </summary>
		private DateTime m_nextCheck = DateTime.MaxValue;
  
		/// <summary>
		/// Holds date of last roll over
		/// </summary>
		private DateTime m_now;
  
		/// <summary>
		/// The type of rolling done
		/// </summary>
		private RollPoint m_rollPoint;
  
		/// <summary>
		/// The default maximum file size is 10MB
		/// </summary>
		private long m_maxFileSize = 10*1024*1024;
  
		/// <summary>
		/// There is zero backup files by default
		/// </summary>
		private int m_maxSizeRollBackups  = 0;

		/// <summary>
		/// How many sized based backups have been made so far
		/// </summary>
		private int m_curSizeRollBackups = 0;
  
		/// <summary>
		/// The rolling file count direction. 
		/// </summary>
		private int m_countDirection = -1;
  
		/// <summary>
		/// The rolling mode used in this appender.
		/// </summary>
		private RollingMode m_rollingStyle = RollingMode.Composite;

		/// <summary>
		/// Cache flag set if we are rolling by date.
		/// </summary>
		private bool m_rollDate = true;

		/// <summary>
		/// Cache flag set if we are rolling by size.
		/// </summary>
		private bool m_rollSize = true;
  
		/// <summary>
		/// Value indicating whether to always log to the same file.
		/// </summary>
		private bool m_staticLogFileName = true;
  
		/// <summary>
		/// FileName provided in configuration.  Used for rolling properly
		/// </summary>
		private string m_baseFileName;
  
		#endregion Private Instance Fields

		#region DateTime

		/// <summary>
		/// This interface is used to supply Date/Time information to the <see cref="RollingFileAppender"/>.
		/// </summary>
		/// <remarks>
		/// This interface is used to supply Date/Time information to the <see cref="RollingFileAppender"/>.
		/// Used primarily to allow test classes to plug themselves in so they can
		/// supply test date/times.
		/// </remarks>
		public interface IDateTime
		{
			/// <summary>
			/// Gets the &quot;current&quot; time.
			/// </summary>
			/// <value>The &quot;current&quot; time.</value>
			DateTime Now { get; }
		}

		/// <summary>
		/// Default implementation of <see cref="IDateTime"/> that returns the current time.
		/// </summary>
		private class DefaultDateTime : IDateTime
		{
			/// <summary>
			/// Gets the &quot;current&quot; time.
			/// </summary>
			/// <value>The &quot;current&quot; time.</value>
			public DateTime Now
			{
				get { return DateTime.Now; }
			}
		}

		#endregion DateTime
	}
}
