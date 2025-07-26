#!/bin/bash

# Wait for RabbitMQ to be fully ready
echo "Waiting for RabbitMQ to be fully ready..."
until rabbitmqctl status >/dev/null 2>&1; do
    echo "RabbitMQ not ready yet, waiting..."
    sleep 2
done
echo "RabbitMQ is ready!"

# Wait an additional moment for complete initialization
sleep 5

# Function to check if queue exists
queue_exists() {
    local queue_name=$1
    rabbitmqctl list_queues name | grep -q "^$queue_name$"
}

# Function to check if exchange exists  
exchange_exists() {
    local exchange_name=$1
    rabbitmqctl list_exchanges name | grep -q "^$exchange_name$"
}

# Function to check if binding exists
binding_exists() {
    local source=$1
    local destination=$2  
    local routing_key=$3
    rabbitmqctl list_bindings source_name destination_name routing_key | grep -q "^$source\s\+$destination\s\+$routing_key$"
}

# Create the SpelunQ-test queue if doesn't exist
echo "Checking if queue 'SpelunQ-test' exists..."
if queue_exists "SpelunQ-test"; then
    echo "Queue 'SpelunQ-test' already exists, skipping creation."
else
    echo "Creating SpelunQ-test queue..." 
    rabbitmqctl eval 'rabbit_amqqueue:declare({resource, <<"/">>, queue, <<"SpelunQ-test">>}, true, false, [], none, <<"guest">>).'
    echo "Queue 'SpelunQ-test' created successfully."
fi

echo "Queue setup completed - SpelunQ-test is ready for use!"
echo "You can create exchanges and bindings through the management UI if needed."

echo ""
echo "RabbitMQ setup completed!"
echo "Queue 'SpelunQ-test' is ready for use."
echo "Management UI is available at: http://localhost:15672"
echo "Default credentials: guest/guest"
