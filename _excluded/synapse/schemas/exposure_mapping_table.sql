-- Exposure Mapping Table Schema for Azure Synapse Analytics
-- Maps panelist impressions to campaigns for lift analysis

CREATE TABLE staging.exposure_mapping
(
    -- Primary Keys
    exposure_id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    panelist_id NVARCHAR(100) NOT NULL,
    impression_id NVARCHAR(100) NOT NULL,
    
    -- Campaign Information
    campaign_id NVARCHAR(100) NOT NULL,
    creative_id NVARCHAR(100) NOT NULL,
    
    -- Temporal Data
    impression_ts DATETIME2 NOT NULL,
    exposure_date DATE NOT NULL,
    
    -- Control/Treatment Assignment
    matched_control_flag BIT NOT NULL DEFAULT 0,
    cohort_type NVARCHAR(20) NOT NULL, -- 'exposed', 'control', 'unmatched'
    
    -- Matching Metadata
    propensity_score FLOAT NULL,
    match_id UNIQUEIDENTIFIER NULL, -- Links exposed to control
    match_quality_score FLOAT NULL,
    
    -- Device and Context
    device_type NVARCHAR(50) NULL,
    country_code NVARCHAR(10) NULL,
    
    -- Bot Detection
    is_bot BIT NOT NULL DEFAULT 0,
    bot_reason NVARCHAR(100) NULL,
    
    -- Audit Fields
    created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    updated_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    processing_batch_id NVARCHAR(100) NULL,
    
    -- Constraints
    CONSTRAINT PK_exposure_mapping PRIMARY KEY NONCLUSTERED (exposure_id),
    CONSTRAINT UQ_impression UNIQUE (impression_id)
)
WITH
(
    DISTRIBUTION = HASH(panelist_id),
    CLUSTERED COLUMNSTORE INDEX
);

-- Indexes for query performance
CREATE INDEX IX_exposure_mapping_panelist ON staging.exposure_mapping(panelist_id) INCLUDE (campaign_id, impression_ts);
CREATE INDEX IX_exposure_mapping_campaign ON staging.exposure_mapping(campaign_id, exposure_date);
CREATE INDEX IX_exposure_mapping_match ON staging.exposure_mapping(match_id) WHERE match_id IS NOT NULL;

-- Partition by date for performance
-- ALTER TABLE staging.exposure_mapping 
-- SWITCH PARTITION $PARTITION.pf_exposure_date(exposure_date) 
-- TO staging.exposure_mapping_archive PARTITION $PARTITION.pf_exposure_date(exposure_date);

-- Statistics for query optimization
CREATE STATISTICS stats_panelist_campaign ON staging.exposure_mapping(panelist_id, campaign_id);
CREATE STATISTICS stats_impression_ts ON staging.exposure_mapping(impression_ts);
