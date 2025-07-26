#!/bin/bash
set -e

# Start RabbitMQ in the background
echo "Starting RabbitMQ server..."
rabbitmq-server &
RABBITMQ_PID=$!

# Function to cleanup on exit
cleanup() {
    echo "Shutting down RabbitMQ..."
    kill $RABBITMQ_PID 2>/dev/null || true
    wait $RABBITMQ_PID 2>/dev/null || true
}
trap cleanup EXIT

# Wait for RabbitMQ to be ready
echo "Waiting for RabbitMQ to be ready..."
until rabbitmq-diagnostics ping >/dev/null 2>&1; do
    echo "Waiting for RabbitMQ to start..."
    sleep 2
done

echo "RabbitMQ is ready! Running initialization script..."

# Copy the init script to a writable location and make it executable
cp /init-queues.sh /tmp/init-queues.sh
chmod +x /tmp/init-queues.sh
/tmp/init-queues.sh

echo "Initialization completed. RabbitMQ is ready for use."

# Keep the container running by waiting for the RabbitMQ process
wait $RABBITMQ_PID
