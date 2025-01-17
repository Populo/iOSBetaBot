using Apache.NMS;
using Apache.NMS.ActiveMQ;
using Newtonsoft.Json;

namespace iOSBot.Service;

public interface IMqService
{
    void QueueMessage<T>(string queue, T message);
    List<T> GetMessage<T>(string queue);
}

public class MqService : IMqService
{
    public void QueueMessage<T>(string queue, T message)
    {
        var factory = new ConnectionFactory("tcp://localhost:61616")
        {
            UserName = "daledoback",
            Password = Environment.GetEnvironmentVariable("daleActiveMqPass")
        };
        using var connection = factory.CreateConnection();
        connection.Start();
        using var session = connection.CreateSession();

        var destination = session.GetQueue(queue);
        using var producer = session.CreateProducer(destination);
        var msg = producer.CreateTextMessage(JsonConvert.SerializeObject(message));
        producer.Send(msg);
    }

    public List<T> GetMessage<T>(string queue)
    {
        List<T> messages = new();
        var factory = new ConnectionFactory("tcp://localhost:61616")
        {
            UserName = "daledoback",
            Password = Environment.GetEnvironmentVariable("daleActiveMqPass")
        };
        using var connection = factory.CreateConnection();
        connection.Start();
        using var session = connection.CreateSession();

        var destination = session.GetQueue(queue);
        using (var consumer = session.CreateConsumer(destination))
        {
            IMessage msg;
            while ((msg = consumer.Receive(TimeSpan.FromMilliseconds(2000))) != null)
            {
                var recMsg = msg as ITextMessage;
                if (null != recMsg)
                {
                    messages.Add(JsonConvert.DeserializeObject<T>(recMsg.Text));
                }
                else
                {
                    throw new Exception("Could not receive message");
                }
            }
        }

        return messages;
    }
}

public enum EnumCommands
{
    Start,
    Stop,
    Force
}