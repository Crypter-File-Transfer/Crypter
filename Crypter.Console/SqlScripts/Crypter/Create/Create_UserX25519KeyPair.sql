--Table: public.UserX25519KeyPair

CREATE TABLE IF NOT EXISTS public."UserX25519KeyPair"
(
    "Id" uuid NOT NULL,
    "Owner" uuid NOT NULL,
    "PrivateKey" text COLLATE pg_catalog."default",
    "PublicKey" text COLLATE pg_catalog."default",
    "Created" timestamp without time zone NOT NULL,
    CONSTRAINT "PK_UserX25519KeyPair" PRIMARY KEY("Id"),
    CONSTRAINT "FK_UserX25519KeyPair_User_Owner" FOREIGN KEY("Owner")
        REFERENCES public."User"("Id") MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE CASCADE
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public."UserX25519KeyPair"
    OWNER to postgres;

GRANT DELETE, INSERT, SELECT, UPDATE ON TABLE public."UserX25519KeyPair" TO cryptuser;

GRANT ALL ON TABLE public."UserX25519KeyPair" TO postgres;
