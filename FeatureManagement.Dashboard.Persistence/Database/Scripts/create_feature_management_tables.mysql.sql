START TRANSACTION;

CREATE TABLE IF NOT EXISTS `FeatureFlags`
(
  `Name` varchar(255) NOT NULL,
  `RequirementType` int NOT NULL,
  `Version` int NOT NULL DEFAULT 1,
  `UpdatedAtUtc` datetime(6) NOT NULL,
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

COMMIT;

