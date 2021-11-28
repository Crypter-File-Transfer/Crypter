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