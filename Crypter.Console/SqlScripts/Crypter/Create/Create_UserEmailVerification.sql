-- Table: public.UserEmailVerification

-- DROP TABLE IF EXISTS public."UserEmailVerification";

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
