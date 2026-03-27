# Databricks notebook source
# MAGIC %md
# MAGIC # Brand Lift Calculation Job
# MAGIC 
# MAGIC Computes lift metrics for matched cohorts using survey/behavioral data
# MAGIC - Joins matched cohorts with survey metrics
# MAGIC - Computes exposed vs control means
# MAGIC - Calculates lift and statistical significance
# MAGIC - Writes results to Microsoft Fabric/Synapse

# COMMAND ----------

from pyspark.sql import SparkSession
from pyspark.sql.functions import *
from pyspark.sql.types import *
from scipy import stats
import numpy as np
from datetime import datetime
import json

# Configuration
SYNAPSE_JDBC_URL = dbutils.secrets.get(scope="adimpactos-kv", key="synapse-jdbc-url")
SYNAPSE_USER = dbutils.secrets.get(scope="adimpactos-kv", key="synapse-user")
SYNAPSE_PASSWORD = dbutils.secrets.get(scope="adimpactos-kv", key="synapse-password")

CAMPAIGN_ID = dbutils.widgets.get("campaign_id") if dbutils.widgets.get("campaign_id") else "camp-001"
BATCH_ID = dbutils.widgets.get("batch_id")  # From PSM job

print(f"Processing lift for campaign: {CAMPAIGN_ID}, batch: {BATCH_ID}")

# COMMAND ----------

# MAGIC %md
# MAGIC ## Step 1: Load Matched Cohorts

# COMMAND ----------

def load_matched_cohorts(campaign_id, batch_id):
    """Load matched cohorts from PSM output"""
    query = f"""
    SELECT 
        panelist_id,
        campaign_id,
        cohort_type,
        match_id,
        propensity_score,
        balance_score
    FROM staging.matched_cohorts
    WHERE campaign_id = '{campaign_id}'
    AND processing_batch_id = '{batch_id}'
    """
    
    df = (spark.read
          .format("jdbc")
          .option("url", SYNAPSE_JDBC_URL)
          .option("query", query)
          .option("user", SYNAPSE_USER)
          .option("password", SYNAPSE_PASSWORD)
          .option("driver", "com.microsoft.sqlserver.jdbc.SQLServerDriver")
          .load())
    
    return df

cohorts_df = load_matched_cohorts(CAMPAIGN_ID, BATCH_ID)

print(f"Loaded cohorts:")
print(f"- Exposed: {cohorts_df.filter(col('cohort_type') == 'exposed').count()}")
print(f"- Control: {cohorts_df.filter(col('cohort_type') == 'control').count()}")

# COMMAND ----------

# MAGIC %md
# MAGIC ## Step 2: Load Survey/Behavioral Metrics

# COMMAND ----------

def load_survey_metrics(campaign_id):
    """
    Load survey responses or behavioral metrics
    Mock implementation - replace with actual survey data source
    """
    query = f"""
    SELECT 
        sm.panelist_id,
        sm.campaign_id,
        sm.metric_name,
        sm.metric_value,
        sm.survey_date,
        sm.demographic_segment
    FROM analytics.survey_metrics sm
    WHERE sm.campaign_id = '{campaign_id}'
    """
    
    df = (spark.read
          .format("jdbc")
          .option("url", SYNAPSE_JDBC_URL)
          .option("query", query)
          .option("user", SYNAPSE_USER)
          .option("password", SYNAPSE_PASSWORD)
          .option("driver", "com.microsoft.sqlserver.jdbc.SQLServerDriver")
          .load())
    
    return df

# For demo purposes, generate mock survey data
def generate_mock_survey_data(cohorts_df):
    """Generate mock survey responses for testing"""
    import random
    
    metrics = ['brand_favorability', 'brand_awareness', 'purchase_intent', 'message_recall']
    
    # Apply treatment effect: exposed cohort has slightly higher values
    def generate_value(cohort_type, metric):
        base_value = random.uniform(50, 70)
        if cohort_type == 'exposed':
            treatment_effect = random.uniform(5, 15)  # 5-15 point lift
            return min(100, base_value + treatment_effect)
        return base_value
    
    survey_data = []
    for row in cohorts_df.collect():
        for metric in metrics:
            survey_data.append({
                'panelist_id': row.panelist_id,
                'campaign_id': row.campaign_id,
                'metric_name': metric,
                'metric_value': generate_value(row.cohort_type, metric),
                'cohort_type': row.cohort_type,
                'survey_date': datetime.utcnow()
            })
    
    return spark.createDataFrame(survey_data)

# Use mock data for demo (replace with actual survey load in production)
survey_df = generate_mock_survey_data(cohorts_df)
print(f"Loaded {survey_df.count()} survey responses")

# COMMAND ----------

# MAGIC %md
# MAGIC ## Step 3: Join Cohorts with Metrics

# COMMAND ----------

# Join cohorts with survey data
joined_df = cohorts_df.join(
    survey_df,
    on=['panelist_id', 'campaign_id'],
    how='inner'
)

print(f"Joined records: {joined_df.count()}")
display(joined_df.select('panelist_id', 'cohort_type', 'metric_name', 'metric_value').limit(10))

# COMMAND ----------

# MAGIC %md
# MAGIC ## Step 4: Compute Lift Metrics

# COMMAND ----------

def compute_lift_statistics(df, metric_name):
    """
    Compute lift statistics for a specific metric
    - Mean, std dev for exposed and control
    - Lift (absolute and relative)
    - T-test for significance
    """
    
    metric_df = df.filter(col('metric_name') == metric_name)
    
    # Exposed cohort statistics
    exposed_stats = metric_df.filter(col('cohort_type') == 'exposed').agg(
        mean('metric_value').alias('exposed_mean'),
        stddev('metric_value').alias('exposed_std'),
        count('*').alias('exposed_n')
    ).collect()[0]
    
    # Control cohort statistics
    control_stats = metric_df.filter(col('cohort_type') == 'control').agg(
        mean('metric_value').alias('control_mean'),
        stddev('metric_value').alias('control_std'),
        count('*').alias('control_n')
    ).collect()[0]
    
    # Lift calculations
    lift_absolute = exposed_stats['exposed_mean'] - control_stats['control_mean']
    lift_relative_pct = (lift_absolute / control_stats['control_mean']) * 100 if control_stats['control_mean'] > 0 else 0
    
    # Perform two-sample t-test
    exposed_values = [row.metric_value for row in metric_df.filter(col('cohort_type') == 'exposed').select('metric_value').collect()]
    control_values = [row.metric_value for row in metric_df.filter(col('cohort_type') == 'control').select('metric_value').collect()]
    
    t_statistic, p_value = stats.ttest_ind(exposed_values, control_values)
    
    # Confidence interval (95%)
    se_diff = np.sqrt((exposed_stats['exposed_std']**2 / exposed_stats['exposed_n']) + 
                      (control_stats['control_std']**2 / control_stats['control_n']))
    ci_margin = 1.96 * se_diff
    
    result = {
        'campaign_id': CAMPAIGN_ID,
        'metric_name': metric_name,
        'exposed_value': float(exposed_stats['exposed_mean']),
        'exposed_sample_size': int(exposed_stats['exposed_n']),
        'exposed_std_dev': float(exposed_stats['exposed_std']),
        'control_value': float(control_stats['control_mean']),
        'control_sample_size': int(control_stats['control_n']),
        'control_std_dev': float(control_stats['control_std']),
        'lift_absolute': float(lift_absolute),
        'lift_relative_pct': float(lift_relative_pct),
        'p_value': float(p_value),
        'confidence_interval_lower': float(lift_absolute - ci_margin),
        'confidence_interval_upper': float(lift_absolute + ci_margin),
        'is_significant': bool(p_value < 0.05),
        'statistical_test': 't-test',
        't_statistic': float(t_statistic)
    }
    
    return result

# Compute lift for each metric
metrics = survey_df.select('metric_name').distinct().rdd.flatMap(lambda x: x).collect()
lift_results = []

for metric in metrics:
    result = compute_lift_statistics(joined_df, metric)
    lift_results.append(result)
    print(f"\n{metric}:")
    print(f"  Exposed: {result['exposed_value']:.2f} (n={result['exposed_sample_size']})")
    print(f"  Control: {result['control_value']:.2f} (n={result['control_sample_size']})")
    print(f"  Lift: {result['lift_relative_pct']:.2f}% (p={result['p_value']:.4f})")
    print(f"  Significant: {'?' if result['is_significant'] else '?'}")

# COMMAND ----------

# MAGIC %md
# MAGIC ## Step 5: Enrich with Metadata

# COMMAND ----------

run_ts = datetime.utcnow()
processing_job_id = f"lift_{CAMPAIGN_ID}_{run_ts.strftime('%Y%m%d_%H%M%S')}"

# Add metadata to results
for result in lift_results:
    result.update({
        'cohort_definition': json.dumps({
            'batch_id': BATCH_ID,
            'matching_method': 'propensity_score_matching',
            'caliper': 0.1
        }),
        'cohort_size_exposed': result['exposed_sample_size'],
        'cohort_size_control': result['control_sample_size'],
        'analysis_start_date': (run_ts - pd.Timedelta(days=30)).date(),
        'analysis_end_date': run_ts.date(),
        'measurement_window_days': 30,
        'balance_score': cohorts_df.select('balance_score').first()[0],
        'run_ts': run_ts,
        'processing_job_id': processing_job_id,
        'model_version': 'v1.0',
        'created_by': 'databricks_lift_job'
    })

# Convert to DataFrame
lift_results_df = spark.createDataFrame(lift_results)

display(lift_results_df.select(
    'metric_name', 'exposed_value', 'control_value', 
    'lift_relative_pct', 'p_value', 'is_significant'
))

# COMMAND ----------

# MAGIC %md
# MAGIC ## Step 6: Write Results to Fabric/Synapse

# COMMAND ----------

def write_lift_results(results_df):
    """Write lift calculation results to analytics table"""
    
    (results_df.write
     .format("jdbc")
     .option("url", SYNAPSE_JDBC_URL)
     .option("dbtable", "analytics.lift_results")
     .option("user", SYNAPSE_USER)
     .option("password", SYNAPSE_PASSWORD)
     .option("driver", "com.microsoft.sqlserver.jdbc.SQLServerDriver")
     .mode("append")
     .save())
    
    print(f"? Written {results_df.count()} lift results to Synapse")
    print(f"Processing Job ID: {processing_job_id}")

write_lift_results(lift_results_df)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Step 7: Summary Report

# COMMAND ----------

print("\n=== Brand Lift Analysis Summary ===")
print(f"Campaign: {CAMPAIGN_ID}")
print(f"Batch: {BATCH_ID}")
print(f"Run Timestamp: {run_ts.isoformat()}")
print(f"\nMetrics Analyzed: {len(lift_results)}")

significant_count = sum(1 for r in lift_results if r['is_significant'])
print(f"Significant Results: {significant_count}/{len(lift_results)}")

print("\n=== Detailed Results ===")
for result in sorted(lift_results, key=lambda x: x['lift_relative_pct'], reverse=True):
    sig_marker = "***" if result['is_significant'] else ""
    print(f"\n{result['metric_name']} {sig_marker}")
    print(f"  Lift: {result['lift_relative_pct']:.2f}%")
    print(f"  p-value: {result['p_value']:.4f}")
    print(f"  95% CI: [{result['confidence_interval_lower']:.2f}, {result['confidence_interval_upper']:.2f}]")

print("\n? Lift calculation completed successfully!")
