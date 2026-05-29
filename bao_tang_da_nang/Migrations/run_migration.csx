#!/usr/bin/env dotnet-script
#r "nuget: Npgsql, 8.0.3"

using Npgsql;
using System;

var connStr = "Server=dpg-d8cegn1kh4rs73brgp40-a.oregon-postgres.render.com;Port=5432;Database=dnfam;User Id=dnfam_user;Password=sh9QPwyTLG53hjUiuPgg0VPZVfhnRpzX;Ssl Mode=Require;Trust Server Certificate=true;";

var conn = new NpgsqlConnection(connStr);
conn.Open();
Console.WriteLine("Ket noi database thanh cong.");

var sql = @"
CREATE TABLE IF NOT EXISTS ""AdminEmails"" (
    ""Id""        SERIAL          PRIMARY KEY,
    ""Email""     VARCHAR(255)    NOT NULL,
    ""FullName""  VARCHAR(200)    NOT NULL,
    ""AddedAt""   TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ""IsActive""  BOOLEAN         NOT NULL DEFAULT TRUE,
    CONSTRAINT ""UQ_AdminEmails_Email"" UNIQUE (""Email"")
);

CREATE INDEX IF NOT EXISTS ""IX_AdminEmails_Email"" ON ""AdminEmails"" (""Email"");

CREATE TABLE IF NOT EXISTS ""AdminOtps"" (
    ""Id""        SERIAL          PRIMARY KEY,
    ""Email""     VARCHAR(255)    NOT NULL,
    ""OtpCode""   CHAR(6)         NOT NULL,
    ""ExpiresAt"" TIMESTAMP       NOT NULL,
    ""IsUsed""    BOOLEAN         NOT NULL DEFAULT FALSE,
    ""CreatedAt"" TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS ""IX_AdminOtps_Email"" ON ""AdminOtps"" (""Email"");
";

var cmd = new NpgsqlCommand(sql, conn);
cmd.ExecuteNonQuery();
Console.WriteLine("Da tao bang AdminEmails va AdminOtps thanh cong!");
conn.Close();
