from typing import Any, Dict, List, Optional

from pydantic import BaseModel, Field


class TransitionHint(BaseModel):
    """Lightweight hint that links a trigger to a state change for transition planning."""

    trigger: str = Field(..., description="Name of the trigger, e.g., LeverPressed or Timeout")
    source_state: Optional[str] = Field(
        None, description="Optional originating state before the transition."
    )
    destination_state: Optional[str] = Field(
        None, description="Optional target state after the transition."
    )
    action: Optional[str] = Field(None, description="Human-friendly summary of the effect.")
    notes: Optional[str] = Field(
        None,
        description="Any supplemental guidance, such as guard conditions or timing details.",
    )


class ScenarioPlan(BaseModel):
    """
    High-level plan extracted from a natural language scenario to guide Vivian JSON generation.

    This structure bridges user intent to the downstream InteractionElements, States, Transitions,
    and Visualization artifacts. Field names are reused verbatim across subsequent JSON files.
    """

    objects: List[str] = Field(
        default_factory=list,
        description="Domain objects relevant to the scenario (e.g., Toaster, Lever, IndicatorLight).",
    )
    states: List[str] = Field(
        default_factory=list,
        description="Logical system states (e.g., Idle, Toasting, Error).",
    )
    interactions: List[str] = Field(
        default_factory=list,
        description="User-facing interaction elements (e.g., Lever, CancelButton).",
    )
    visualizations: List[str] = Field(
        default_factory=list,
        description="Visualization elements and outputs (e.g., IndicatorLight, Screen).",
    )
    events: List[Dict[str, Any]] = Field(
        default_factory=list,
        description=(
            "Event-like definitions tying triggers to actions or state changes, "
            'e.g., {"trigger": "LeverPressed", "action": "start toasting"}.'
        ),
    )
    initial_state: Optional[str] = Field(
        None, description="Optional entry point state for the scenario."
    )
    final_states: List[str] = Field(
        default_factory=list,
        description="Terminal states that conclude the scenario, if any.",
    )
    transition_hints: List[TransitionHint] = Field(
        default_factory=list,
        description=(
            "Optional structured hints to map triggers to transitions and destinations."
        ),
    )
