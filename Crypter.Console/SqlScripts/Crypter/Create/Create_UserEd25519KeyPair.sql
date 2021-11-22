-- Table: public.UserEd25519KeyPair

-- DROP TABLE IF EXISTS public."UserEd25519KeyPair";

CREATE TABLE IF NOT EXISTS public."UserEd25519KeyPair"
(
    "Id" uuid NOT NULL,
    "Owner" uuid NOT NULL,
    "PrivateKey" text COLLATE pg_catalog."default",
    "PublicKey" text COLLATE pg_catalog."default",
    "Created" timestamp without time zone NOT NULL,
    CONSTRAINT "PK_UserEd25519KeyPair" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_UserEd25519KeyPair_User_Owner" FOREIGN KEY ("Owner")
        REFERENCES public."User" ("Id") MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE CASCADE
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public."UserEd25519KeyPair"
    OWNER to postgres;

GRANT DELETE, INSERT, SELECT, UPDATE ON TABLE public."UserEd25519KeyPair" TO cryptuser;

GRANT ALL ON TABLE public."UserEd25519KeyPair" TO postgres;
-- Index: IX_UserEd25519KeyPair_Owner

-- DROP INDEX IF EXISTS public."IX_UserEd25519KeyPair_Owner";

CREATE UNIQUE INDEX IF NOT EXISTS "IX_UserEd25519KeyPair_Owner"
    ON public."UserEd25519KeyPair" USING btree
    ("Owner" ASC NULLS LAST)
    TABLESPACE pg_default;
