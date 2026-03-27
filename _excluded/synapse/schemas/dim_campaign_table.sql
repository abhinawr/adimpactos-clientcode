-- Campaign Dimension Table
-- Stores campaign metadata and targeting criteria

CREATE TABLE dbo.dim_campaign
(
    campaign_id VARCHAR(50) NOT NULL,
    campaign_name VARCHAR(200) NOT NULL,
    advertiser VARCHAR(100),
    start_date DATE NOT NULL,
    end_date DATE,
    budget_usd DECIMAL(18, 2),
    target_impressions BIGINT,
    status VARCHAR(20),
    created_date DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    last_updated DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT PK_dim_campaign PRIMARY KEY (campaign_id)
)
WITH
(
    DISTRIBUTION = REPLICATE
);

CREATE NONCLUSTERED INDEX IX_dim_campaign_dates 
ON dbo.dim_campaign(start_date, end_date);
