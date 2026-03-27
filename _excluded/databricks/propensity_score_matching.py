# Databricks notebook source
# MAGIC %md
# MAGIC # Propensity Score Matching (PSM) for Brand Lift Analysis
# MAGIC 
# MAGIC This notebook demonstrates propensity score matching using PySpark on Azure Synapse/Databricks:
# MAGIC 1. Load panelist features and exposure labels
# MAGIC 2. Train logistic regression propensity model
# MAGIC 3. Perform nearest neighbor matching
# MAGIC 4. Compute balance diagnostics
# MAGIC 5. Output matched cohorts back to Synapse

# COMMAND ----------

# MAGIC %md
# MAGIC ## Setup and Configuration

# COMMAND ----------

from pyspark.sql import SparkSession
from pyspark.sql.functions import *
from pyspark.sql.types import *
from pyspark.ml.feature import VectorAssembler, StandardScaler
from pyspark.ml.classification import LogisticRegression
from pyspark.ml import Pipeline
import numpy as np
from datetime import datetime

# Configuration
SYNAPSE_JDBC_URL = dbutils.secrets.get(scope="adimpactos-kv", key="synapse-jdbc-url")
SYNAPSE_USER = dbutils.secrets.get(scope="adimpactos-kv", key="synapse-user")
SYNAPSE_PASSWORD = dbutils.secrets.get(scope="adimpactos-kv", key="synapse-password")

# Campaign configuration
CAMPAIGN_ID = dbutils.widgets.get("campaign_id") if dbutils.widgets.get("campaign_id") else "camp-001"
CALIPER_WIDTH = 0.1  # Maximum propensity score difference for matching
MATCHING_RATIO = 1  # 1:1 matching (exposed:control)

print(f"Processing campaign: {CAMPAIGN_ID}")

# COMMAND ----------

# MAGIC %md
# MAGIC ## Step 1: Load Data from Synapse

# COMMAND ----------

def load_exposure_data(campaign_id):
    """Load exposure mapping data from Synapse"""
    query = f"""
    SELECT 
        em.panelist_id,
        em.impression_id,
        em.campaign_id,
        em.creative_id,
        em.impression_ts,
        em.device_type,
        em.country_code,
        em.is_bot,
        CASE WHEN em.impression_id IS NOT NULL THEN 1 ELSE 0 END as exposed
    FROM staging.exposure_mapping em
    WHERE em.campaign_id = '{campaign_id}'
    AND em.is_bot = 0
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

def load_panelist_features(campaign_id):
    """Load panelist demographic features from Cosmos/Synapse"""
    query = f"""
    SELECT DISTINCT
        p.panelist_id,
        p.age,
        p.gender,
        p.country,
        p.hhIncomeBucket,
        p.deviceType,
        p.browser
    FROM panelists p
    INNER JOIN staging.exposure_mapping em ON p.panelist_id = em.panelist_id
    WHERE em.campaign_id = '{campaign_id}'
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

# Load data
exposure_df = load_exposure_data(CAMPAIGN_ID)
panelist_features_df = load_panelist_features(CAMPAIGN_ID)

# Join exposure with features
data_df = exposure_df.join(panelist_features_df, on="panelist_id", how="left")

print(f"Total records: {data_df.count()}")
print(f"Exposed: {data_df.filter(col('exposed') == 1).count()}")
print(f"Control: {data_df.filter(col('exposed') == 0).count()}")

# COMMAND ----------

# MAGIC %md
# MAGIC ## Step 2: Feature Engineering

# COMMAND ----------

def prepare_features(df):
    """Prepare features for propensity model"""
    
    # One-hot encode categorical variables
    df = df.fillna({
        'age': 0,
        'gender': 'Unknown',
        'country': 'XX',
        'hhIncomeBucket': 'Unknown',
        'deviceType': 'Unknown',
        'browser': 'Other'
    })
    
    # Create dummy variables
    df = df.withColumn('age_normalized', col('age') / 100.0)
    df = df.withColumn('gender_m', when(col('gender') == 'M', 1).otherwise(0))
    df = df.withColumn('gender_f', when(col('gender') == 'F', 1).otherwise(0))
    df = df.withColumn('device_mobile', when(col('deviceType') == 'Mobile', 1).otherwise(0))
    df = df.withColumn('device_tablet', when(col('deviceType') == 'Tablet', 1).otherwise(0))
    df = df.withColumn('country_us', when(col('country') == 'US', 1).otherwise(0))
    df = df.withColumn('country_uk', when(col('country') == 'UK', 1).otherwise(0))
    
    # Income buckets
    df = df.withColumn('income_low', when(col('hhIncomeBucket').isin(['<25K', '25K-50K']), 1).otherwise(0))
    df = df.withColumn('income_high', when(col('hhIncomeBucket').isin(['100K-150K', '150K+']), 1).otherwise(0))
    
    return df

data_prepared_df = prepare_features(data_df)

# Define feature columns
feature_cols = [
    'age_normalized', 'gender_m', 'gender_f', 
    'device_mobile', 'device_tablet',
    'country_us', 'country_uk',
    'income_low', 'income_high'
]

# Assemble features
assembler = VectorAssembler(inputCols=feature_cols, outputCol="features_raw")
scaler = StandardScaler(inputCol="features_raw", outputCol="features", withStd=True, withMean=True)

data_prepared_df = assembler.transform(data_prepared_df)
scaler_model = scaler.fit(data_prepared_df)
data_scaled_df = scaler_model.transform(data_prepared_df)

display(data_scaled_df.select('panelist_id', 'exposed', 'features').limit(10))

# COMMAND ----------

# MAGIC %md
# MAGIC ## Step 3: Train Propensity Score Model

# COMMAND ----------

# Train logistic regression model
lr = LogisticRegression(
    featuresCol="features",
    labelCol="exposed",
    predictionCol="prediction",
    probabilityCol="probability",
    maxIter=100,
    regParam=0.01,
    elasticNetParam=0.5
)

# Fit model
propensity_model = lr.fit(data_scaled_df)

# Generate propensity scores
scored_df = propensity_model.transform(data_scaled_df)

# Extract propensity score (probability of being exposed)
scored_df = scored_df.withColumn(
    "propensity_score",
    col("probability").getItem(1)
)

print("Propensity Model Summary:")
print(f"Coefficients: {propensity_model.coefficients}")
print(f"Intercept: {propensity_model.intercept}")

# Check propensity score distribution
display(scored_df.select('panelist_id', 'exposed', 'propensity_score').describe())

# COMMAND ----------

# MAGIC %md
# MAGIC ## Step 4: Perform Nearest Neighbor Matching

# COMMAND ----------

def perform_nn_matching(df, caliper=0.1, ratio=1):
    """
    Perform nearest neighbor matching with caliper
    
    Args:
        df: DataFrame with propensity scores
        caliper: Maximum propensity score difference
        ratio: Number of controls per exposed unit
    
    Returns:
        DataFrame with matched pairs
    """
    
    # Separate exposed and control
    exposed_df = df.filter(col('exposed') == 1).select(
        col('panelist_id').alias('exposed_id'),
        col('propensity_score').alias('exposed_ps'),
        col('impression_id').alias('exposed_impression_id')
    )
    
    control_df = df.filter(col('exposed') == 0).select(
        col('panelist_id').alias('control_id'),
        col('propensity_score').alias('control_ps')
    )
    
    # Cross join to find potential matches
    cross_df = exposed_df.crossJoin(control_df)
    
    # Calculate absolute difference in propensity scores
    cross_df = cross_df.withColumn(
        'ps_diff',
        abs(col('exposed_ps') - col('control_ps'))
    )
    
    # Apply caliper
    within_caliper_df = cross_df.filter(col('ps_diff') <= caliper)
    
    # Find nearest neighbor for each exposed unit
    from pyspark.sql.window import Window
    
    window_spec = Window.partitionBy('exposed_id').orderBy(col('ps_diff').asc())
    
    matched_df = within_caliper_df.withColumn(
        'match_rank',
        row_number().over(window_spec)
    ).filter(col('match_rank') <= ratio)
    
    # Generate match ID
    matched_df = matched_df.withColumn(
        'match_id',
        monotonically_increasing_id()
    )
    
    return matched_df

matched_pairs_df = perform_nn_matching(scored_df, caliper=CALIPER_WIDTH, ratio=MATCHING_RATIO)

print(f"Total matched pairs: {matched_pairs_df.count()}")
print(f"Match rate: {matched_pairs_df.count() / scored_df.filter(col('exposed') == 1).count() * 100:.2f}%")

display(matched_pairs_df.select('exposed_id', 'control_id', 'ps_diff', 'match_id').limit(10))

# COMMAND ----------

# MAGIC %md
# MAGIC ## Step 5: Compute Balance Diagnostics

# COMMAND ----------

def compute_balance_diagnostics(original_df, matched_df, feature_cols):
    """
    Compute standardized mean differences before and after matching
    
    SMD = (mean_exposed - mean_control) / sqrt((var_exposed + var_control) / 2)
    SMD < 0.1 indicates good balance
    """
    
    balance_results = []
    
    for feature in feature_cols:
        # Before matching
        exposed_stats = original_df.filter(col('exposed') == 1).select(
            mean(col(feature)).alias('mean_exposed_before'),
            variance(col(feature)).alias('var_exposed_before')
        ).collect()[0]
        
        control_stats = original_df.filter(col('exposed') == 0).select(
            mean(col(feature)).alias('mean_control_before'),
            variance(col(feature)).alias('var_control_before')
        ).collect()[0]
        
        smd_before = (exposed_stats['mean_exposed_before'] - control_stats['mean_control_before']) / \
                     np.sqrt((exposed_stats['var_exposed_before'] + control_stats['var_control_before']) / 2)
        
        # After matching - need to reconstruct matched sample
        exposed_matched = matched_df.select(col('exposed_id').alias('panelist_id')).distinct()
        control_matched = matched_df.select(col('control_id').alias('panelist_id')).distinct()
        
        exposed_matched_df = original_df.join(exposed_matched, on='panelist_id', how='inner')
        control_matched_df = original_df.join(control_matched, on='panelist_id', how='inner')
        
        exposed_stats_after = exposed_matched_df.select(
            mean(col(feature)).alias('mean_exposed_after'),
            variance(col(feature)).alias('var_exposed_after')
        ).collect()[0]
        
        control_stats_after = control_matched_df.select(
            mean(col(feature)).alias('mean_control_after'),
            variance(col(feature)).alias('var_control_after')
        ).collect()[0]
        
        smd_after = (exposed_stats_after['mean_exposed_after'] - control_stats_after['mean_control_after']) / \
                    np.sqrt((exposed_stats_after['var_exposed_after'] + control_stats_after['var_control_after']) / 2)
        
        balance_results.append({
            'feature': feature,
            'smd_before': float(smd_before),
            'smd_after': float(smd_after),
            'improvement': abs(smd_before) - abs(smd_after),
            'balanced': abs(smd_after) < 0.1
        })
    
    return spark.createDataFrame(balance_results)

balance_df = compute_balance_diagnostics(data_prepared_df, matched_pairs_df, feature_cols)

print("Balance Diagnostics:")
display(balance_df.orderBy(abs(col('smd_after')).desc()))

# Check overall balance
balanced_features = balance_df.filter(col('balanced') == True).count()
total_features = balance_df.count()
balance_score = balanced_features / total_features

print(f"\nOverall Balance Score: {balance_score:.2%} ({balanced_features}/{total_features} features balanced)")

# COMMAND ----------

# MAGIC %md
# MAGIC ## Step 6: Output Matched Cohorts to Synapse

# COMMAND ----------

def write_matched_cohorts(matched_df, balance_score):
    """Write matched cohorts back to Synapse with metadata"""
    
    run_ts = datetime.utcnow()
    batch_id = f"psm_{CAMPAIGN_ID}_{run_ts.strftime('%Y%m%d_%H%M%S')}"
    
    # Prepare exposed cohort
    exposed_cohort = matched_df.select(
        col('exposed_id').alias('panelist_id'),
        lit(CAMPAIGN_ID).alias('campaign_id'),
        col('exposed_impression_id').alias('impression_id'),
        col('match_id'),
        col('exposed_ps').alias('propensity_score'),
        col('ps_diff').alias('match_quality_score'),
        lit('exposed').alias('cohort_type'),
        lit(True).alias('matched_control_flag'),
        lit(balance_score).alias('balance_score'),
        lit(run_ts).alias('processing_ts'),
        lit(batch_id).alias('processing_batch_id')
    )
    
    # Prepare control cohort
    control_cohort = matched_df.select(
        col('control_id').alias('panelist_id'),
        lit(CAMPAIGN_ID).alias('campaign_id'),
        lit(None).cast('string').alias('impression_id'),
        col('match_id'),
        col('control_ps').alias('propensity_score'),
        col('ps_diff').alias('match_quality_score'),
        lit('control').alias('cohort_type'),
        lit(True).alias('matched_control_flag'),
        lit(balance_score).alias('balance_score'),
        lit(run_ts).alias('processing_ts'),
        lit(batch_id).alias('processing_batch_id')
    )
    
    # Union both cohorts
    final_cohorts = exposed_cohort.union(control_cohort)
    
    # Write to Synapse
    (final_cohorts.write
     .format("jdbc")
     .option("url", SYNAPSE_JDBC_URL)
     .option("dbtable", "staging.matched_cohorts")
     .option("user", SYNAPSE_USER)
     .option("password", SYNAPSE_PASSWORD)
     .option("driver", "com.microsoft.sqlserver.jdbc.SQLServerDriver")
     .mode("append")
     .save())
    
    print(f"? Written {final_cohorts.count()} matched records to Synapse")
    print(f"Batch ID: {batch_id}")
    print(f"Balance Score: {balance_score:.2%}")
    
    return batch_id

batch_id = write_matched_cohorts(matched_pairs_df, balance_score)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Step 7: Summary and Validation

# COMMAND ----------

summary = {
    'campaign_id': CAMPAIGN_ID,
    'batch_id': batch_id,
    'total_exposed': scored_df.filter(col('exposed') == 1).count(),
    'total_control': scored_df.filter(col('exposed') == 0).count(),
    'matched_pairs': matched_pairs_df.count(),
    'match_rate': matched_pairs_df.count() / scored_df.filter(col('exposed') == 1).count(),
    'balance_score': balance_score,
    'caliper_width': CALIPER_WIDTH,
    'run_timestamp': datetime.utcnow().isoformat()
}

print("\n=== PSM Summary ===")
for key, value in summary.items():
    print(f"{key}: {value}")

# Write summary to metadata table
summary_df = spark.createDataFrame([summary])
(summary_df.write
 .format("jdbc")
 .option("url", SYNAPSE_JDBC_URL)
 .option("dbtable", "analytics.psm_run_metadata")
 .option("user", SYNAPSE_USER)
 .option("password", SYNAPSE_PASSWORD)
 .option("driver", "com.microsoft.sqlserver.jdbc.SQLServerDriver")
 .mode("append")
 .save())

print("\n? PSM process completed successfully!")
