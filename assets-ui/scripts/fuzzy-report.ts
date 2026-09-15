/**
 * Standalone fuzzy-match accuracy report.
 *
 * Run with: npm run test:fuzzy-report
 *
 * This is deliberately separate from the Jest suite (src/__tests__/fuzzySearch.test.ts):
 * Jest gives a pass/fail CI gate, this script gives a human-readable
 * accuracy percentage and a list of any misses - useful when extending
 * TYPO_CASES to see how the match rate trends over time, without that
 * being a pipeline failure on its own.
 */
import { matchAssetType } from "../src/search/fuzzyMatch";
import { KNOWN_ASSET_TYPES, TYPO_CASES } from "../src/search/typoTestCases";

function run() {
  let passed = 0;
  const failures: Array<{ input: string; expected: string; actual: string | null }> = [];

  for (const testCase of TYPO_CASES) {
    const actual = matchAssetType(testCase.input, KNOWN_ASSET_TYPES);
    if (actual === testCase.expected) {
      passed += 1;
    } else {
      failures.push({ input: testCase.input, expected: testCase.expected, actual });
    }
  }

  const accuracy = ((passed / TYPO_CASES.length) * 100).toFixed(1);

  console.log(`Fuzzy match report: ${passed}/${TYPO_CASES.length} (${accuracy}%) typo cases matched correctly.\n`);

  if (failures.length > 0) {
    console.log("Misses:");
    for (const f of failures) {
      console.log(`  "${f.input}" -> expected "${f.expected}", got "${f.actual ?? "null"}"`);
    }
    process.exitCode = 1;
  } else {
    console.log("All typo cases matched as expected.");
  }
}

run();
