
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
  `Password` VARCHAR(64),
  `Email` VARCHAR(50), 
  `Authenticated` TINYINT(1),
  `UserCreated` TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL, 
  PRIMARY KEY (UserID)
 ) ENGINE=InnoDB;

CREATE TABLE `MessageUploads` (
  `ID` VARCHAR(36) NOT NULL,
  `UserID` VARCHAR(36) NOT NULL,
  `UntrustedName` VARCHAR(100),
  `Size` INT DEFAULT NULL,
  `EncryptedMessagePath` VARCHAR(256),
  `Signature` TEXT,
  `Created` TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL, 
  `ExpirationDate` TIMESTAMP NOT NULL,
  PRIMARY KEY (ID),
  /*Commented out for testing
  FOREIGN KEY (UserID) REFERENCES Users(UserID)*/
) ENGINE=InnoDB; 

CREATE TABLE `FileUploads` (
  `ID` VARCHAR(36) NOT NULL,
  `UserID` VARCHAR(36) NOT NULL,
  `UntrustedName` VARCHAR(100),
  `EncryptedFileContentPath` VARCHAR(256), 
  `Signature` TEXT,
  `Size` INT DEFAULT NULL,
  `Created` TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL, 
  `ExpirationDate` TIMESTAMP NOT NULL,
  PRIMARY KEY (ID),
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
  PRIMARY KEY(KeyID), 
  /*Commented out for testing FOREIGN KEY(UserID) REFERENCES Users(UserID)*/
) ENGINE=InnoDB;

CREATE TABLE `ExchangedKeys` (
  `UserID` VARCHAR(36) NOT NULL,
  `ExchangedKey` TEXT, 
  `OtherUserID` VARCHAR(36) NOT NULL, 
  `OtherUserExchangedKey` TEXT,
  `ExchangeCreated` TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL, 
   PRIMARY KEY(UserID, OtherUserID), 
   CONSTRAINT KeyExchange UNIQUE (UserID, OtherUserID),
   /*Commented out for testing
   CONSTRAINT ExchangeToUser1_FK FOREIGN KEY (UserID) REFERENCES Users(UserID), 
   CONSTRAINT ExchangeToUser2_FK FOREIGN KEY (OtherUserID) REFERENCES Users(UserID)
   */
) ENGINE=InnoDB;

--disble strict mode to allow ID non auto-increment
SET GLOBAL sql_mode = ''; 

