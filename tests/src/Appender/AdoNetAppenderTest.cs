using System;
using System.Data;
using System.Xml;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Repository;
using log4net.Tests.Appender.AdoNet;
using NUnit.Framework;

namespace log4net.Tests.Appender
{
    [TestFixture]
    public class AdoNetAppenderTest
    {
        [Test]
        public void NoBufferingTest()
        {
            ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());

            AdoNetAppender adoNetAppender = new AdoNetAppender();
            adoNetAppender.BufferSize = -1;
            adoNetAppender.ConnectionType = "log4net.Tests.Appender.AdoNet.Log4NetConnection";
            adoNetAppender.ActivateOptions();

            BasicConfigurator.Configure(rep, adoNetAppender);

            ILog log = LogManager.GetLogger(rep.Name, "NoBufferingTest");
            log.Debug("Message");
            Assert.AreEqual(1, Log4NetCommand.MostRecentInstance.ExecuteNonQueryCount);
        }

        [Test]
        public void WebsiteExample()
        {
            XmlDocument log4netConfig = new XmlDocument();
            #region Load log4netConfig
            log4netConfig.LoadXml(@"
                <log4net>
                <appender name=""AdoNetAppender"" type=""log4net.Appender.AdoNetAppender"">
                    <bufferSize value=""-1"" />
                    <connectionType value=""log4net.Tests.Appender.AdoNet.Log4NetConnection"" />
                    <connectionString value=""data source=[database server];initial catalog=[database name];integrated security=false;persist security info=True;User ID=[user];Password=[password]"" />
                    <commandText value=""INSERT INTO Log ([Date],[Thread],[Level],[Logger],[Message],[Exception]) VALUES (@log_date, @thread, @log_level, @logger, @message, @exception)"" />
                    <parameter>
                        <parameterName value=""@log_date"" />
                        <dbType value=""DateTime"" />
                        <layout type=""log4net.Layout.RawTimeStampLayout"" />
                    </parameter>
                    <parameter>
                        <parameterName value=""@thread"" />
                        <dbType value=""String"" />
                        <size value=""255"" />
                        <layout type=""log4net.Layout.PatternLayout"">
                            <conversionPattern value=""%thread"" />
                        </layout>
                    </parameter>
                    <parameter>
                        <parameterName value=""@log_level"" />
                        <dbType value=""String"" />
                        <size value=""50"" />
                        <layout type=""log4net.Layout.PatternLayout"">
                            <conversionPattern value=""%level"" />
                        </layout>
                    </parameter>
                    <parameter>
                        <parameterName value=""@logger"" />
                        <dbType value=""String"" />
                        <size value=""255"" />
                        <layout type=""log4net.Layout.PatternLayout"">
                            <conversionPattern value=""%logger"" />
                        </layout>
                    </parameter>
                    <parameter>
                        <parameterName value=""@message"" />
                        <dbType value=""String"" />
                        <size value=""4000"" />
                        <layout type=""log4net.Layout.PatternLayout"">
                            <conversionPattern value=""%message"" />
                        </layout>
                    </parameter>
                    <parameter>
                        <parameterName value=""@exception"" />
                        <dbType value=""String"" />
                        <size value=""2000"" />
                        <layout type=""log4net.Layout.ExceptionLayout"" />
                    </parameter>
                </appender>
                <root>
                    <level value=""ALL"" />
                    <appender-ref ref=""AdoNetAppender"" />
                  </root>  
                </log4net>");
            #endregion

            ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
            XmlConfigurator.Configure(rep, log4netConfig["log4net"]);
            ILog log = LogManager.GetLogger(rep.Name, "WebsiteExample");
            log.Debug("Message");

            IDbCommand command = Log4NetCommand.MostRecentInstance;
            
            Assert.AreEqual(
                "INSERT INTO Log ([Date],[Thread],[Level],[Logger],[Message],[Exception]) VALUES (@log_date, @thread, @log_level, @logger, @message, @exception)",
                command.CommandText);
            
            Assert.AreEqual(6, command.Parameters.Count);

            IDbDataParameter param = (IDbDataParameter)command.Parameters["@message"];
            Assert.AreEqual("Message", param.Value);

            param = (IDbDataParameter)command.Parameters["@log_level"];
            Assert.AreEqual(Level.Debug.ToString(), param.Value);

            param = (IDbDataParameter)command.Parameters["@logger"];
            Assert.AreEqual("WebsiteExample", param.Value);

            param = (IDbDataParameter)command.Parameters["@exception"];
            Assert.IsEmpty((string)param.Value);
        }
    }
}
