BEGIN TRANSACTION;

CREATE TABLE IF NOT EXISTS "FeatureFlags"
(
  "Name" TEXT PRIMARY KEY,
  "Owner" TEXT NOT NULL DEFAULT '',
  "TagsJson" TEXT NOT NULL DEFAULT '[]',
  "RequirementType" INTEGER NOT NULL,
  "Version" INTEGER NOT NULL DEFAULT 1,
  "UpdatedAtUtc" TEXT NOT NULL,
  "ScheduledAtUtc" TEXT
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

CREATE TABLE IF NOT EXISTS "FeatureFlagAuditLogs"
(
  "Id" INTEGER PRIMARY KEY AUTOINCREMENT,
  "FeatureFlagName" TEXT NOT NULL,
  "Action" INTEGER NOT NULL,
  "SnapshotVersion" INTEGER NOT NULL,
  "SnapshotJson" TEXT NOT NULL,
  "ChangedAtUtc" TEXT NOT NULL,
  "ChangedBy" TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS "IX_FeatureFlagAuditLogs_FeatureFlagName_Id"
  ON "FeatureFlagAuditLogs" ("FeatureFlagName", "Id");

CREATE TABLE IF NOT EXISTS "FeatureFlagActivityEntries"
(
  "Id" INTEGER PRIMARY KEY AUTOINCREMENT,
  "FeatureFlagName" TEXT NOT NULL,
  "ActivityType" TEXT NOT NULL,
  "Description" TEXT NOT NULL,
  "ChangeType" TEXT,
  "ChangedAtUtc" TEXT NOT NULL,
  "ChangedBy" TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS "IX_FeatureFlagActivityEntries_FeatureFlagName_Id"
  ON "FeatureFlagActivityEntries" ("FeatureFlagName", "Id");

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

