BEGIN TRANSACTION;

CREATE TABLE IF NOT EXISTS "FeatureFlagExperiments"
(
  "Id" INTEGER PRIMARY KEY AUTOINCREMENT,
  "FeatureFlagName" TEXT NOT NULL,
  "BaselineVariant" TEXT NOT NULL,
  "ChallengerVariant" TEXT NOT NULL,
  "BaselineTrafficPercentage" INTEGER NOT NULL,
  "ChallengerTrafficPercentage" INTEGER NOT NULL,
  "ConversionMetricName" TEXT NOT NULL,
  "LatencyMetricName" TEXT NOT NULL,
  "MinimumSampleSize" INTEGER NOT NULL,
  "IsActive" INTEGER NOT NULL,
  "CreatedAtUtc" TEXT NOT NULL,
  "UpdatedAtUtc" TEXT NOT NULL,
  CONSTRAINT "FK_FeatureFlagExperiments_FeatureFlags_FeatureFlagName"
    FOREIGN KEY ("FeatureFlagName")
    REFERENCES "FeatureFlags"("Name")
    ON DELETE CASCADE
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_FeatureFlagExperiments_FeatureFlagName"
  ON "FeatureFlagExperiments" ("FeatureFlagName");

CREATE TABLE IF NOT EXISTS "FeatureFlagExperimentVariantMetrics"
(
  "Id" INTEGER PRIMARY KEY AUTOINCREMENT,
  "FeatureFlagExperimentId" INTEGER NOT NULL,
  "Variant" TEXT NOT NULL,
  "SampleSize" INTEGER NOT NULL,
  "ConversionCount" INTEGER NOT NULL,
  "ErrorCount" INTEGER NOT NULL,
  "TotalLatencyMs" REAL NOT NULL,
  CONSTRAINT "FK_FeatureFlagExperimentVariantMetrics_FeatureFlagExperiments_FeatureFlagExperimentId"
    FOREIGN KEY ("FeatureFlagExperimentId")
    REFERENCES "FeatureFlagExperiments"("Id")
    ON DELETE CASCADE
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_FeatureFlagExperimentVariantMetrics_ExperimentId_Variant"
  ON "FeatureFlagExperimentVariantMetrics" ("FeatureFlagExperimentId", "Variant");

COMMIT;

