-- MySQL Migration Script
-- Adds new columns and tables for scheduled rollouts and activity feed
-- Version: 1.0
-- Date: 2026-05-26

START TRANSACTION;

-- Add ScheduledAtUtc column to FeatureFlags table
-- If the column already exists, this will fail; you can safely ignore the error
ALTER TABLE IF EXISTS `FeatureFlags` ADD COLUMN IF NOT EXISTS `ScheduledAtUtc` datetime(6);

-- Create FeatureFlagActivityEntries table if it doesn't exist
CREATE TABLE IF NOT EXISTS `FeatureFlagActivityEntries`
(
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `FeatureFlagName` varchar(255) NOT NULL,
  `ActivityType` varchar(255) NOT NULL,
  `Description` longtext NOT NULL,
  `ChangeType` varchar(255),
  `ChangedAtUtc` datetime(6) NOT NULL,
  `ChangedBy` varchar(255) NOT NULL,
  PRIMARY KEY (`Id`),
  INDEX `IX_FeatureFlagActivityEntries_FeatureFlagName_Id` (`FeatureFlagName`, `Id`)
) ENGINE=InnoDB;

COMMIT;

