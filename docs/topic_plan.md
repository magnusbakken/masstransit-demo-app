So far we've only been using commands with single consumers. I would like to extend our example to using topics, and demonstrate how multiple consumers can handle the same event.

If possible, the triggering of the event and the multiple consumers can all be defined inside the same CLI host. I would like the console app to trigger the event, then wait for both handlers to run.

So far I believe we've been a little imprecise about the use of Publish vs. Send. I would like to redefine all the existing functionality to use Pub/Sub, which I understand to mean that they should use Send instead of Publish. Correct me if I'm wrong on this.

The new topic, however, should be set up to use Publish and, as mentioned, should define two different consumers.

The feature should work for all the supported transport types. Consider the documentation related to topologies for the different transports.

Make sure to add automated tests to verify both the existing and the new functionality.
