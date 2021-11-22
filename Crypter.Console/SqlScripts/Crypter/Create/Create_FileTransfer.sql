-- Table: public.FileTransfer

CREATE TABLE IF NOT EXISTS public."FileTransfer"
(
    "Id" uuid NOT NULL,
    "FileName" text COLLATE pg_catalog."default",
    "ContentType" text COLLATE pg_catalog."default",
    "Sender" uuid NOT NULL,
    "Recipient" uuid NOT NULL,
    "Size" integer NOT NULL,
    "ClientIV" text COLLATE pg_catalog."default",
    "Signature" text COLLATE pg_catalog."default",
    "X25519PublicKey" text COLLATE pg_catalog."default",
    "Ed25519PublicKey" text COLLATE pg_catalog."default",
    "ServerIV" bytea,
    "ServerDigest" bytea,
    "Created" timestamp without time zone NOT NULL,
    "Expiration" timestamp without time zone NOT NULL,
    CONSTRAINT "PK_FileTransfer" PRIMARY KEY ("Id")
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public."FileTransfer"
    OWNER to postgres;

GRANT DELETE, INSERT, SELECT, UPDATE ON TABLE public."FileTransfer" TO cryptuser;

GRANT ALL ON TABLE public."FileTransfer" TO postgres;
