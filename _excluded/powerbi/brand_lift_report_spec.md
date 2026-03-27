# Power BI Brand Lift Report Specification

## Data Model

### Tables and Relationships

#### 1. **lift_results** (Fact Table)
**Source**: `analytics.lift_results` from Synapse/Fabric

**Columns**:
- lift_result_id (Key)
- campaign_id ? Links to dim_campaigns
- metric_name
- exposed_value
- control_value
- lift_absolute
- lift_relative_pct
- p_value
- is_significant
- run_ts
- cohort_size_exposed
- cohort_size_control

#### 2. **dim_campaigns** (Dimension)
**Source**: Campaign metadata table

**Columns**:
- campaign_id (Key)
- campaign_name
- start_date
- end_date
- client_id
- industry
- objective

#### 3. **dim_time** (Dimension)
**Source**: Date dimension

**Columns**:
- date (Key)
- year
- quarter
- month
- week
- day_of_week

#### 4. **exposure_mapping** (Supporting Fact)
**Source**: `staging.exposure_mapping`

**Columns**:
- impression_id
- panelist_id
- campaign_id
- impression_ts
- device_type
- country_code

---

## Relationships

```
dim_campaigns[campaign_id] ??(1)??(*) lift_results[campaign_id]
dim_time[date] ??(1)??(*) lift_results[run_ts]
dim_campaigns[campaign_id] ??(1)??(*) exposure_mapping[campaign_id]
```

---

## DAX Measures

### Core Metrics

```dax
// Total Impressions
Total Impressions = COUNTROWS(exposure_mapping)

// Unique Panelists
Unique Panelists = DISTINCTCOUNT(exposure_mapping[panelist_id])

// Average Lift
Average Lift % = AVERAGE(lift_results[lift_relative_pct])

// Significant Lift Count
Significant Lifts = CALCULATE(
    COUNTROWS(lift_results),
    lift_results[is_significant] = TRUE()
)

// Lift Significance Rate
Lift Significance Rate = 
DIVIDE(
    [Significant Lifts],
    COUNTROWS(lift_results),
    0
)

// Weighted Average Lift
Weighted Avg Lift = 
DIVIDE(
    SUMX(
        lift_results,
        lift_results[lift_relative_pct] * lift_results[cohort_size_exposed]
    ),
    SUM(lift_results[cohort_size_exposed]),
    0
)

// Confidence Interval Width
CI Width = 
AVERAGEX(
    lift_results,
    lift_results[confidence_interval_upper] - lift_results[confidence_interval_lower]
)
```

### Advanced Metrics

```dax
// Brand Favorability Lift
Brand Favorability Lift = 
CALCULATE(
    [Average Lift %],
    lift_results[metric_name] = "brand_favorability"
)

// Awareness Lift
Awareness Lift = 
CALCULATE(
    [Average Lift %],
    lift_results[metric_name] = "brand_awareness"
)

// Purchase Intent Lift
Purchase Intent Lift = 
CALCULATE(
    [Average Lift %],
    lift_results[metric_name] = "purchase_intent"
)
```

---

## Page 1: Executive Dashboard

### Visualizations

#### 1. **KPI Cards** (Top Row)
- **Total Campaigns**: `DISTINCTCOUNT(lift_results[campaign_id])`
- **Total Impressions**: `[Total Impressions]`
- **Avg Lift**: `[Average Lift %]`
- **Significance Rate**: `[Lift Significance Rate]`

#### 2. **Lift by Metric** (Column Chart)
- **X-Axis**: `lift_results[metric_name]`
- **Y-Axis**: `[Average Lift %]`
- **Data Labels**: Lift percentage + significance indicator
- **Conditional Formatting**: Green if significant, Gray if not

#### 3. **Time Series** (Line Chart)
- **X-Axis**: `dim_time[date]`
- **Y-Axis**: `[Total Impressions]`
- **Legend**: `dim_campaigns[campaign_name]`
- **Drill-down**: Year ? Quarter ? Month ? Day

#### 4. **Exposed vs Control** (Clustered Bar Chart)
- **X-Axis**: `lift_results[metric_name]`
- **Values**: 
  - `AVERAGE(lift_results[exposed_value])` 
  - `AVERAGE(lift_results[control_value])`
- **Data Labels**: Mean values

#### 5. **Campaign Performance Table**
- **Columns**:
  - Campaign Name
  - Metric
  - Exposed Mean
  - Control Mean
  - Lift %
  - p-value
  - Significant (?/?)
- **Sort by**: Lift % descending
- **Conditional Formatting**: Highlight significant rows

---

## Page 2: Statistical Details

### Visualizations

#### 1. **Lift Distribution** (Histogram)
- **X-Axis**: `lift_results[lift_relative_pct]`
- **Y-Axis**: Count
- **Bins**: 10
- **Reference Line**: 0% lift

#### 2. **P-Value Distribution** (Scatter Plot)
- **X-Axis**: `lift_results[cohort_size_exposed]`
- **Y-Axis**: `lift_results[p_value]`
- **Size**: Lift magnitude
- **Color**: Is significant (green/red)
- **Reference Line**: p = 0.05

#### 3. **Confidence Intervals** (Error Bars Chart)
- **X-Axis**: `lift_results[metric_name]`
- **Y-Axis**: `lift_results[lift_absolute]`
- **Error Bars**: 
  - Lower: `lift_results[confidence_interval_lower]`
  - Upper: `lift_results[confidence_interval_upper]`

#### 4. **Sample Size Analysis**
- **Exposed Cohort Size**: `SUM(lift_results[cohort_size_exposed])`
- **Control Cohort Size**: `SUM(lift_results[cohort_size_control])`
- **Match Rate**: Ratio visualization

---

## Page 3: Segmentation Analysis

### Slicers

- **Campaign** (dim_campaigns[campaign_name])
- **Metric** (lift_results[metric_name])
- **Date Range** (dim_time[date])
- **Device Type** (exposure_mapping[device_type])
- **Country** (exposure_mapping[country_code])

### Visualizations

#### 1. **Lift by Device Type** (Bar Chart)
- Requires device type in lift results (segmented analysis)

#### 2. **Lift by Geography** (Map Visual)
- **Location**: Country/Region
- **Value**: Average Lift %
- **Color**: Lift magnitude

#### 3. **Demographic Breakdown** (Matrix)
- **Rows**: Age Range, Gender, Income Bucket
- **Values**: Lift %, Sample Size, Significance

---

## Filters and Interactivity

### Report-Level Filters
- **Date Range**: Last 90 days by default
- **Client**: Filter by `dim_campaigns[client_id]`

### Page-Level Filters
- Campaign status (Active/Completed)
- Minimum sample size threshold

### Cross-Filtering Behavior
- Campaign selection ? Filters all visuals
- Metric selection ? Filters related visualizations
- Time selection ? Filters impressions and lift results

---

## Row-Level Security (RLS)

### Security Roles

#### 1. **Client Role**
```dax
[client_id] = USERNAME()
```

#### 2. **Agency Role**
```dax
[agency_id] = LOOKUPVALUE(
    dim_users[agency_id],
    dim_users[email],
    USERNAME()
)
```

#### 3. **Internal Admin**
```dax
1 = 1  // Full access
```

### Implementation
1. Create roles in Power BI Desktop
2. Add DAX filter expressions
3. Assign users to roles in Power BI Service
4. Test with "View as" feature

---

## Refresh Schedule

### Power BI Service Configuration

**Dataset Refresh**:
- **Frequency**: Daily at 6 AM UTC
- **Source**: Synapse/Fabric Direct Query or Import
- **Incremental Refresh**: 
  - Rolling 90 days
  - Historical archive: 2 years

**Gateway**: 
- On-premises data gateway (if using Synapse)
- Or native Fabric connector

---

## Performance Optimizations

### 1. **Aggregations**
Pre-aggregate lift results by campaign + metric:
```sql
CREATE VIEW analytics.lift_results_agg AS
SELECT 
    campaign_id,
    metric_name,
    AVG(lift_relative_pct) as avg_lift,
    COUNT(*) as measurement_count,
    SUM(CASE WHEN is_significant = 1 THEN 1 ELSE 0 END) as significant_count
FROM analytics.lift_results
GROUP BY campaign_id, metric_name;
```

### 2. **Composite Model**
- Import dimension tables (campaigns, time)
- DirectQuery fact tables (lift results, exposures)

### 3. **Query Folding**
Ensure DAX measures translate to SQL pushdown

---

## Mobile Layout

- Simplified KPIs on phone layout
- Single-page summary view
- Touch-optimized slicer panels

---

## Export and Sharing

### Report Formats
- **Interactive**: Published to Power BI Service
- **Static**: PDF export for client deliverables
- **Data**: Excel export for ad-hoc analysis

### Subscriptions
- Email subscriptions with snapshot
- Daily/Weekly/Monthly cadence
- Conditional alerts for significant findings

---

## Testing Checklist

- [ ] Data model relationships validated
- [ ] DAX measures return correct values
- [ ] RLS tested for each role
- [ ] Performance < 3 seconds for page load
- [ ] Mobile layout renders correctly
- [ ] Refresh schedule working
- [ ] Export functionality tested

---

## Deployment

1. **Development**: Power BI Desktop (.pbix file)
2. **Test**: Power BI Service (Test workspace)
3. **Production**: Power BI Service (Production workspace)
4. **Distribution**: App distribution to clients
