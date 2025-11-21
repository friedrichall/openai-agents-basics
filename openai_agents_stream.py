from agents import Agent, Runner
from openai.types.responses import ResponseTextDeltaEvent


async def agent_stream():
    """
    Demonstration function that streams events from an agent and prints them.
    This function has side effects only and does not return any value.
    """
    agent = Agent(
        name="agent",
        instructions="You are a useless storyteller and your stories are always way too short"
    )



    result = Runner.run_streamed(agent, input="Tell me a story about AI agents.")
    async for event in result.stream_events():
        if event.type == "raw_response_event" and isinstance(event.data, ResponseTextDeltaEvent):
            print(event.data.delta, end="", flush=True)
