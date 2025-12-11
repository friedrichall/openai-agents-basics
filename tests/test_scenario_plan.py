from model.output_type_ScenarioPlan import ScenarioPlan, TransitionHint


def test_scenario_plan_toaster_example_fields():
    plan = ScenarioPlan(
        objects=["Toaster", "Toast", "Lever", "CancelButton", "IndicatorLight"],
        states=["Idle", "Toasting"],
        interactions=["Lever", "CancelButton"],
        visualizations=["IndicatorLight"],
        events=[
            {"trigger": "LeverPressed", "action": "start toasting", "destination_state": "Toasting"},
            {"trigger": "CancelButtonPressed", "action": "stop toasting", "destination_state": "Idle"},
            {"trigger": "Timeout", "action": "finish cycle", "destination_state": "Idle"},
        ],
        initial_state="Idle",
        final_states=["Idle"],
        transition_hints=[
            TransitionHint(trigger="LeverPressed", source_state="Idle", destination_state="Toasting"),
            TransitionHint(trigger="CancelButtonPressed", source_state="Toasting", destination_state="Idle"),
            TransitionHint(trigger="Timeout", source_state="Toasting", destination_state="Idle"),
        ],
    )

    assert plan.initial_state == "Idle"
    assert "Toasting" in plan.states
    assert plan.interactions == ["Lever", "CancelButton"]
    assert any(h.destination_state == "Toasting" for h in plan.transition_hints)
    assert plan.events[0]["trigger"] == "LeverPressed"
