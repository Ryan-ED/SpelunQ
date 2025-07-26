using RabbitMQ.Client;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using RabbitMQ.Client.Events;
using SpelunQ.Models;

namespace SpelunQ.Services;

public class RabbitMqService(ObservableCollection<RabbitMessage> _messages)
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
    
    public async Task<List<QueueInfo>> GetQueuesAsync(string managementUrl = "http://localhost:15672", 
        string username = "guest", string password = "guest")
    {
        try
        {
            using var client = new HttpClient();
            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);

            var response = await client.GetStringAsync($"{managementUrl}/api/queues");
            var queues = JsonSerializer.Deserialize<QueueInfo[]>(response, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                PropertyNameCaseInsensitive = true 
            });

            return queues?.ToList() ?? [];
        }
        catch (Exception ex)
        {
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

            await _channel.QueueDeclarePassiveAsync(queueName);

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
                Application.Current.Dispatcher.Invoke(() => { _messages.Insert(0, rabbitMessage); });

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
                // Check if channel is still open before trying to cancel
                if (_channel.IsOpen)
                {
                    var consumerTag = _consumer.ConsumerTags.FirstOrDefault();
                    if (!string.IsNullOrEmpty(consumerTag))
                    {
                        await _channel.BasicCancelAsync(consumerTag);
                    }
                }
            }
            catch (ObjectDisposedException ex)
            {
                // Channel or connection is already disposed, ignore
                Console.WriteLine(ex.Message);
            }
            catch (Exception ex)
            {
                // log
                Console.WriteLine(ex.Message);
                throw;
            }
            finally
            {
                _consumer = null;
                _currentQueue = string.Empty;
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
            await _channel.QueueDeclarePassiveAsync(queueName);

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