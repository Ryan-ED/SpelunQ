# RabbitMQ Docker Setup for SpelunQ

This setup provides a RabbitMQ server running in Docker with a pre-configured test queue for the SpelunQ application.

## Quick Start

1. **Start RabbitMQ:**
   ```bash
   docker-compose up -d
   ```

2. **Access the Management UI:**
   - URL: http://localhost:15672
   - Username: `guest`
   - Password: `guest`

3. **Stop RabbitMQ:**
   ```bash
   docker-compose down
   ```

## What's Included

- **RabbitMQ Server** running on port `5672`
- **Management Web UI** on port `15672`
- **Pre-configured queue:** `SpelunQ-test`
- **Default exchange:** `SpelunQ-exchange` (bound to the test queue)
- **Default credentials:** `guest/guest`

## Connection Details for Your App

- **Host:** `localhost`
- **Port:** `5672`
- **Username:** `guest`
- **Password:** `guest`
- **Queue Name:** `SpelunQ-test`

## Useful Commands

- **View logs:** `docker-compose logs rabbitmq`
- **Access container:** `docker exec -it spelunq-rabbitmq /bin/bash`
- **Reset data:** `docker-compose down -v` (removes all queues and messages)

## Queue Management

The `SpelunQ-test` queue is automatically created when the container starts. You can:
- Send messages to it from your application
- View messages in the Management UI
- Create additional queues through the UI or via rabbitmqadmin

## Troubleshooting

If the queue initialization fails:
1. Wait for RabbitMQ to fully start (can take 30-60 seconds)
2. Check logs: `docker-compose logs`
3. Restart the services: `docker-compose restart`
