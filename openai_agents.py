from agents import Agent, Runner


def my_agent_fun():
    agent = Agent(
        name="MyAgent",
        instructions="You are a helpful assistant that writes short stories about AI agents.")
    result = Runner.run_sync(agent, "Write me a short story about how openai agents work and if and if so why they are better than ChatGPT")
    print("Agent Response:", result.final_output)
