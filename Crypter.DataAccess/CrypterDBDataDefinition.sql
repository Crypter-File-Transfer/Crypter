SET FOREIGN_KEY_CHECKS = 0;
DROP TABLE IF EXISTS `MessageUploads`; 
DROP TABLE IF EXISTS `FileUploads`; 
DROP TABLE IF EXISTS `ExchangedKeys`; 
DROP TABLE IF EXISTS `Keys`; 
DROP TABLE IF EXISTS `Users`; 
SET FOREIGN_KEY_CHECKS = 1;

CREATE TABLE `Users` (
  `UserID` VARCHAR(36) NOT NULL,
  `UserName` VARCHAR(32) UNIQUE, 
  `PasswordHash` VARCHAR(256),
  `PasswordSalt` VARCHAR(256), 
  `PublicAlias` VARCHAR(32) UNIQUE,
  `IsPublic` TINYINT,
  `AllowAnonMessages` TINYINT,
  `AllowAnonFiles` TINYINT, 
  `Email` VARCHAR(50), 
  `UserCreated` TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL, 
  PRIMARY KEY (UserID)
 ) ENGINE=InnoDB;

CREATE TABLE `MessageUploads` (
  `ID` VARCHAR(36) NOT NULL,
  `UserID` VARCHAR(36) NOT NULL,
  `UntrustedName` VARCHAR(100),
  `Size` INT DEFAULT NULL,
  `EncryptedMessagePath` VARCHAR(256),
  `SignaturePath` VARCHAR(256),
  `Created` TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL, 
  `ExpirationDate` TIMESTAMP NOT NULL,
  `Iv` VARCHAR(256),
  `ServerDigest` VARCHAR(256),
  PRIMARY KEY (ID)
  /*Commented out for testing
  FOREIGN KEY (UserID) REFERENCES Users(UserID)*/
) ENGINE=InnoDB; 

CREATE TABLE `FileUploads` (
  `ID` VARCHAR(36) NOT NULL,
  `UserID` VARCHAR(36) NOT NULL,
  `UntrustedName` VARCHAR(100),
  `Size` INT DEFAULT NULL,
  `ContentType` VARCHAR(256),
  `EncryptedFileContentPath` VARCHAR(256), 
  `SignaturePath` VARCHAR(256),
  `Created` TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL, 
  `ExpirationDate` TIMESTAMP NOT NULL,
  `Iv` VARCHAR(256),
  `ServerDigest` VARCHAR(256),
  PRIMARY KEY (ID)
 /*Commented out for testing
  FOREIGN KEY (UserID) REFERENCES Users(UserID)
 */
) ENGINE=InnoDB;


CREATE TABLE `Keys` (
  `KeyID` VARCHAR(36) NOT NULL,
  `UserID` VARCHAR(36), 
  `PrivateKey` TEXT, 
  `PublicKey` TEXT, 
  `KeyType` VARCHAR(25),
  `Created` TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL, 
  PRIMARY KEY(KeyID)
  /*Commented out for testing FOREIGN KEY(UserID) REFERENCES Users(UserID)*/
) ENGINE=InnoDB;
