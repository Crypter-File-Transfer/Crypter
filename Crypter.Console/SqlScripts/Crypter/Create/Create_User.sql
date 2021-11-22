-- Table: public.User

-- DROP TABLE IF EXISTS public."User";

CREATE TABLE IF NOT EXISTS public."User"
(
    "Id" uuid NOT NULL,
    "Username" text COLLATE pg_catalog."default",
    "Email" text COLLATE pg_catalog."default",
    "PasswordHash" bytea,
    "PasswordSalt" bytea,
    "EmailVerified" boolean NOT NULL,
    "Created" timestamp without time zone NOT NULL,
    "LastLogin" timestamp without time zone NOT NULL,
    CONSTRAINT "PK_User" PRIMARY KEY ("Id")
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public."User"
    OWNER to postgres;

GRANT DELETE, INSERT, SELECT, UPDATE ON TABLE public."User" TO cryptuser;

GRANT ALL ON TABLE public."User" TO postgres;
-- Index: user_username_unique

-- DROP INDEX IF EXISTS public.user_username_unique;

CREATE UNIQUE INDEX IF NOT EXISTS user_username_unique
    ON public."User" USING btree
    (LOWER("Username") COLLATE pg_catalog."default" ASC NULLS LAST)
    TABLESPACE pg_default;
-- Index: user_email_unique

-- DROP INDEX IF EXISTS public.user_email_unique;

CREATE UNIQUE INDEX IF NOT EXISTS user_email_unique
    ON public."User" USING btree
    (LOWER("Email") COLLATE pg_catalog."default" ASC NULLS LAST)
    TABLESPACE pg_default;