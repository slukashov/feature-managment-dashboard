START TRANSACTION;

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

