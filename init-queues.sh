#!/bin/bash

# Wait for RabbitMQ to be ready
echo "Waiting for RabbitMQ to be ready..."
sleep 10

# Install rabbitmqadmin if not present
if ! command -v rabbitmqadmin &> /dev/null; then
    echo "Installing rabbitmqadmin..."
    wget -O /usr/local/bin/rabbitmqadmin http://rabbitmq:15672/cli/rabbitmqadmin
    chmod +x /usr/local/bin/rabbitmqadmin
fi

# Function to check if queue exists
queue_exists() {
    local queue_name=$1
    rabbitmqadmin -H rabbitmq -u guest -p guest list queues name | grep -q "^| $queue_name "
}

# Function to check if exchange exists
exchange_exists() {
    local exchange_name=$1
    rabbitmqadmin -H rabbitmq -u guest -p guest list exchanges name | grep -q "^| $exchange_name "
}

# Function to check if binding exists
binding_exists() {
    local source=$1
    local destination=$2
    local routing_key=$3
    rabbitmqadmin -H rabbitmq -u guest -p guest list bindings source destination routing_key | grep -q "^| $source | $destination | $routing_key"
}

# Create the SpelunQ-test queue if it doesn't exist
if queue_exists "SpelunQ-test"; then
    echo "Queue 'SpelunQ-test' already exists, skipping creation."
else
    echo "Creating SpelunQ-test queue..."
    rabbitmqadmin -H rabbitmq -u guest -p guest declare queue name=SpelunQ-test durable=true
    echo "Queue 'SpelunQ-test' created successfully."
fi

# Create exchange if it doesn't exist
if exchange_exists "SpelunQ-exchange"; then
    echo "Exchange 'SpelunQ-exchange' already exists, skipping creation."
else
    echo "Creating SpelunQ exchange..."
    rabbitmqadmin -H rabbitmq -u guest -p guest declare exchange name=SpelunQ-exchange type=direct
    echo "Exchange 'SpelunQ-exchange' created successfully."
fi

# Create binding if it doesn't exist
if binding_exists "SpelunQ-exchange" "SpelunQ-test" "test"; then
    echo "Binding between 'SpelunQ-exchange' and 'SpelunQ-test' already exists, skipping creation."
else
    echo "Binding SpelunQ-test queue to SpelunQ-exchange..."
    rabbitmqadmin -H rabbitmq -u guest -p guest declare binding source=SpelunQ-exchange destination=SpelunQ-test routing_key=test
    echo "Binding created successfully."
fi

echo ""
echo "RabbitMQ setup completed!"
echo "Queue 'SpelunQ-test' is ready for use."
echo "Management UI is available at: http://localhost:15672"
echo "Default credentials: guest/guest"
