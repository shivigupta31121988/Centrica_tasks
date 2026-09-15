import { matchAssetType, searchAssets } from "../search/fuzzyMatch";
import { AssetDto } from "../types/Asset";
import { KNOWN_ASSET_TYPES, TYPO_CASES } from "../search/typoTestCases";

/**
 * TYPO_CASES lives in ../search/typoTestCases.ts and is also what
 * scripts/fuzzy-report.ts runs standalone to print a match-accuracy
 * percentage - single source of truth so the two never drift apart.
 */
describe("matchAssetType (fuzzy asset-type matching)", () => {
  it.each(TYPO_CASES)("matches '$input' to $expected", ({ input, expected }) => {
    expect(matchAssetType(input, KNOWN_ASSET_TYPES)).toBe(expected);
  });

  it("returns null for an empty query", () => {
    expect(matchAssetType("", KNOWN_ASSET_TYPES)).toBeNull();
  });

  it("returns null for a query with no reasonable match", () => {
    expect(matchAssetType("xyzzyqqq", KNOWN_ASSET_TYPES)).toBeNull();
  });
});

describe("searchAssets (fuzzy filtering of the asset list)", () => {
  const assets: AssetDto[] = [
    { id: "1", type: "SolarPanel", fields: { capacity: 20, meterPointId: "570715000000088747", compassOrientation: "South" } },
    { id: "2", type: "WindTurbine", fields: { capacity: 150, meterPointId: "570715000000099999", hubHeight: 90, rotorDiameter: 120 } },
  ];

  it("returns all assets for an empty query", () => {
    expect(searchAssets(assets, "")).toHaveLength(2);
  });

  it("finds the solar panel with a typo'd query", () => {
    const results = searchAssets(assets, "sloar");
    expect(results).toHaveLength(1);
    expect(results[0].type).toBe("SolarPanel");
  });

  it("finds the wind turbine with a typo'd query", () => {
    const results = searchAssets(assets, "wnd trbine");
    expect(results).toHaveLength(1);
    expect(results[0].type).toBe("WindTurbine");
  });

  it("finds an asset by a (typo'd) meter point id fragment", () => {
    const results = searchAssets(assets, "088747");
    expect(results.some((a) => a.id === "1")).toBe(true);
  });
});
