-- Table: public.Schema

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
