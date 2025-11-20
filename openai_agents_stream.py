from agents import Agent, Runner
from openai.types.responses import ResponseTextDeltaEvent


async def agent_stream():
    """
    Demonstration function that streams events from an agent and prints them.
    This function has side effects only and does not return any value.
    """
    agent = Agent(
        name="StreamingAgent",
        instructions="You are a helpful assistant that provides information in a streaming manner."
    )
    result = Runner.run_streamed(agent, input="Provide a step-by-step explanation of how streaming works in AI agents.")
    async for event in result.stream_events():
        if event.type == "raw_response_event" and isinstance(event.data, ResponseTextDeltaEvent):
            print(event.data.delta, end="", flush=True)
