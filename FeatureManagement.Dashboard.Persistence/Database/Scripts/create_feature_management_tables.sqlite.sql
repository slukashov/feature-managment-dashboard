BEGIN TRANSACTION;

CREATE TABLE IF NOT EXISTS "FeatureFlags"
(
  "Name" TEXT PRIMARY KEY,
  "RequirementType" INTEGER NOT NULL,
  "Version" INTEGER NOT NULL DEFAULT 1,
  "UpdatedAtUtc" TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS "FeatureFilters"
(
  "Id" INTEGER PRIMARY KEY AUTOINCREMENT,
  "Name" TEXT NOT NULL,
  "FeatureFlagName" TEXT NOT NULL,
  "ParametersJson" TEXT NOT NULL DEFAULT '{}',
  CONSTRAINT "FK_FeatureFilters_FeatureFlags_FeatureFlagName"
    FOREIGN KEY ("FeatureFlagName")
    REFERENCES "FeatureFlags"("Name")
    ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_FeatureFilters_FeatureFlagName"
  ON "FeatureFilters" ("FeatureFlagName");

COMMIT;

