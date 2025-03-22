-- Run this script before running the 20250317181033_UserEmailChangeUpdates migration.

UPDATE crypter."User"
    SET "EmailAddress" = null
    WHERE "EmailVerified" = false;