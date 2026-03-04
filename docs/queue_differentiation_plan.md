Particularly when running with a real Azure Service Bus instance, we want to avoid collisions between queues if different people are running the app.

We also want to categorize the queues created for this demo app so they can easily be recognized as belong together.

To solve these problems, we should add a prefix "masstransitdemo" for all queues, and we should use the username of the logged+in user as a prefix. Use "." as a separator.

For consistency we will apply this convention for all transport types, even though it's most important when running with ASB.
