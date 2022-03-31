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

-- Table: public.MessageTransfer

CREATE TABLE IF NOT EXISTS public."MessageTransfer"
(
    "Id" uuid NOT NULL,
    "Subject" text COLLATE pg_catalog."default",
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
    CONSTRAINT "PK_MessageTransfer" PRIMARY KEY ("Id")
)

TABLESPACE pg_default;

CREATE INDEX "Idx_MessageTransfer_Sender" ON public."MessageTransfer"("Sender");
CREATE INDEX "Idx_MessageTransfer_Recipient" ON public."MessageTransfer"("Recipient");

ALTER TABLE IF EXISTS public."MessageTransfer"
    OWNER to postgres;

GRANT DELETE, INSERT, SELECT, UPDATE ON TABLE public."MessageTransfer" TO cryptuser;

GRANT ALL ON TABLE public."MessageTransfer" TO postgres;
