# RabbitMQ Development Environment

## Quick Start

Start RabbitMQ with pre-configured SpelunQ-test queue:
```bash
docker-compose up -d
```

Stop RabbitMQ:
```bash
docker-compose down
```

## Auto-Created Resources

The container automatically creates:
- **SpelunQ-test** queue (durable)
- Default **guest** user with full permissions

## Connection Details

- **AMQP URL**: `amqp://guest:guest@localhost:5672/`
- **Host**: `localhost`
- **Port**: `5672`
- **Username**: `guest`
- **Password**: `guest`
- **Virtual Host**: `/` (default)

## Management Interface

- **URL**: http://localhost:15672
- **Username**: `guest`
- **Password**: `guest`

The management interface allows you to:
- Create and manage queues
- Send test messages
- Monitor queue statistics
- View connections and channels

## Testing Your SpelunQ App

The following queue is automatically created when the container starts:

- **SpelunQ-test** - Pre-created durable queue ready for testing

You can also create additional queues for testing:

1. **priority-queue** - Queue with message priority
2. **durable-queue** - Additional persistent queue

### Sample Connection Code (Python with pika)
```python
import pika

# Establish connection
connection = pika.BlockingConnection(
    pika.ConnectionParameters(
        host='localhost',
        port=5672,
        credentials=pika.PlainCredentials('guest', 'guest')
    )
)
channel = connection.channel()

# Use the pre-created SpelunQ-test queue
# (No need to declare it since it's already created)

# Send a message to SpelunQ-test queue
channel.basic_publish(
    exchange='',
    routing_key='SpelunQ-test',
    body='Hello SpelunQ!',
    properties=pika.BasicProperties(delivery_mode=2)  # Make message persistent
)

print("Message sent to SpelunQ-test queue!")
connection.close()
```

## Useful Docker Commands

- Check container status: `docker ps`
- View logs: `docker logs spelunq-rabbitmq`
- Access container shell: `docker exec -it spelunq-rabbitmq bash`
- Restart container: `docker-compose restart`

## Data Persistence

RabbitMQ data is persisted in a Docker volume (`spelunq_rabbitmq_data`), so your queues and messages will survive container restarts.
