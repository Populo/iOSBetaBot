using Apache.NMS;
using Apache.NMS.ActiveMQ;
using Newtonsoft.Json;
using NUnit.Framework;

namespace iOSBot.Tests;

[TestFixture]
public class ActiveMqTests
{
    [SetUp]
    public void TestInitialize()
    {
        IConnectionFactory factory = new ConnectionFactory("tcp://localhost:61616")
        {
            UserName = "daledoback",
            Password = Environment.GetEnvironmentVariable("daleActiveMqPass")
        };
        _connection = factory.CreateConnection();
        _connection.Start();
        _session = _connection.CreateSession();
    }

    [TearDown]
    public void TestCleanup()
    {
        _session.Close();
        _connection.Close();
    }

    private IConnection _connection;
    private ISession _session;
    private const string QUEUE_DESTINATION = "DotNet.ActiveMQ.Test.Queue";

    [Test, Order(1)]
    public void Produce()
    {
        IDestination dest = _session.GetQueue(QUEUE_DESTINATION);
        using (IMessageProducer producer = _session.CreateProducer(dest))
        {
            var test = new MqTestClass()
            {
                Name = "Dale Doback",
                Version = "18.3"
            };
            var jsonMessage = producer.CreateTextMessage(JsonConvert.SerializeObject(test));
            producer.Send(jsonMessage);
        }
    }

    [Test, Order(2)]
    public void Consume()
    {
        IDestination dest = _session.GetQueue(QUEUE_DESTINATION);
        MqTestClass testCase = null;
        using (IMessageConsumer consumer = _session.CreateConsumer(dest))
        {
            IMessage msg;
            while ((msg = consumer.Receive(TimeSpan.FromMilliseconds(2000))) != null)
            {
                var recMsg = msg as ITextMessage;
                if (null != recMsg)
                {
                    testCase = JsonConvert.DeserializeObject<MqTestClass>(recMsg.Text);
                    if (null != testCase)
                    {
                        Assert.That("Dale Doback", Is.EqualTo(testCase.Name));
                        Assert.That(testCase.Version, Is.EqualTo(testCase.Version));
                    }
                }
                else
                {
                    Assert.Fail("No message received");
                }
            }
        }

        if (testCase == null)
        {
            Assert.Fail("TestCase is null");
        }
    }
}

public class MqTestClass
{
    public string Name { get; set; }

    public string Version { get; set; }
}