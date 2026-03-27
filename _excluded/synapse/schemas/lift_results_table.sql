-- Analytics Lift Results Table Schema for Microsoft Fabric / Synapse
-- Stores computed brand lift metrics and statistical test results

CREATE TABLE analytics.lift_results
(
    -- Primary Key
    lift_result_id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    
    -- Campaign Context
    campaign_id NVARCHAR(100) NOT NULL,
    creative_id NVARCHAR(100) NULL,
    
    -- Metric Being Measured
    metric_name NVARCHAR(100) NOT NULL, -- 'brand_favorability', 'awareness', 'purchase_intent', etc.
    metric_category NVARCHAR(50) NULL, -- 'brand', 'behavioral', 'sentiment'
    
    -- Exposed Cohort Results
    exposed_value FLOAT NOT NULL,
    exposed_sample_size INT NOT NULL,
    exposed_std_dev FLOAT NULL,
    
    -- Control Cohort Results
    control_value FLOAT NOT NULL,
    control_sample_size INT NOT NULL,
    control_std_dev FLOAT NULL,
    
    -- Lift Calculation
    lift_absolute FLOAT NOT NULL, -- exposed - control
    lift_relative_pct FLOAT NOT NULL, -- (exposed - control) / control * 100
    
    -- Statistical Significance
    p_value FLOAT NULL,
    confidence_interval_lower FLOAT NULL,
    confidence_interval_upper FLOAT NULL,
    is_significant BIT NOT NULL DEFAULT 0, -- p < 0.05
    statistical_test NVARCHAR(50) NULL, -- 't-test', 'chi-square', 'mann-whitney'
    
    -- Cohort Definition
    cohort_definition NVARCHAR(MAX) NULL, -- JSON with filters
    cohort_size_exposed INT NOT NULL,
    cohort_size_control INT NOT NULL,
    
    -- Segmentation
    segment_age_range NVARCHAR(20) NULL,
    segment_gender NVARCHAR(10) NULL,
    segment_country NVARCHAR(10) NULL,
    segment_device_type NVARCHAR(50) NULL,
    
    -- Time Period
    analysis_start_date DATE NOT NULL,
    analysis_end_date DATE NOT NULL,
    measurement_window_days INT NOT NULL,
    
    -- Quality Metrics
    balance_score FLOAT NULL, -- PSM balance quality (0-1)
    common_support_pct FLOAT NULL, -- % of samples in common support region
    
    -- Processing Metadata
    run_ts DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    processing_job_id NVARCHAR(100) NULL,
    model_version NVARCHAR(50) NULL,
    
    -- Audit
    created_by NVARCHAR(100) NULL,
    notes NVARCHAR(MAX) NULL,
    
    -- Constraints
    CONSTRAINT PK_lift_results PRIMARY KEY NONCLUSTERED (lift_result_id)
)
WITH
(
    DISTRIBUTION = HASH(campaign_id),
    CLUSTERED COLUMNSTORE INDEX
);

-- Indexes
CREATE INDEX IX_lift_results_campaign ON analytics.lift_results(campaign_id, run_ts DESC);
CREATE INDEX IX_lift_results_metric ON analytics.lift_results(metric_name, run_ts DESC);
CREATE INDEX IX_lift_results_significant ON analytics.lift_results(is_significant) WHERE is_significant = 1;

-- Statistics
CREATE STATISTICS stats_campaign_metric ON analytics.lift_results(campaign_id, metric_name);
