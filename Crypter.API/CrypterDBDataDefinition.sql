
SET FOREIGN_KEY_CHECKS = 0;
DROP TABLE IF EXISTS `MessageUploads`; 
DROP TABLE IF EXISTS `FileUploads`; 
DROP TABLE IF EXISTS `ExchangedKeys`; 
DROP TABLE IF EXISTS `Keys`; 
DROP TABLE IF EXISTS `Users`; 
SET FOREIGN_KEY_CHECKS = 1;


CREATE TABLE `Users` (
  `UserId` CHAR(36) NOT NULL UNIQUE,
  `UserName` VARCHAR(32) UNIQUE, 
  `Password` VARCHAR(64),
  `Email` VARCHAR(50), 
  `Authenticated` TINYINT(1),
  `UserCreated` TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL, 
  PRIMARY KEY (UserId)
 ) ENGINE=InnoDB;

CREATE TABLE `MessageUploads` (
  `Id` CHAR(36) NOT NULL UNIQUE,
  `UserID` CHAR(36) NOT NULL,
  `UntrustedName` VARCHAR(100),
  `Size` INT DEFAULT NULL,
  `EncryptedMessagePath` VARCHAR(256),
  `Signature` TEXT,
  `Created` TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL, 
  `ExpirationDate` TIMESTAMP NOT NULL,
  PRIMARY KEY (Id), 
  FOREIGN KEY (UserId) REFERENCES Users(UserId)
) ENGINE=InnoDB; 

CREATE TABLE `FileUploads` (
  `Id` CHAR(36) NOT NULL UNIQUE,
  `UserID` CHAR(36) NOT NULL,
  `UntrustedName` VARCHAR(100),
  `EncryptedFileContentPath` VARCHAR(256), 
  `Signature` TEXT,
  `Size` INT DEFAULT NULL,
  `Created` TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL, 
  `ExpirationDate` TIMESTAMP NOT NULL,
  PRIMARY KEY (Id), 
  FOREIGN KEY (UserId) REFERENCES Users(UserId)
) ENGINE=InnoDB;


CREATE TABLE `Keys` (
  `KeyId` CHAR(36) NOT NULL UNIQUE,
  `UserId` CHAR(36), 
  `PrivateKey` TEXT, 
  `PublicKey` TEXT, 
  `KeyType` VARCHAR(25),
  `Created` TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL, 
  PRIMARY KEY(KeyId), 
  FOREIGN KEY(UserId) REFERENCES Users(UserId)
) ENGINE=InnoDB;

CREATE TABLE `ExchangedKeys` (
  `UserId` char(36) NOT NULL,
  `ExchangedKey` TEXT, 
  `OtherUserId` char(36) NOT NULL, 
  `OtherUserExchangedKey` TEXT,
  `ExchangeCreated` TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL, 
   PRIMARY KEY(UserId, OtherUserId), 
   CONSTRAINT KeyExchange UNIQUE (UserId, OtherUserId), 
   CONSTRAINT ExchangeToUser1_FK FOREIGN KEY (UserId) REFERENCES Users(UserId), 
   CONSTRAINT ExchangeToUser2_FK FOREIGN KEY (OtherUserId) REFERENCES Users(UserId)
) ENGINE=InnoDB;


