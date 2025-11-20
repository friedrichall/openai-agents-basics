# This is a sample Python script.
import asyncio

from openai_agents import my_agent_fun
from openai_agents_stream import agent_stream


# Press Umschalt+F10 to execute it or replace it with your code.
# Press Double Shift to search everywhere for classes, files, tool windows, actions, and settings.


async def main():
    await agent_stream()

if __name__ == '__main__':
    asyncio.run(main())


