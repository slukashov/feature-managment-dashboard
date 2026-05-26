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

COMMIT;

