-- Table: public.UserNotificationSetting

CREATE TABLE IF NOT EXISTS public."UserNotificationSetting"
(
    "Owner" uuid NOT NULL,
    "EnableTransferNotifications" boolean NOT NULL,
    "EmailNotifications" boolean NOT NULL,
    CONSTRAINT "PK_UserNotificationSetting" PRIMARY KEY ("Owner"),
    CONSTRAINT "FK_UserNotificationSetting_User_Owner" FOREIGN KEY ("Owner")
        REFERENCES public."User" ("Id") MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE CASCADE
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public."UserNotificationSetting"
    OWNER to postgres;

GRANT DELETE, INSERT, SELECT, UPDATE ON TABLE public."UserNotificationSetting" TO cryptuser;

GRANT ALL ON TABLE public."UserNotificationSetting" TO postgres;
