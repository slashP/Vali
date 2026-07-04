// Minimal starter map definition inserted by "New from template".
export const STARTER_DEFINITION = `{
  "countryCodes": ["NO"],
  "distributionStrategy": {
    "key": "FixedCountByMaxMinDistance",
    "locationCountGoal": 5000,
    "minMinDistance": 100
  },
  "globalLocationFilter": ""
}
`;

/** Preview runs use a fixed, small goal for now. */
export const PREVIEW_GOAL = 3000;
