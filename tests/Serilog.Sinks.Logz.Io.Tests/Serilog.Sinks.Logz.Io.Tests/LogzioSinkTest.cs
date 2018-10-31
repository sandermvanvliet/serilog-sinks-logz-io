using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog.Events;
using Serilog.Sinks.Logz.Io.Tests.Support;
using Xunit;

namespace Serilog.Sinks.Logz.Io.Tests
{
    public class LogzioSinkTest
    {
        [Fact]
        public async Task AllLogzIoShouldHaveTimestampAndMessage()
        {
            //Arrange
            var httpData = new List<HttpContent>();
            var log = new LoggerConfiguration()
                .WriteTo.Sink(new LogzioSink(new GoodFakeHttpClient(httpData), "testAuthCode", "testTyoe", 100,
                    TimeSpan.FromSeconds(1)))
                .CreateLogger();

            //Act
            var logMsg = "This a Information Log Trace";
            log.Information(logMsg);
            log.Dispose();

            //Assert
            httpData.Should().NotBeNullOrEmpty();
            httpData.Should().HaveCount(1);

            var data = await httpData.Single().ReadAsStringAsync();
            data.Should().NotBeNullOrWhiteSpace();
            var dataDic = JsonConvert.DeserializeObject<Dictionary<string, string>>(data);
            dataDic.Should()
                .ContainKey(
                    "@timestamp"); //LogzIo Requered a @timestamp (Iso DateTime) to indicate the time of the event.
            dataDic.Should().ContainKey("message"); //LogzIo Requered a lowercase message string
            dataDic["@timestamp"].Should().NotBeNullOrWhiteSpace();
            dataDic["message"].Should().Be(logMsg);
            dataDic["level"].Should().Be(LogEventLevel.Information.ToString());
        }

        [Fact]
        public async Task ExtraPropertiesShouldBeSentToLogzIo()
        {
            //Arrange
            var httpData = new List<HttpContent>();
            var log = new LoggerConfiguration()
                .Enrich.WithProperty("PropStr1", "banana")
                .Enrich.WithProperty("PropInt1", 42)
                .Enrich.WithProperty("PropInt2", -42)
                .Enrich.WithProperty("PropFloat1", 88.8)
                .Enrich.WithProperty("PropFloat2", -43.5)
                .Enrich.WithProperty("PropBool1", false)
                .Enrich.WithProperty("PropBool2", true)
                .Enrich.WithProperty("PropEnum1", DateTimeKind.Utc)
                .Enrich.WithProperty("PropEnum2", StringComparison.CurrentCultureIgnoreCase)
                .Enrich.WithProperty("PropArr1", new[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 0})
                .Enrich.WithProperty("PropArr2", new[] {"banana", "apple", "lemon"})
                .Enrich.WithProperty("PropArr3", new object[] {1, "banana", 3.5, false})
                .Enrich.WithProperty("PropNull1", null)
                .Enrich.WithProperty("PropDic1",
                    new Dictionary<string, int> {{"banana", 2}, {"apple", 5}, {"lemon", 76}})
                .Enrich.WithProperty("PropObj1",
                    new {Name = "banana", Itens = new[] {1, 2, 3, 4}, Id = 99, active = true})
                .Enrich.WithProperty("PropObj2",
                    new {Name = "banana", Itens = new[] {1, 2, 3, 4}, Id = 99, active = true}, true)
                .WriteTo.Sink(new LogzioSink(new GoodFakeHttpClient(httpData), "testAuthCode", "testTyoe", 100,
                    TimeSpan.FromSeconds(1)))
                .CreateLogger();

            //Act
            var logMsg = "This a Information Log Trace";
            log.Warning(logMsg);
            log.Dispose();

            //Assert
            httpData.Should().NotBeNullOrEmpty();
            httpData.Should().HaveCount(1);

            var data = await httpData.Single().ReadAsStringAsync();
            data.Should().NotBeNullOrWhiteSpace();

            var dataDic = JsonConvert.DeserializeObject<Dictionary<string, object>>(data);
            dataDic.Should()
                .ContainKey(
                    "@timestamp"); //LogzIo Requered a @timestamp (Iso DateTime) to indicate the time of the event.
            dataDic.Should().ContainKey("message"); //LogzIo Requered a lowercase message string
            dataDic["@timestamp"].Should().NotBeNull();
            dataDic["message"].Should().Be(logMsg);
            dataDic["level"].Should().Be(LogEventLevel.Warning.ToString());

            dataDic.Should().ContainKeys("Properties.PropStr1", "Properties.PropInt1", "Properties.PropInt2",
                "Properties.PropBool1", "Properties.PropArr1", "Properties.PropArr2",
                "Properties.PropObj1", "Properties.PropObj2", "Properties.PropNull1",
                "Properties.PropDic1", "Properties.PropFloat1", "Properties.PropFloat2",
                "Properties.PropArr3", "Properties.PropEnum1", "Properties.PropEnum2");

            dataDic["Properties.PropStr1"].Should().Be("banana");
            dataDic["Properties.PropInt1"].Should().Be(42);
            dataDic["Properties.PropInt2"].Should().Be(-42);
            dataDic["Properties.PropFloat1"].Should().Be(88.8);
            dataDic["Properties.PropFloat2"].Should().Be(-43.5);
            dataDic["Properties.PropBool1"].Should().Be(false);
            dataDic["Properties.PropBool2"].Should().Be(true);
            dataDic["Properties.PropNull1"].Should().BeNull();
            dataDic["Properties.PropEnum1"].Should().Be(DateTimeKind.Utc.ToString());
            dataDic["Properties.PropEnum2"].Should().Be(StringComparison.CurrentCultureIgnoreCase.ToString());

            var dataDinamic = JObject.Parse(data);
            dataDinamic["Properties.PropStr1"].Should().BeNullOrEmpty();
            dataDinamic["Properties.PropArr1"].Should().BeOfType<JArray>();
            dataDinamic["Properties.PropArr2"].Should().BeOfType<JArray>();
            dataDinamic["Properties.PropArr3"].Should().BeOfType<JArray>();
            dataDinamic["Properties.PropDic1"].Should().BeOfType<JObject>();
            dataDinamic["Properties.PropObj2"].Should().BeOfType<JObject>();

            //TODO More Test for other Props
        }

        [Fact]
        public async Task GivenBoostedPropertiesIsDisabled_PropertiesHavePropertiesPrefix()
        {
            //Arrange
            var httpData = new List<HttpContent>();
            var log = new LoggerConfiguration()
                .WriteTo.Sink(new LogzioSink(new GoodFakeHttpClient(httpData), "testAuthCode", "testTyoe", 100,
                    TimeSpan.FromSeconds(1), boostProperties: false))
                .Enrich.WithProperty("EnrichedProperty", "banana")
                .CreateLogger();

            //Act
            var logMsg = "This a Information Log Trace {MessageTemplateProperty}";
            log.Information(logMsg, "pear");
            log.Dispose();

            //Assert
            httpData.Should().NotBeNullOrEmpty();
            httpData.Should().HaveCount(1);

            var data = await httpData.Single().ReadAsStringAsync();
            data.Should().NotBeNullOrWhiteSpace();
            var dataDic = JsonConvert.DeserializeObject<Dictionary<string, string>>(data);

            dataDic["Properties.EnrichedProperty"].Should().Be("banana");
            dataDic["Properties.MessageTemplateProperty"].Should().Be("pear");
        }

        [Fact]
        public async Task GivenBoostPropertiesIsEnabled_EnrichedPropertyDoesNotHavePropertiesPrefix()
        {
            //Arrange
            var httpData = new List<HttpContent>();
            var log = new LoggerConfiguration()
                .WriteTo.Sink(new LogzioSink(new GoodFakeHttpClient(httpData), "testAuthCode", "testTyoe", 100,
                    TimeSpan.FromSeconds(1), boostProperties: true))
                .Enrich.WithProperty("EnrichedProperty", "banana")
                .CreateLogger();

            //Act
            var logMsg = "This a Information Log Trace {MessageTemplateProperty}";
            log.Information(logMsg, "pear");
            log.Dispose();

            //Assert
            httpData.Should().NotBeNullOrEmpty();
            httpData.Should().HaveCount(1);

            var data = await httpData.Single().ReadAsStringAsync();
            data.Should().NotBeNullOrWhiteSpace();
            var dataDic = JsonConvert.DeserializeObject<Dictionary<string, string>>(data);

            dataDic["EnrichedProperty"].Should().Be("banana");
        }

        [Fact]
        public async Task GivenBoostPropertiesIsEnabled_MessagePropertyDoesNotHavePropertiesPrefix()
        {
            //Arrange
            var httpData = new List<HttpContent>();
            var log = new LoggerConfiguration()
                .WriteTo.Sink(new LogzioSink(new GoodFakeHttpClient(httpData), "testAuthCode", "testTyoe", 100,
                    TimeSpan.FromSeconds(1), boostProperties: true))
                .Enrich.WithProperty("EnrichedProperty", "banana")
                .CreateLogger();

            //Act
            var logMsg = "This a Information Log Trace {MessageTemplateProperty}";
            log.Information(logMsg, "pear");
            log.Dispose();

            //Assert
            httpData.Should().NotBeNullOrEmpty();
            httpData.Should().HaveCount(1);

            var data = await httpData.Single().ReadAsStringAsync();
            data.Should().NotBeNullOrWhiteSpace();
            var dataDic = JsonConvert.DeserializeObject<Dictionary<string, string>>(data);
            
            dataDic["MessageTemplateProperty"].Should().Be("pear");
        }

        [Fact]
        public async Task GivenMessageWithProperties_DataContainsMessageTemplateProperty()
        {
            //Arrange
            var httpData = new List<HttpContent>();
            var log = new LoggerConfiguration()
                .WriteTo.Sink(new LogzioSink(new GoodFakeHttpClient(httpData), "testAuthCode", "testTyoe", 100,
                    TimeSpan.FromSeconds(1), boostProperties: true))
                .Enrich.WithProperty("EnrichedProperty", "banana")
                .CreateLogger();

            //Act
            var logMsg = "This a Information Log Trace {MessageTemplateProperty}";
            log.Information(logMsg, "pear");
            log.Dispose();

            //Assert
            httpData.Should().NotBeNullOrEmpty();
            httpData.Should().HaveCount(1);

            var data = await httpData.Single().ReadAsStringAsync();
            data.Should().NotBeNullOrWhiteSpace();
            var dataDic = JsonConvert.DeserializeObject<Dictionary<string, string>>(data);

            dataDic["message"].Should().Be(logMsg);
        }

        [Fact]
        public async Task GivenMessageWithProperties_DataContainsRenderedMessageTemplateProperty()
        {
            //Arrange
            var httpData = new List<HttpContent>();
            var log = new LoggerConfiguration()
                .WriteTo.Sink(new LogzioSink(new GoodFakeHttpClient(httpData), "testAuthCode", "testTyoe", 100,
                    TimeSpan.FromSeconds(1), boostProperties: true))
                .Enrich.WithProperty("EnrichedProperty", "banana")
                .CreateLogger();

            //Act
            var logMsg = "This a Information Log Trace {MessageTemplateProperty}";
            log.Information(logMsg, "pear");
            log.Dispose();

            //Assert
            httpData.Should().NotBeNullOrEmpty();
            httpData.Should().HaveCount(1);

            var data = await httpData.Single().ReadAsStringAsync();
            data.Should().NotBeNullOrWhiteSpace();
            var dataDic = JsonConvert.DeserializeObject<Dictionary<string, string>>(data);

            dataDic["RenderedMessage"].Should().Be("This a Information Log Trace \"pear\"");
        }
    }
}