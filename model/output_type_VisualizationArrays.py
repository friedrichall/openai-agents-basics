from pydantic import BaseModel, Field
from typing import List


class VisualizationArrayItem(BaseModel):
    """Empty object placeholder for visualization array items."""

    model_config = {"extra": "forbid"}


class VisualizationArrays(BaseModel):
    """Schema: {"Elements": []} with strictly typed item objects."""

    model_config = {"extra": "forbid"}
    Elements: List[VisualizationArrayItem] = Field(default_factory=list)
