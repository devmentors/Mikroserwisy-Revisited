FROM rabbitmq:3.13.6-management
RUN rabbitmq-plugins enable rabbitmq_consistent_hash_exchange
# Add this to ensure we start fresh
ENV RABBITMQ_SERVER_ADDITIONAL_ERL_ARGS="-rabbitmq_management force_fine_statistics true"