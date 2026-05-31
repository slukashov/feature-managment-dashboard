START TRANSACTION;

CREATE TABLE IF NOT EXISTS `FeatureFlags`
(
  `Name` varchar(255) NOT NULL,
  `Owner` varchar(255) NOT NULL DEFAULT '',
  `TagsJson` longtext NOT NULL DEFAULT ('[]'),
  `RequirementType` int NOT NULL,
  `Version` int NOT NULL DEFAULT 1,
  `UpdatedAtUtc` datetime(6) NOT NULL,
  `ScheduledAtUtc` datetime(6),
  PRIMARY KEY (`Name`)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `FeatureFilters`
(
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(255) NOT NULL,
  `FeatureFlagName` varchar(255) NOT NULL,
  `ParametersJson` text NOT NULL,
  PRIMARY KEY (`Id`),
  CONSTRAINT `FK_FeatureFilters_FeatureFlags_FeatureFlagName`
    FOREIGN KEY (`FeatureFlagName`)
    REFERENCES `FeatureFlags` (`Name`)
    ON DELETE CASCADE
) ENGINE=InnoDB;

CREATE INDEX IF NOT EXISTS `IX_FeatureFilters_FeatureFlagName`
  ON `FeatureFilters` (`FeatureFlagName`);

CREATE TABLE IF NOT EXISTS `FeatureFlagAuditLogs`
(
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `FeatureFlagName` varchar(255) NOT NULL,
  `Action` int NOT NULL,
  `SnapshotVersion` int NOT NULL,
  `SnapshotJson` longtext NOT NULL,
  `ChangedAtUtc` datetime(6) NOT NULL,
  `ChangedBy` varchar(255) NOT NULL,
  PRIMARY KEY (`Id`),
  INDEX `IX_FeatureFlagAuditLogs_FeatureFlagName_Id` (`FeatureFlagName`, `Id`)
) ENGINE=InnoDB;

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

CREATE TABLE IF NOT EXISTS `FeatureFlagExperiments`
(
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `FeatureFlagName` varchar(255) NOT NULL,
  `BaselineVariant` varchar(255) NOT NULL,
  `ChallengerVariant` varchar(255) NOT NULL,
  `BaselineTrafficPercentage` int NOT NULL,
  `ChallengerTrafficPercentage` int NOT NULL,
  `ConversionMetricName` varchar(255) NOT NULL,
  `LatencyMetricName` varchar(255) NOT NULL,
  `MinimumSampleSize` int NOT NULL,
  `IsActive` tinyint(1) NOT NULL,
  `CreatedAtUtc` datetime(6) NOT NULL,
  `UpdatedAtUtc` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE INDEX `IX_FeatureFlagExperiments_FeatureFlagName` (`FeatureFlagName`),
  CONSTRAINT `FK_FeatureFlagExperiments_FeatureFlags_FeatureFlagName`
    FOREIGN KEY (`FeatureFlagName`)
    REFERENCES `FeatureFlags` (`Name`)
    ON DELETE CASCADE
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `FeatureFlagExperimentVariantMetrics`
(
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `FeatureFlagExperimentId` bigint NOT NULL,
  `Variant` varchar(255) NOT NULL,
  `SampleSize` bigint NOT NULL,
  `ConversionCount` bigint NOT NULL,
  `ErrorCount` bigint NOT NULL,
  `TotalLatencyMs` double NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE INDEX `IX_FeatureFlagExperimentVariantMetrics_ExperimentId_Variant` (`FeatureFlagExperimentId`, `Variant`),
  CONSTRAINT `FK_FeatureFlagExperimentVariantMetrics_FeatureFlagExperiments_FeatureFlagExperimentId`
    FOREIGN KEY (`FeatureFlagExperimentId`)
    REFERENCES `FeatureFlagExperiments` (`Id`)
    ON DELETE CASCADE
) ENGINE=InnoDB;

COMMIT;

