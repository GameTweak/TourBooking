using RabbitMQ.Client;
using System.Threading.Channels;

namespace RabbitMQService
{
    public class RabbitService
    {
        private IConnection? _connection = null;
        private IChannel? _channel = null;
        private string _hostName;

        public RabbitService(string hostName)
        {
            _hostName = hostName;
        }

        private async Task SetupRabbitMQ()
        {
            if (_connection == null && _channel == null)
            {
                ConnectionFactory factory = new ConnectionFactory() { HostName = _hostName };
                _connection = await factory.CreateConnectionAsync();
                _channel = await _connection.CreateChannelAsync();
            }
        }

        public async Task SetupQueue(string queue, bool durable, string routingKey, bool deadLetter)
        {
            var exchangeName = queue.ToLower().EndsWith(".queue") 
                                ? queue[..^6] + ".exchange" 
                                : queue + ".exchange";

            await SetupQueue(queue, exchangeName, routingKey, durable, deadLetter);
        }

        public async Task SetupQueue(string queue, string exchange, string routingKey, bool durable, bool deadLetter)
        {
            await SetupRabbitMQ();

            var queueArgs = new Dictionary<string, object> { };

            if (deadLetter == true)
            {
                var (DLX, DLXroutingKey) = await SetupDeadLetter(queue, durable);
                queueArgs.Add("x-dead-letter-exchange", DLX);
                queueArgs.Add("x-dead-letter-routing-key", DLXroutingKey);
            }

            await _channel.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable, autoDelete: false);

            await _channel.QueueDeclareAsync(queue, durable, exclusive: false, autoDelete: false, arguments: queueArgs);

            await _channel.QueueBindAsync(queue, exchange, "tour.booked");
        }

        private async Task<(string DLX, string DLXroutingKey)> SetupDeadLetter(string queue, bool durable)
        {
            string exchangeName = $"{queue}.DLX";
            await _channel.ExchangeDeclareAsync(exchangeName, ExchangeType.Direct, durable: durable, autoDelete: false);

            string queueName = $"{exchangeName}.queue";
            await _channel.QueueDeclareAsync(queueName, durable: durable, exclusive: false, autoDelete: false);

            string routingKey = "failed";
            await _channel.QueueBindAsync(queueName, exchangeName, routingKey: routingKey);

            return (exchangeName, routingKey);
        }
    }
}
