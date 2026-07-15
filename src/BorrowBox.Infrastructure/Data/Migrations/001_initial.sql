CREATE TABLE Equipment (
    Id TEXT NOT NULL CONSTRAINT PK_Equipment PRIMARY KEY,
    AssetTag TEXT NOT NULL,
    Name TEXT NOT NULL,
    Category TEXT NOT NULL,
    Status TEXT NOT NULL
);

CREATE UNIQUE INDEX IX_Equipment_AssetTag ON Equipment (AssetTag);
