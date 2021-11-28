-- Table: public.UserPrivacySetting

CREATE TABLE IF NOT EXISTS public."UserPrivacySetting"
(
    "Owner" uuid NOT NULL,
    "AllowKeyExchangeRequests" boolean NOT NULL,
    "Visibility" integer NOT NULL,
    "ReceiveFiles" integer NOT NULL,
    "ReceiveMessages" integer NOT NULL,
    CONSTRAINT "PK_UserPrivacySetting" PRIMARY KEY ("Owner"),
    CONSTRAINT "FK_UserPrivacySetting_User_Owner" FOREIGN KEY ("Owner")
        REFERENCES public."User" ("Id") MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE CASCADE
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public."UserPrivacySetting"
    OWNER to postgres;

GRANT DELETE, INSERT, SELECT, UPDATE ON TABLE public."UserPrivacySetting" TO cryptuser;

GRANT ALL ON TABLE public."UserPrivacySetting" TO postgres;
