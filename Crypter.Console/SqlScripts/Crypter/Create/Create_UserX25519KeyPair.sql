/*
 * Copyright (C) 2021 Crypter File Transfer
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
 * Contact the current copyright holder to discuss commerical license options.
 */

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
