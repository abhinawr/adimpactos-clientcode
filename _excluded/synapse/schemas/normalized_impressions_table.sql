-- Normalized Impression Staging Table
-- This table stores cleaned and enriched impression data from EventHub

CREATE TABLE dbo.normalized_impressions
(
    impression_id VARCHAR(50) NOT NULL,
    timestamp_utc DATETIME2 NOT NULL,
    campaign_id VARCHAR(50) NOT NULL,
    creative_id VARCHAR(50) NOT NULL,
    panelist_id VARCHAR(100) NOT NULL,
    device_type VARCHAR(20),
    country VARCHAR(10),
    is_bot BIT NOT NULL DEFAULT 0,
    ingest_source VARCHAR(20),
    bot_reason VARCHAR(200),
    processed_date DATE NOT NULL,
    CONSTRAINT PK_normalized_impressions PRIMARY KEY (impression_id, processed_date)
)
WITH
(
    DISTRIBUTION = HASH(panelist_id),
    PARTITION (processed_date RANGE RIGHT FOR VALUES 
        ('2024-01-01', '2024-02-01', '2024-03-01', '2024-04-01', 
         '2024-05-01', '2024-06-01', '2024-07-01', '2024-08-01',
         '2024-09-01', '2024-10-01', '2024-11-01', '2024-12-01'))
);

-- Indexes for common queries
CREATE NONCLUSTERED INDEX IX_normalized_impressions_campaign 
ON dbo.normalized_impressions(campaign_id, timestamp_utc);

CREATE NONCLUSTERED INDEX IX_normalized_impressions_panelist 
ON dbo.normalized_impressions(panelist_id, timestamp_utc);

-- Statistics for query optimization
CREATE STATISTICS STAT_campaign_id ON dbo.normalized_impressions(campaign_id);
CREATE STATISTICS STAT_panelist_id ON dbo.normalized_impressions(panelist_id);
CREATE STATISTICS STAT_timestamp ON dbo.normalized_impressions(timestamp_utc);
