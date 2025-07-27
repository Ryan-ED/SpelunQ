# SpelunQ - A RabbitMQ Message Manager

A Windows desktop application built with WPF for monitoring, managing, and interacting with RabbitMQ message queues. SpelunQ allows you to view messages in queues, download those messages to file, load messages from file, send new messages, and manage your RabbitMQ workflows with an intuitive graphical interface.

## Features

### 🔌 Connection Management
- Connect to local or remote RabbitMQ instances
- Configurable host, port, username, and password
- Automatic queue discovery via RabbitMQ Management API

### 📊 Queue Monitoring
- **Non-destructive monitoring**: View messages without consuming them from the queue
- Real-time message display as they arrive
- QoS-based approach ensures messages remain available for other consumers
- Start/stop monitoring with proper cleanup

### 📨 Message Management
- View complete message content including headers, exchange, and routing key information
- Save messages as JSON files for later use
- Load and resend saved messages
- Clear message history

### 📤 Message Publishing
- Send messages to any available queue
- Simple text-based message composition
- Queue selection from discovered queues

### 💾 File Operations
- Export messages to JSON format with full metadata
- Import and resend previously saved messages
- Timestamped file naming for organization

## Prerequisites

- **Operating System**: Windows (requires .NET 9.0 Windows runtime)
- **RabbitMQ Server**: Running instance with Management plugin enabled (see the included sample docker-compose file)
- **Network Access**: To RabbitMQ AMQP port (default: 5672) and Management API (default: 15672)

## Installation

1. Clone the repository:
```shell script
git clone https://github.com/@Ryan-ED/SpelunQ.git
   cd SpelunQ
```


2. Build the application:
```shell script
dotnet build
```


3. Run the application:
```shell script
dotnet run
```

## Run the Docker container (optional)
If you don't already have a RabbitMQ server to connect to, use the included docker-compose file with `docker-compose up -d`


## Usage

### Getting Started

1. **Connect to RabbitMQ**:
    - Enter your RabbitMQ server details (host, port, username, password)
    - Click "Connect" to establish connection
    - Available queues will be automatically discovered

2. **Monitor a Queue**:
    - Select a queue from the dropdown
    - Click "Start Listening" to begin monitoring
    - Messages will appear in real-time without being consumed
    - Click "Stop Listening" when done (messages return to queue)

3. **Send Messages**:
    - Select a target queue
    - Enter your message content
    - Click "Send" to publish the message

4. **Save/Load Messages**:
    - Select a message from the list
    - Use "Save Message" to export as JSON
    - Use "Load Message" to import and resend saved messages

### Configuration

Default connection settings:
- **Host**: localhost
- **Port**: 5672
- **Username**: guest
- **Password**: guest
- **Management URL**: http://localhost:15672

## Architecture

### Core Components

- **RabbitMqService**: Handles all RabbitMQ operations using the official RabbitMQ.Client library
- **FileService**: Manages message serialization and file I/O operations
- **RabbitMessage Model**: Represents message data with metadata
- **MainWindow**: WPF UI with comprehensive queue management interface

### Key Technical Features

- **Async/await pattern** throughout for responsive UI
- **Event-driven architecture** for real-time message updates
- **QoS-based monitoring** to prevent message consumption
- **Proper resource disposal** with connection cleanup
- **Error handling** with user-friendly error messages

## Dependencies

- **.NET 9.0** (Windows target framework)
- **WPF** (Windows Presentation Foundation)
- **RabbitMQ.Client 7.1.2** (Official RabbitMQ client library)

## Development

### Project Structure
```
SpelunQ/
├── Models/
│   ├── RabbitMessage.cs      # Message data model
│   └── QueueInfo.cs          # Queue information model
├── Services/
│   ├── RabbitMqService.cs    # RabbitMQ operations
│   └── FileService.cs        # File I/O operations
├── MainWindow.xaml           # Main UI layout
├── MainWindow.xaml.cs        # Main UI logic
├── SendMessageDialog.xaml    # Message sending dialog
└── SpelunQ.csproj           # Project configuration
```


### Building from Source

```shell script
# Restore dependencies
dotnet restore

# Build the project
dotnet build --configuration Release

# Run tests (if any)
dotnet test

# Publish for distribution
dotnet publish --configuration Release --runtime win-x64 --self-contained
```


## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Troubleshooting

### Common Issues

**Connection Failed**
- Verify RabbitMQ server is running
- Check firewall settings for ports 5672 and 15672
- Confirm credentials are correct

**No Queues Found**
- Ensure RabbitMQ Management plugin is enabled: `rabbitmq-plugins enable rabbitmq_management`
- Verify Management API is accessible at http://{server-host}:15672

**Messages Not Appearing**
- Check queue has messages using RabbitMQ Management UI
- Verify queue name spelling and case sensitivity
- Ensure user has permissions to access the queue

## Acknowledgments

- Built with the official [RabbitMQ .NET Client](https://github.com/rabbitmq/rabbitmq-dotnet-client)
- Uses WPF for the desktop interface
- Inspired by the need for FOSS message queue monitoring tools