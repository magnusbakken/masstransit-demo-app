In order to create a more realistic and interesting demo, we should split the app so the app that consumes messages is a separate host from the command line app.

This means that all the handlers (consumers) should be moved to a new project, with the console app functioning only as a triggering mechanism.

Update tests and other setup as necessary.
