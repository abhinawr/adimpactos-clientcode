"""
EventHub Streaming Processor
Reads ad impression events from Azure Event Hubs, applies bot detection,
enriches with geo data, writes to ADLS staging as Parquet, and upserts to Synapse staging table.
"""

from pyspark.sql import SparkSession
from pyspark.sql.functions import (
    col, from_json, to_timestamp, year, month, dayofmonth,
    regexp_extract, when, lower, lit, current_timestamp, sha2, concat_ws
)
from pyspark.sql.types import (
    StructType, StructField, StringType, BooleanType, TimestampType, MapType
)
from delta.Tables import DeltaTable
import json

class EventHubStreamingProcessor:
    
    def __init__(self, spark):
        self.spark = spark
        self.bot_user_agents = self._load_bot_patterns()
        self.rate_threshold_per_minute = 100
        
    def _load_bot_patterns(self):
        """Load bot detection patterns"""
        return [
            "bot", "crawler", "spider", "scraper", "curl", "wget", "python-requests",
            "java/", "go-http-client", "okhttp", "httpclient", "headless",
            "phantom", "selenium", "webdriver", "slurp", "bingbot", "googlebot",
            "baiduspider", "yandex", "duckduckbot", "facebookexternalhit"
        ]
    
    def get_event_schema(self):
        """Define schema for incoming Event Hub messages"""
        return StructType([
            StructField("event_id", StringType(), False),
            StructField("timestamp", StringType(), False),
            StructField("campaign_id", StringType(), True),
            StructField("creative_id", StringType(), True),
            StructField("panelist_token", StringType(), True),
            StructField("user_agent", StringType(), True),
            StructField("device_type", StringType(), True),
            StructField("ip", StringType(), True),
            StructField("referrer", StringType(), True),
            StructField("ad_server", StringType(), True),
            StructField("s2s_flag", BooleanType(), False),
            StructField("raw_headers", MapType(StringType(), StringType()), True),
            StructField("tracking_hash", StringType(), True)
        ])
    
    def read_from_eventhub(self, eventhub_connection_string, consumer_group="$Default"):
        """
        Read streaming data from Azure Event Hubs
        
        Args:
            eventhub_connection_string: EventHub connection string with EntityPath
            consumer_group: Consumer group name
        """
        
        eventhub_conf = {
            "eventhubs.connectionString": self.spark._jvm.org.apache.spark.eventhubs.EventHubsUtils.encrypt(
                eventhub_connection_string
            ),
            "eventhubs.consumerGroup": consumer_group,
            "maxEventsPerTrigger": 10000
        }
        
        df = (self.spark
              .readStream
              .format("eventhubs")
              .options(**eventhub_conf)
              .load())
        
        return df
    
    def parse_events(self, df):
        """Parse JSON body from EventHub messages"""
        schema = self.get_event_schema()
        
        parsed_df = (df
                     .select(
                         from_json(col("body").cast("string"), schema).alias("data"),
                         col("enqueuedTime").alias("eventhub_enqueued_time"),
                         col("offset").alias("eventhub_offset"),
                         col("sequenceNumber").alias("eventhub_sequence_number"),
                         col("publisher").alias("eventhub_publisher"),
                         col("partitionKey").alias("eventhub_partition_key")
                     )
                     .select("data.*", 
                            "eventhub_enqueued_time", 
                            "eventhub_offset",
                            "eventhub_sequence_number",
                            "eventhub_publisher",
                            "eventhub_partition_key"))
        
        # Convert timestamp string to timestamp type
        parsed_df = parsed_df.withColumn(
            "timestamp", 
            to_timestamp(col("timestamp"))
        )
        
        # Add processing timestamp
        parsed_df = parsed_df.withColumn("processed_at", current_timestamp())
        
        return parsed_df
    
    def apply_bot_detection(self, df):
        """
        Apply bot detection heuristics
        - User-Agent blacklist matching
        - Rate threshold detection (handled in aggregation)
        """
        
        # Create bot detection condition based on user agent patterns
        bot_condition = lower(col("user_agent")).rlike("|".join(self.bot_user_agents))
        
        # Flag known bots
        df = df.withColumn(
            "is_bot",
            when(bot_condition, True)
            .when(col("user_agent").isNull(), True)
            .when(col("user_agent") == "Unknown", True)
            .when(col("s2s_flag") == False, False)  # S2S traffic needs separate validation
            .otherwise(False)
        )
        
        # Add bot detection reason
        df = df.withColumn(
            "bot_detection_reason",
            when(bot_condition, "user_agent_blacklist")
            .when(col("user_agent").isNull(), "missing_user_agent")
            .when(col("user_agent") == "Unknown", "unknown_user_agent")
            .otherwise(None)
        )
        
        return df
    
    def enrich_geo_data(self, df):
        """
        Enrich with geo data from IP address
        Mock implementation - replace with actual IP geolocation service
        """
        
        # Extract first octet for mock geo mapping
        df = df.withColumn(
            "ip_first_octet",
            regexp_extract(col("ip"), r"^(\d+)\.", 1).cast("int")
        )
        
        # Mock geo enrichment based on IP ranges
        df = (df
              .withColumn("country_code", 
                         when(col("ip_first_octet").between(1, 50), "US")
                         .when(col("ip_first_octet").between(51, 100), "UK")
                         .when(col("ip_first_octet").between(101, 150), "DE")
                         .when(col("ip_first_octet").between(151, 200), "FR")
                         .otherwise("XX"))
              .withColumn("country_name",
                         when(col("country_code") == "US", "United States")
                         .when(col("country_code") == "UK", "United Kingdom")
                         .when(col("country_code") == "DE", "Germany")
                         .when(col("country_code") == "FR", "France")
                         .otherwise("Unknown"))
              .withColumn("city",
                         when(col("country_code") == "US", "New York")
                         .when(col("country_code") == "UK", "London")
                         .when(col("country_code") == "DE", "Berlin")
                         .when(col("country_code") == "FR", "Paris")
                         .otherwise("Unknown"))
              .withColumn("region",
                         when(col("country_code") == "US", "NY")
                         .when(col("country_code") == "UK", "ENG")
                         .when(col("country_code") == "DE", "BE")
                         .when(col("country_code") == "FR", "IDF")
                         .otherwise("XX"))
              .withColumn("latitude",
                         when(col("country_code") == "US", 40.7128)
                         .when(col("country_code") == "UK", 51.5074)
                         .when(col("country_code") == "DE", 52.5200)
                         .when(col("country_code") == "FR", 48.8566)
                         .otherwise(0.0))
              .withColumn("longitude",
                         when(col("country_code") == "US", -74.0060)
                         .when(col("country_code") == "UK", -0.1278)
                         .when(col("country_code") == "DE", 13.4050)
                         .when(col("country_code") == "FR", 2.3522)
                         .otherwise(0.0))
              .drop("ip_first_octet"))
        
        return df
    
    def add_partitioning_columns(self, df):
        """Add date partitioning columns"""
        df = (df
              .withColumn("year", year(col("timestamp")))
              .withColumn("month", month(col("timestamp")))
              .withColumn("day", dayofmonth(col("timestamp")))
              .withColumn("date", col("timestamp").cast("date")))
        
        return df
    
    def write_to_adls_staging(self, df, adls_path, checkpoint_path):
        """
        Write streaming data to ADLS as Parquet partitioned by date
        
        Args:
            df: Streaming DataFrame
            adls_path: ADLS path (e.g., abfss://container@account.dfs.core.windows.net/staging/ad-impressions)
            checkpoint_path: Checkpoint location for streaming state
        """
        
        query = (df
                 .writeStream
                 .format("parquet")
                 .outputMode("append")
                 .option("checkpointLocation", checkpoint_path)
                 .partitionBy("year", "month", "day")
                 .option("path", adls_path)
                 .trigger(processingTime="30 seconds")
                 .start())
        
        return query
    
    def write_to_delta_staging(self, df, delta_path, checkpoint_path):
        """
        Write streaming data to Delta Lake format
        Better for schema evolution and ACID transactions
        
        Args:
            df: Streaming DataFrame
            delta_path: Delta table path
            checkpoint_path: Checkpoint location
        """
        
        query = (df
                 .writeStream
                 .format("delta")
                 .outputMode("append")
                 .option("checkpointLocation", checkpoint_path)
                 .partitionBy("year", "month", "day")
                 .option("path", delta_path)
                 .option("mergeSchema", "true")  # Handle schema evolution
                 .trigger(processingTime="30 seconds")
                 .start())
        
        return query
    
    def upsert_to_synapse(self, batch_df, batch_id, synapse_config):
        """
        Upsert normalized rows into Synapse staging table
        Uses tracking_hash as the unique key for deduplication
        
        Args:
            batch_df: Micro-batch DataFrame
            batch_id: Batch identifier
            synapse_config: Dictionary with Synapse connection details
                {
                    "jdbc_url": "jdbc:sqlserver://...",
                    "table": "staging.ad_impressions",
                    "user": "username",
                    "password": "password"
                }
        """
        
        if batch_df.isEmpty():
            print(f"Batch {batch_id}: No data to process")
            return
        
        print(f"Processing batch {batch_id} with {batch_df.count()} records")
        
        # Select and normalize columns for Synapse
        normalized_df = batch_df.select(
            col("event_id"),
            col("timestamp"),
            col("campaign_id"),
            col("creative_id"),
            col("panelist_token"),
            col("user_agent"),
            col("device_type"),
            col("ip"),
            col("referrer"),
            col("ad_server"),
            col("s2s_flag"),
            col("is_bot"),
            col("bot_detection_reason"),
            col("country_code"),
            col("country_name"),
            col("city"),
            col("region"),
            col("latitude"),
            col("longitude"),
            col("tracking_hash"),
            col("processed_at"),
            col("date")
        )
        
        # Create temp table for merge operation
        temp_table = f"temp_ad_impressions_{batch_id}"
        normalized_df.createOrReplaceTempView(temp_table)
        
        # Write to temp table in Synapse
        (normalized_df
         .write
         .format("jdbc")
         .option("url", synapse_config["jdbc_url"])
         .option("dbtable", f"staging.{temp_table}")
         .option("user", synapse_config["user"])
         .option("password", synapse_config["password"])
         .option("driver", "com.microsoft.sqlserver.jdbc.SQLServerDriver")
         .mode("overwrite")
         .save())
        
        # Execute MERGE statement for upsert
        merge_sql = f"""
        MERGE INTO {synapse_config['table']} AS target
        USING staging.{temp_table} AS source
        ON target.tracking_hash = source.tracking_hash
        WHEN MATCHED THEN
            UPDATE SET
                target.event_id = source.event_id,
                target.timestamp = source.timestamp,
                target.campaign_id = source.campaign_id,
                target.creative_id = source.creative_id,
                target.panelist_token = source.panelist_token,
                target.user_agent = source.user_agent,
                target.device_type = source.device_type,
                target.ip = source.ip,
                target.referrer = source.referrer,
                target.ad_server = source.ad_server,
                target.s2s_flag = source.s2s_flag,
                target.is_bot = source.is_bot,
                target.bot_detection_reason = source.bot_detection_reason,
                target.country_code = source.country_code,
                target.country_name = source.country_name,
                target.city = source.city,
                target.region = source.region,
                target.latitude = source.latitude,
                target.longitude = source.longitude,
                target.processed_at = source.processed_at,
                target.date = source.date
        WHEN NOT MATCHED THEN
            INSERT (
                event_id, timestamp, campaign_id, creative_id, panelist_token,
                user_agent, device_type, ip, referrer, ad_server, s2s_flag, is_bot,
                bot_detection_reason, country_code, country_name, city, region,
                latitude, longitude, tracking_hash, processed_at, date
            )
            VALUES (
                source.event_id, source.timestamp, source.campaign_id, source.creative_id,
                source.panelist_token, source.user_agent, source.device_type, source.ip, source.referrer,
                source.ad_server, source.s2s_flag, source.is_bot, source.bot_detection_reason,
                source.country_code, source.country_name, source.city, source.region,
                source.latitude, source.longitude, source.tracking_hash, source.processed_at,
                source.date
            );
        """
        
        # Execute merge via JDBC
        connection_properties = {
            "user": synapse_config["user"],
            "password": synapse_config["password"],
            "driver": "com.microsoft.sqlserver.jdbc.SQLServerDriver"
        }
        
        # Use Spark SQL to execute the merge
        self.spark.read \
            .format("jdbc") \
            .option("url", synapse_config["jdbc_url"]) \
            .option("query", merge_sql) \
            .option("user", synapse_config["user"]) \
            .option("password", synapse_config["password"]) \
            .option("driver", "com.microsoft.sqlserver.jdbc.SQLServerDriver") \
            .load()
        
        print(f"Batch {batch_id}: Successfully upserted {normalized_df.count()} records to Synapse")
        
        # Clean up temp table
        drop_temp_sql = f"DROP TABLE IF EXISTS staging.{temp_table}"
        self.spark.read \
            .format("jdbc") \
            .option("url", synapse_config["jdbc_url"]) \
            .option("query", drop_temp_sql) \
            .option("user", synapse_config["user"]) \
            .option("password", synapse_config["password"]) \
            .option("driver", "com.microsoft.sqlserver.jdbc.SQLServerDriver") \
            .load()
    
    def write_to_synapse_staging(self, df, synapse_config, checkpoint_path):
        """
        Write streaming data to Synapse with upsert logic using foreachBatch
        
        Args:
            df: Streaming DataFrame
            synapse_config: Synapse connection configuration
            checkpoint_path: Checkpoint location
        """
        
        query = (df
                 .writeStream
                 .foreachBatch(lambda batch_df, batch_id: self.upsert_to_synapse(batch_df, batch_id, synapse_config))
                 .option("checkpointLocation", checkpoint_path)
                 .trigger(processingTime="30 seconds")
                 .start())
        
        return query


def main():
    """Main execution function"""
    
    # Initialize Spark Session with EventHubs and Delta support
    spark = (SparkSession.builder
             .appName("AdTracking-EventHub-Processor")
             .config("spark.sql.extensions", "io.delta.sql.DeltaSparkSessionExtension")
             .config("spark.sql.catalog.spark_catalog", "org.apache.spark.sql.delta.catalog.DeltaCatalog")
             .config("spark.sql.streaming.schemaInference", "true")
             .config("spark.sql.adaptive.enabled", "true")
             .getOrCreate())
    
    # Configuration - these should come from Databricks secrets or environment variables
    eventhub_connection_string = dbutils.secrets.get(scope="adtracking-kv", key="eventhub-connection-string")
    consumer_group = "$Default"
    
    # Storage paths
    adls_staging_path = "abfss://staging@adtrackingdl.dfs.core.windows.net/ad-impressions"
    delta_staging_path = "abfss://staging@adtrackingdl.dfs.core.windows.net/delta/ad-impressions"
    checkpoint_path = "abfss://staging@adtrackingdl.dfs.core.windows.net/checkpoints/ad-impressions"
    synapse_checkpoint_path = "abfss://staging@adtrackingdl.dfs.core.windows.net/checkpoints/synapse-upsert"
    
    # Synapse configuration
    synapse_config = {
        "jdbc_url": dbutils.secrets.get(scope="adtracking-kv", key="synapse-jdbc-url"),
        "table": "staging.ad_impressions",
        "user": dbutils.secrets.get(scope="adtracking-kv", key="synapse-user"),
        "password": dbutils.secrets.get(scope="adtracking-kv", key="synapse-password")
    }
    
    # Initialize processor
    processor = EventHubStreamingProcessor(spark)
    
    # Read from EventHub
    raw_stream = processor.read_from_eventhub(eventhub_connection_string, consumer_group)
    
    # Parse events
    parsed_stream = processor.parse_events(raw_stream)
    
    # Apply bot detection
    bot_detected_stream = processor.apply_bot_detection(parsed_stream)
    
    # Enrich with geo data
    enriched_stream = processor.enrich_geo_data(bot_detected_stream)
    
    # Add partitioning columns
    final_stream = processor.add_partitioning_columns(enriched_stream)
    
    # Write to Delta Lake (recommended for ACID and schema evolution)
    delta_query = processor.write_to_delta_staging(
        final_stream,
        delta_staging_path,
        checkpoint_path
    )
    
    # Write to Synapse with upsert logic
    synapse_query = processor.write_to_synapse_staging(
        final_stream,
        synapse_config,
        synapse_checkpoint_path
    )
    
    # Keep both streams running
    delta_query.awaitTermination()
    synapse_query.awaitTermination()


if __name__ == "__main__":
    main()
