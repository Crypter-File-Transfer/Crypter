/*
 * Copyright (C) 2022 Crypter File Transfer
 * 
 * This file is part of the Crypter file transfer project.
 * 
 * Crypter is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * The Crypter source code is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 * 
 * You should have received a copy of the GNU Affero General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 * 
 * You can be released from the requirements of the aforementioned license
 * by purchasing a commercial license. Buying such a license is mandatory
 * as soon as you develop commercial activities involving the Crypter source
 * code without disclosing the source code of your own applications.
 * 
 * Contact the current copyright holder to discuss commercial license options.
 */

BEGIN;

   -- Drop BetaKey

   DROP TABLE IF EXISTS public."BetaKey";

   -- Rename UserPrivacy

   ALTER TABLE IF EXISTS public."UserPrivacy" RENAME TO "UserPrivacySetting";

   -- Add foreign key constraint to UserPrivacySetting

   ALTER TABLE IF EXISTS public."UserPrivacySetting" 
      ADD CONSTRAINT "FK_UserPrivacySetting_User_Owner" FOREIGN KEY ("Owner")
      REFERENCES public."User" ("Id") MATCH SIMPLE
      ON UPDATE NO ACTION
      ON DELETE CASCADE;

   -- Add foreign key constraint to UserEd25519KeyPair

   ALTER TABLE IF EXISTS public."UserEd25519KeyPair"
      ADD CONSTRAINT "FK_UserEd25519KeyPair_User_Owner" FOREIGN KEY ("Owner")
      REFERENCES public."User" ("Id") MATCH SIMPLE
      ON UPDATE NO ACTION
      ON DELETE CASCADE;

   -- Drop unique constraint on UserEd25519KeyPair.Owner

   DROP INDEX IF EXISTS public."IX_UserEd25519KeyPair_Owner";

   -- Add foreign key constraint to UserX25519KeyPair

   ALTER TABLE IF EXISTS public."UserX25519KeyPair"
      ADD CONSTRAINT "FK_UserX25519KeyPair_User_Owner" FOREIGN KEY ("Owner")
      REFERENCES public."User" ("Id") MATCH SIMPLE
      ON UPDATE NO ACTION
      ON DELETE CASCADE;

   -- Drop unique constraint on UserX25519KeyPair.Owner

   DROP INDEX IF EXISTS public."IX_UserX25519KeyPair_Owner";

   -- Add unique constraints on User.Username and User.Email

   CREATE UNIQUE INDEX IF NOT EXISTS user_username_unique
         ON public."User" USING btree
         (LOWER("Username") COLLATE pg_catalog."default" ASC NULLS LAST)
         TABLESPACE pg_default;

   CREATE UNIQUE INDEX IF NOT EXISTS user_email_unique
         ON public."User" USING btree
         (LOWER("Email") COLLATE pg_catalog."default" ASC NULLS LAST)
         TABLESPACE pg_default;

   -- Create UserNotificationSetting table

   CREATE TABLE IF NOT EXISTS public."UserNotificationSetting"
   (
         "Owner" uuid NOT NULL,
         "EnableTransferNotifications" boolean NOT NULL,
         "EmailNotifications" boolean NOT NULL,
         CONSTRAINT "PK_UserNotificationSetting" PRIMARY KEY ("Owner"),
         CONSTRAINT "FK_UserNotificationSetting_User_Owner" FOREIGN KEY ("Owner")
            REFERENCES public."User" ("Id") MATCH SIMPLE
            ON UPDATE NO ACTION
            ON DELETE CASCADE
   )

   TABLESPACE pg_default;

   ALTER TABLE IF EXISTS public."UserNotificationSetting"
         OWNER to postgres;

   GRANT DELETE, INSERT, SELECT, UPDATE ON TABLE public."UserNotificationSetting" TO cryptuser;

   GRANT ALL ON TABLE public."UserNotificationSetting" TO postgres;

   -- Create UserEmailVerification table

   CREATE TABLE IF NOT EXISTS public."UserEmailVerification"
   (
         "Owner" uuid NOT NULL,
         "Code" uuid NOT NULL,
         "VerificationKey" bytea,
         "Created" timestamp without time zone NOT NULL,
         CONSTRAINT "PK_UserEmailVerification" PRIMARY KEY ("Owner"),
         CONSTRAINT "FK_UserEmailVerification_User_Owner" FOREIGN KEY ("Owner")
            REFERENCES public."User" ("Id") MATCH SIMPLE
            ON UPDATE NO ACTION
            ON DELETE CASCADE
   )

   TABLESPACE pg_default;

   ALTER TABLE IF EXISTS public."UserEmailVerification"
         OWNER to postgres;

   GRANT DELETE, INSERT, SELECT, UPDATE ON TABLE public."UserEmailVerification" TO cryptuser;

   GRANT ALL ON TABLE public."UserEmailVerification" TO postgres;

   -- Create Schema table

   CREATE TABLE IF NOT EXISTS public."Schema"
   (
         "Version" integer NOT NULL,
         "Updated" timestamp without time zone NOT NULL
   )

   TABLESPACE pg_default;

   ALTER TABLE IF EXISTS public."Schema"
         OWNER to postgres;

   GRANT DELETE, INSERT, SELECT, UPDATE ON TABLE public."Schema" TO cryptuser;

   GRANT ALL ON TABLE public."Schema" TO postgres;

   -- Insert current schema version

   INSERT INTO public."Schema" ("Version", "Updated")
      VALUES (1, CURRENT_TIMESTAMP);

COMMIT;
