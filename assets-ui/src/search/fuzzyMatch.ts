import Fuse from "fuse.js";
import { AssetDto } from "../types/Asset";

/**
 * Threshold tuned to be forgiving of common typos (missing/extra/swapped
 * letters) without becoming so loose that unrelated words match. 0.0 is an
 * exact match, 1.0 matches anything - 0.4 sits in the "typo-tolerant but
 * still meaningful" range recommended by Fuse's own docs for short strings.
 */
const FUZZY_THRESHOLD = 0.4;

/**
 * Fuzzy-matches a free-text query against a list of known asset type names
 * (e.g. ["WindTurbine", "SolarPanel"]) and returns the best match, or null
 * if nothing was close enough. This is what makes "sloar"/"solra" resolve
 * to "SolarPanel".
 */
export function matchAssetType(query: string, knownTypes: string[]): string | null {
  if (!query.trim()) return null;

  const fuse = new Fuse(
    knownTypes.map((type) => ({ type })),
    { keys: ["type"], threshold: FUZZY_THRESHOLD, includeScore: true }
  );

  const results = fuse.search(query);
  return results.length > 0 ? results[0].item.type : null;
}

/**
 * Fuzzy-filters the full asset list against a free-text query, matching on
 * asset type and on every field value (so a typo'd meter point id or a
 * typo'd type name both still surface the right rows). Empty query returns
 * everything unfiltered.
 */
export function searchAssets(assets: AssetDto[], query: string): AssetDto[] {
  if (!query.trim()) return assets;

  const searchable = assets.map((asset) => ({
    asset,
    type: asset.type,
    fieldValues: Object.values(asset.fields).map(String).join(" "),
  }));

  const fuse = new Fuse(searchable, {
    keys: ["type", "fieldValues"],
    threshold: FUZZY_THRESHOLD,
    includeScore: true,
  });

  return fuse.search(query).map((result) => result.item.asset);
}
