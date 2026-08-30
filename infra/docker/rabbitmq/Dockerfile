# Adds the delayed-message-exchange plugin to the official image so MassTransit's
# UseDelayedRedelivery (Level-2 retry — see MassTransitExtensions.cs) works. Plugin version pinned
# to match the RabbitMQ series below; bump both together if the RabbitMQ image tag changes.
FROM rabbitmq:3.13-management

ADD https://github.com/rabbitmq/rabbitmq-delayed-message-exchange/releases/download/v3.13.0/rabbitmq_delayed_message_exchange-3.13.0.ez /plugins/
RUN rabbitmq-plugins enable --offline rabbitmq_delayed_message_exchange
