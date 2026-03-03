Create a demo project using the MassTransit library.

The output should be a .NET console application, which interactively lets the user choose which features to test. For example, the app could let the user trigger a command. The app exists both to demonstrate how to write code that 

Reference the documentation at https://masstransit.io/ when considering how to use the library, particularly under https://masstransit.io/documentation.

MassTransit switched to a paid license from version 9 and onwards. For this sample project we will stick to MassTransit 8.x for now.

Write integration tests and unit tests along the way to verify that each step is valid.

Break the development process into several targeted pull requests. Do not put all the code in a single big PR.

These are the features we want to investigate:
- Choice of transport: Azure Service Bus, RabbitMQ, PostgreSQL database. This should be available as a setting that can be customized for all the other 
- Simple messaging with a basic handler that simply prints some output.
- A chain of handlers that trigger each other, e.g. Message A triggers B, which triggers C, which completes the chain (names should be substituted for something more realistic).
- A handler that intentionally errors out. Demonstrate different uses of dead-letter queues when this happens. For Azure Service Bus, include an option of whether to use the platform's native dead-letter queues or custom named queues.
- Also include a handler that errors out, but can be retried, and succeeds the second time around.
- A demonstration of the transactional outbox pattern. Include some update to a dummy database table, and demonstrate that our database and the event publish happen atomically. Include both the Bus Outbox and the Entity Framework Outbox.
- A saga that can be initiated either by message X or message Y, which can arrive in any order. The saga completes when both messages have been received. Include one example using consumer sagas and another version with State Machine-based sagas.

In all cases, try to invent realistic names for the different messages used in the examples, e.g. a CustomerCreated event could trigger a SendVerificationEmail command.

Use Docker and Docker Compose to handle external dependencies: PostgreSQL, RabbitMQ, Azure Service Bus Emulator. For RabbitMQ and Postgres we will only use Docker, but for Azure Service Bus we will also include an alternative of using a real instance by providing a connection string.

If any part of this plan seems poorly thought out or incomplete, let me know before starting to write any code. I'm using this to become familiar with MassTransit, so you should not assume that I'm using all terminology correctly or that all my requirements make perfect sense.
