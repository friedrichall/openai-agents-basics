import json

from agents import Agent, Runner, ItemHelpers

from constants.agent_instructions import MANAGER_INSTRUCTIONS, INTERACTION_ELEMENTS_INSTRUCTIONS, \
    TRANSITIONS_INSTRUCTIONS, STATES_INSTRUCTIONS, VISUALIZATION_ELEMENTS_INSTRUCTIONS, \
    VISUALIZATION_ARRAYS_INSTRUCTIONS
from model.output_type_FuncSpec import FunctionalSpecification
from model.output_type_InteractionElements import InteractionElements
from model.output_type_States import States
from model.output_type_Transitions import Transitions
from model.output_type_VisualizationElements import VisualizationElements
from model.output_type_VisualizationArrays import VisualizationArrays

BASE_MODEL = "gpt-5.1"

USER_INPUT = (
    "generate a complete functional specification of a virtual prototype with two cubes: one is a slider and the other one is a rotatable. Omit all the optional elements within the generated JSON files"
)

async def agents_vivian():
    """Sets up and runs the Vivian Functional Specification generation agents."""
    interaction_elements_agent = Agent(
        name="interaction_elements_agent",
        model=BASE_MODEL,
        instructions=INTERACTION_ELEMENTS_INSTRUCTIONS,
        output_type=InteractionElements
    )
    transitions_agent = Agent(
        name="transitions_agent",
        model=BASE_MODEL,
        instructions=TRANSITIONS_INSTRUCTIONS,
        output_type=Transitions
    )
    states_agent = Agent(
        name="states_agent",
        model=BASE_MODEL,
        instructions=STATES_INSTRUCTIONS,
        output_type=States
    )
    visualization_elements_agent = Agent(
        name="visualization_elements_agent",
        model=BASE_MODEL,
        instructions=VISUALIZATION_ELEMENTS_INSTRUCTIONS,
        output_type=VisualizationElements
    )
    visualization_arrays_agent = Agent(
        name="visualization_arrays_agent",
        model=BASE_MODEL,
        instructions=VISUALIZATION_ARRAYS_INSTRUCTIONS,
        output_type=VisualizationArrays
    )



    manager_agent = Agent(
        name="manager_agent",
        model=BASE_MODEL,
        instructions=MANAGER_INSTRUCTIONS,
        tools=[
            interaction_elements_agent.as_tool(
                tool_name="interaction_elements_JSON_generator",
                tool_description="Generates the InteractionElements.json file based on the prototype description and existing elements."
            ),
            transitions_agent.as_tool(
                tool_name="transitions_JSON_generator",
                tool_description="Generates the Transitions.json file based on the prototype description and existing elements."
            ),
            states_agent.as_tool(
                tool_name="states_JSON_generator",
                tool_description="Generates the States.json file based on the prototype description and existing elements."
            ),
            visualization_elements_agent.as_tool(
                tool_name="visualization_elements_JSON_generator",
                tool_description="Generates the VisualizationElements.json file based on the prototype description and existing elements."),
            visualization_arrays_agent.as_tool(
                tool_name="visualization_arrays_JSON_generator",
                tool_description="Generates the VisualizationArrays.json file based on the prototype description and existing elements.")
        ],
        output_type=FunctionalSpecification
    )


    result = Runner.run_streamed(
        manager_agent, input=USER_INPUT
    )
    async for event in result.stream_events():
        # We'll ignore the raw responses event deltas
        if event.type == "raw_response_event":
            continue
        # When the agent updates, print that
        elif event.type == "agent_updated_stream_event":
            print(f"Agent updated: {event.new_agent.name}")
            continue
        # When items are generated, print them
        elif event.type == "run_item_stream_event":
            if event.item.type == "tool_call_item":
                print("-- Tool was called")
            elif event.item.type == "tool_call_output_item":
                print(f"-- Tool output: ")
                print(json.dumps(json.loads(event.item.output), indent=4, ensure_ascii=False))
            elif event.item.type == "message_output_item":
                print(f"-- Message output:\n {ItemHelpers.text_message_output(event.item)}")
            else:
                pass  # Ignore other event types

    print("=== Run complete ===")

