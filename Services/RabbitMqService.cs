using RabbitMQ.Client;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using RabbitMQ.Client.Events;
using SpelunQ_wpf.Models;

namespace SpelunQ_wpf.Services;

public class RabbitMqService(ObservableCollection<RabbitMessage> messages)
    : IDisposable
{
    private IConnection? _connection;
    private IChannel? _channel;
    private string _currentQueue = string.Empty;
    private AsyncEventingBasicConsumer? _consumer;

    public async Task Connect(string hostName = "localhost", int port = 5672, string userName = "guest",
        string password = "guest")
    {
        try
        {
            var factory = new ConnectionFactory()
            {
                HostName = hostName,
                Port = port,
                UserName = userName,
                Password = password
            };

            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();
        }
        catch (Exception ex)
        {
            // log
            Console.WriteLine(ex.Message);
            throw;
        }
    }

    public async Task StartListening(string queueName)
    {
        if (_channel == null)
        {
            MessageBox.Show("Not connected to RabbitMQ", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            // Stop previous consumer if exists
            await StopListening();

            _currentQueue = queueName;

            // Declare queue (creates if doesn't exist)
            await _channel.QueueDeclareAsync(queue: queueName, durable: false, exclusive: false, autoDelete: false,
                arguments: null);

            _consumer = new AsyncEventingBasicConsumer(_channel);

            _consumer.ReceivedAsync += (_, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                var rabbitMessage = new RabbitMessage
                {
                    Content = message,
                    Queue = queueName,
                    Exchange = ea.Exchange,
                    RoutingKey = ea.RoutingKey,
                    ReceivedAt = DateTime.Now
                };

                // Add headers
                if (ea.BasicProperties.Headers != null)
                {
                    foreach (var header in ea.BasicProperties.Headers)
                    {
                        rabbitMessage.Headers[header.Key] = header.Value ?? throw new InvalidOperationException("Header value is null");
                    }
                }

                // Update UI on the main thread
                Application.Current.Dispatcher.Invoke(() => { messages.Insert(0, rabbitMessage); });

                return Task.CompletedTask;
            };

            await _channel.BasicConsumeAsync(queue: queueName, autoAck: true, consumer: _consumer);
        }
        catch (Exception ex)
        {
            // log
            Console.WriteLine(ex.Message);
            throw;
        }
    }

    public async Task StopListening()
    {
        if (_consumer != null && _channel != null && !string.IsNullOrEmpty(_currentQueue))
        {
            try
            {
                await _channel.BasicCancelAsync(_consumer.ConsumerTags.FirstOrDefault() ?? "");
            }
            catch (Exception ex)
            {
                // log
                Console.WriteLine(ex.Message);
                throw;
            }
        }
    }

    public async Task SendMessage(string queueName, string message)
    {
        if (_channel == null)
        {
            MessageBox.Show("Not connected to RabbitMQ", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            await _channel.QueueDeclareAsync(queue: queueName, durable: false, exclusive: false, autoDelete: false,
                arguments: null);

            var body = Encoding.UTF8.GetBytes(message);
            await _channel.BasicPublishAsync("", queueName, body);
        }
        catch (Exception ex)
        {
            // log
            Console.WriteLine(ex.Message);
            throw;
        }
    }

    public async void Dispose()
    {
        try
        {
            await StopListening();
            _channel?.CloseAsync();
            _connection?.CloseAsync();
            _channel?.Dispose();
            _connection?.Dispose();
        }
        catch (Exception ex)
        {
            // log
            Console.WriteLine(ex.Message);
            throw;
        }
    }
}