The CLI approach is good for local testing and demo purposes, but in order for automated agents to be able to verify the correctness of the code it's necessary to be able to trigger each demo without needing to pass input dynamically while the app is running.

Use an appropriate .NET CLI library to build a well-structured command-line interface with input options that allow running the app either interactively or by passing in use-cases up front.

For example, there could be input switches that control the transport technology used, and which demo case to run.
