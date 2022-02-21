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

-- Table: public.User

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
