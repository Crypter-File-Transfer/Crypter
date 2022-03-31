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

BEGIN;

   -- Create UserContact table

   CREATE TABLE IF NOT EXISTS public."UserContact"
   (
       "Id" uuid NOT NULL,
       "Owner" uuid NOT NULL,
       "Contact" uuid NOT NULL,
       CONSTRAINT "PK_UserContact" PRIMARY KEY ("Id"),
       CONSTRAINT "FK_UserContact_User_Contact" FOREIGN KEY ("Contact")
           REFERENCES public."User" ("Id") MATCH SIMPLE
           ON UPDATE NO ACTION
           ON DELETE CASCADE,
       CONSTRAINT "FK_UserContact_User_Owner" FOREIGN KEY ("Owner")
           REFERENCES public."User" ("Id") MATCH SIMPLE
           ON UPDATE NO ACTION
           ON DELETE CASCADE
   )

   TABLESPACE pg_default;

   CREATE INDEX IF NOT EXISTS "IX_UserContact_Owner"
       ON public."UserContact" USING btree
       ("Owner" ASC NULLS LAST)
       TABLESPACE pg_default;

   CREATE INDEX IF NOT EXISTS "IX_UserContact_Contact"
       ON public."UserContact" USING btree
       ("Contact" ASC NULLS LAST)
       TABLESPACE pg_default;

   ALTER TABLE IF EXISTS public."UserContact"
       OWNER to postgres;

   GRANT DELETE, INSERT, SELECT, UPDATE ON TABLE public."UserContact" TO cryptuser;

   GRANT ALL ON TABLE public."UserContact" TO postgres;

   -- Update schema version

   UPDATE public."Schema" SET "Version" = 4, "Updated" = CURRENT_TIMESTAMP;

COMMIT;
