-- Table: public.UserProfile

-- DROP TABLE IF EXISTS public."UserProfile";

CREATE TABLE IF NOT EXISTS public."UserProfile"
(
    "Owner" uuid NOT NULL,
    "Alias" text COLLATE pg_catalog."default",
    "About" text COLLATE pg_catalog."default",
    "Image" text COLLATE pg_catalog."default",
    CONSTRAINT "PK_UserProfile" PRIMARY KEY ("Owner"),
    CONSTRAINT "FK_UserProfile_User_Owner" FOREIGN KEY ("Owner")
        REFERENCES public."User" ("Id") MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE CASCADE
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public."UserProfile"
    OWNER to postgres;

GRANT DELETE, INSERT, SELECT, UPDATE ON TABLE public."UserProfile" TO cryptuser;

GRANT ALL ON TABLE public."UserProfile" TO postgres;
