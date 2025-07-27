using RabbitMQ.Client;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client.Events;
using SpelunQ.Models;

namespace SpelunQ.Services;

public class RabbitMqService : IDisposable
{
    private IConnection? _connection;
    private IChannel? _channel;
    private string _currentQueue = string.Empty;
    private AsyncEventingBasicConsumer? _consumer;

    // Event to notify when a message is received
    public event Action<RabbitMessage>? MessageReceived;

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
            // TODO: log
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
            // TODO: log
            Console.WriteLine(ex.Message);
            throw;
        }
    }
    
    public async Task StartListening(string queueName)
    {
        if (_channel == null)
        {
            throw new InvalidOperationException("Not connected to RabbitMQ");
        }

        try
        {
            // Stop previous consumer if exists
            await StopListening();
            _currentQueue = queueName;

            await _channel.QueueDeclarePassiveAsync(queueName);
        
            // Set QoS to prefetch more messages for better monitoring coverage
            // prefetchCount: 0 means unlimited - you'll see all available messages
            await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 0, global: false);

            _consumer = new AsyncEventingBasicConsumer(_channel);

            _consumer.ReceivedAsync += (_, ea) =>
            {
                try
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

                    // Fire event to update UI
                    MessageReceived?.Invoke(rabbitMessage);
                }
                catch (Exception ex)
                {
                    // TODO: log
                    Console.WriteLine($"Error processing message: {ex.Message}");
                }
                
                return Task.CompletedTask;
            };
            
            // Set autoAck to false - this way messages stay in "unacknowledged" state
            // and will be visible to your monitoring without being consumed
            await _channel.BasicConsumeAsync(queue: queueName, autoAck: false, consumer: _consumer);
        }
        catch (Exception ex)
        {
            // TODO: log
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
                // Check if the channel is still open before trying to cancel
                if (_channel.IsOpen)
                {
                    var consumerTag = _consumer.ConsumerTags.FirstOrDefault();
                    if (!string.IsNullOrEmpty(consumerTag))
                    {
                        // Cancel the consumer first
                        await _channel.BasicCancelAsync(consumerTag);

                        // Wait a brief moment for any in-flight messages to be processed
                        await Task.Delay(100);

                        // Reject all unacknowledged messages to return them to the queue
                        // The 'multiple: true' parameter rejects all unacknowledged messages
                        // delivered to this consumer
                        await _channel.BasicNackAsync(0, multiple: true, requeue: true);
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                // Channel or connection is already disposed, ignore
                // TODO: log
            }
            catch (Exception ex)
            {
                // TODO: log
                Console.WriteLine($"Error stopping listener: {ex.Message}");
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
            throw new InvalidOperationException("Not connected to RabbitMQ");
        }

        try
        {
            await _channel.QueueDeclarePassiveAsync(queueName);

            var body = Encoding.UTF8.GetBytes(message);
            await _channel.BasicPublishAsync("", queueName, body);
        }
        catch (Exception ex)
        {
            // TODO: log
            Console.WriteLine(ex.Message);
            throw;
        }
    }

    public void Dispose()
    {
        try
        {
            // Capture the references to avoid disposal race conditions
            var channel = _channel;
            var connection = _connection;
        
            // Use synchronous disposal to avoid async void issues
            Task.Run(async () =>
            {
                await StopListening();
            
                if (channel?.IsOpen == true)
                {
                    await channel.CloseAsync();
                }
            
                if (connection?.IsOpen == true)
                {
                    await connection.CloseAsync();
                }
            }).Wait(TimeSpan.FromSeconds(5)); // Give it 5 seconds max
        
            channel?.Dispose();
            connection?.Dispose();
        }
        catch (ObjectDisposedException)
        {
            // Objects already disposed, ignore
            // TODO: log
        }
        catch (Exception ex)
        {
            // TODO: log
            Console.WriteLine(ex.Message);
        }
        finally
        {
            // Clear the field references
            _channel = null;
            _connection = null;
        }
    }
}