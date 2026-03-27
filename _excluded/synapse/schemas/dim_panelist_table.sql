-- Panelist Dimension Table (Synapse copy for analytics)
-- Synchronized from Cosmos DB for join performance

CREATE TABLE dbo.dim_panelist
(
    panelist_id VARCHAR(100) NOT NULL,
    age_range VARCHAR(20),
    gender VARCHAR(10),
    hh_income_bucket VARCHAR(30),
    interests VARCHAR(500),
    consent_gdpr BIT DEFAULT 0,
    consent_ccpa BIT DEFAULT 0,
    last_active DATETIME2,
    points_balance INT DEFAULT 0,
    last_updated DATETIME2 NOT NULL,
    CONSTRAINT PK_dim_panelist PRIMARY KEY (panelist_id)
)
WITH
(
    DISTRIBUTION = REPLICATE
);

-- Index for lookups
CREATE NONCLUSTERED INDEX IX_dim_panelist_demographics 
ON dbo.dim_panelist(age_range, gender, hh_income_bucket);

CREATE STATISTICS STAT_age_range ON dbo.dim_panelist(age_range);
CREATE STATISTICS STAT_gender ON dbo.dim_panelist(gender);
