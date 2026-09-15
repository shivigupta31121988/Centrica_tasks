export interface TypoCase {
  input: string;
  expected: string;
}

export const KNOWN_ASSET_TYPES = ["WindTurbine", "SolarPanel"];

export const TYPO_CASES: TypoCase[] = [
  { input: "solar", expected: "SolarPanel" },
  { input: "Solar", expected: "SolarPanel" },
  { input: "sloar", expected: "SolarPanel" },
  { input: "solra", expected: "SolarPanel" },
  { input: "solr", expected: "SolarPanel" },
  { input: "solaar", expected: "SolarPanel" },
  { input: "SOLAR", expected: "SolarPanel" },
  { input: "wind", expected: "WindTurbine" },
  { input: "wnd", expected: "WindTurbine" },
  { input: "windd", expected: "WindTurbine" },
  { input: "wind turbine", expected: "WindTurbine" },
  { input: "turbine", expected: "WindTurbine" },
  { input: "trubine", expected: "WindTurbine" },
];
