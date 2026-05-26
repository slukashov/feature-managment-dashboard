-- SQLite Migration Script
-- Adds new columns and tables for scheduled rollouts and activity feed
-- Version: 1.0
-- Date: 2026-05-26

BEGIN TRANSACTION;

-- Add ScheduledAtUtc column to FeatureFlags table
ALTER TABLE "FeatureFlags" ADD COLUMN "ScheduledAtUtc" TEXT;

-- Create FeatureFlagActivityEntries table if it doesn't exist
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

-- Create index if it doesn't exist
CREATE INDEX IF NOT EXISTS "IX_FeatureFlagActivityEntries_FeatureFlagName_Id"
  ON "FeatureFlagActivityEntries" ("FeatureFlagName", "Id");

COMMIT;


