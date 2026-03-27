using Microsoft.Azure.Cosmos;
using System.Collections.ObjectModel;

namespace AdImpactOs.Survey.Migration;

public class SurveyDbMigration
{
    private readonly CosmosClient _cosmosClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SurveyDbMigration> _logger;

    public SurveyDbMigration(CosmosClient cosmosClient, IConfiguration configuration, ILogger<SurveyDbMigration> logger)
    {
        _cosmosClient = cosmosClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task RunMigrationAsync()
    {
        var databaseName = _configuration["CosmosDb:DatabaseName"] ?? "AdTrackingDB";
        var surveyContainerName = _configuration["CosmosDb:SurveyContainerName"] ?? "Surveys";
        var responseContainerName = _configuration["CosmosDb:SurveyResponseContainerName"] ?? "SurveyResponses";

        try
        {
            await _cosmosClient.CreateDatabaseIfNotExistsAsync(databaseName);
            var database = _cosmosClient.GetDatabase(databaseName);

            await CreateSurveyContainerAsync(database, surveyContainerName);
            await CreateResponseContainerAsync(database, responseContainerName);

            _logger.LogInformation("Survey migration completed successfully");
        }
        catch (CosmosException ex)
        {
            _logger.LogError(ex, "Survey migration failed: {Message}", ex.Message);
            throw;
        }
    }

    public async Task SeedSampleDataAsync()
    {
        var databaseName = _configuration["CosmosDb:DatabaseName"] ?? "AdTrackingDB";
        var surveyContainerName = _configuration["CosmosDb:SurveyContainerName"] ?? "Surveys";
        var responseContainerName = _configuration["CosmosDb:SurveyResponseContainerName"] ?? "SurveyResponses";
        var container = _cosmosClient.GetDatabase(databaseName).GetContainer(surveyContainerName);
        var responseContainer = _cosmosClient.GetDatabase(databaseName).GetContainer(responseContainerName);

        var sampleSurveys = new List<object>
        {
            new
            {
                id = "survey_summer_beverage_2024",
                surveyId = "survey_summer_beverage_2024",
                campaignId = "campaign_summer_beverage_2024",
                surveyName = "Summer Beverage Brand Lift Study",
                description = "Post-campaign survey to measure brand awareness and purchase intent for Summer Refresh energy drink launch",
                surveyType = "BrandLift",
                status = "Active",
                distributionStartDate = new DateTime(2024, 7, 15),
                distributionEndDate = new DateTime(2024, 9, 15, 23, 59, 59),
                targetAudience = "{\"ageRange\":[\"18-24\",\"25-34\"],\"cohorts\":[\"exposed\",\"control\"]}",
                questions = new List<object>
                {
                    new
                    {
                        questionId = "q1_awareness",
                        questionText = "How familiar are you with FreshBurst Energy Drink?",
                        questionType = "LikertScale",
                        metric = "brand_awareness",
                        options = new List<string> { "Not at all familiar", "Slightly familiar", "Moderately familiar", "Very familiar", "Extremely familiar" },
                        required = true,
                        order = 1
                    },
                    new
                    {
                        questionId = "q2_purchase_intent",
                        questionText = "How likely are you to purchase FreshBurst Energy Drink in the next 30 days?",
                        questionType = "LikertScale",
                        metric = "purchase_intent",
                        options = new List<string> { "Not at all likely", "Slightly likely", "Moderately likely", "Very likely", "Extremely likely" },
                        required = true,
                        order = 2
                    },
                    new
                    {
                        questionId = "q3_favorability",
                        questionText = "What is your overall opinion of FreshBurst Energy Drink?",
                        questionType = "LikertScale",
                        metric = "brand_favorability",
                        options = new List<string> { "Very unfavorable", "Somewhat unfavorable", "Neutral", "Somewhat favorable", "Very favorable" },
                        required = true,
                        order = 3
                    },
                    new
                    {
                        questionId = "q4_message_recall",
                        questionText = "Do you recall seeing any advertising for FreshBurst Energy Drink recently?",
                        questionType = "YesNo",
                        metric = "message_recall",
                        required = true,
                        order = 4
                    },
                    new
                    {
                        questionId = "q5_consideration",
                        questionText = "When choosing an energy drink, how likely would you be to consider FreshBurst?",
                        questionType = "Rating",
                        metric = "brand_consideration",
                        scale = 10,
                        required = false,
                        order = 5
                    }
                },
                createdAt = new DateTime(2024, 7, 10, 10, 0, 0),
                updatedAt = new DateTime(2024, 7, 15, 8, 0, 0)
            },
            new
            {
                id = "survey_retail_back_to_school_2024",
                surveyId = "survey_retail_back_to_school_2024",
                campaignId = "campaign_retail_back_to_school_2024",
                surveyName = "Back to School Retail Brand Lift",
                description = "Measure effectiveness of back-to-school campaign",
                surveyType = "BrandLift",
                status = "Active",
                distributionStartDate = new DateTime(2024, 8, 20),
                distributionEndDate = new DateTime(2024, 9, 30, 23, 59, 59),
                targetAudience = "{\"ageRange\":[\"25-34\",\"35-44\"],\"cohorts\":[\"exposed\",\"control\"]}",
                questions = new List<object>
                {
                    new
                    {
                        questionId = "q1_awareness",
                        questionText = "How familiar are you with SmartShop Retail?",
                        questionType = "LikertScale",
                        metric = "brand_awareness",
                        options = new List<string> { "Not at all familiar", "Slightly familiar", "Moderately familiar", "Very familiar", "Extremely familiar" },
                        required = true,
                        order = 1
                    },
                    new
                    {
                        questionId = "q2_purchase_intent",
                        questionText = "How likely are you to shop at SmartShop for back-to-school supplies?",
                        questionType = "LikertScale",
                        metric = "purchase_intent",
                        options = new List<string> { "Not at all likely", "Slightly likely", "Moderately likely", "Very likely", "Extremely likely" },
                        required = true,
                        order = 2
                    },
                    new
                    {
                        questionId = "q3_message_recall",
                        questionText = "Do you recall seeing any back-to-school advertising from SmartShop recently?",
                        questionType = "YesNo",
                        metric = "message_recall",
                        required = true,
                        order = 3
                    }
                },
                createdAt = new DateTime(2024, 8, 15, 10, 0, 0),
                updatedAt = new DateTime(2024, 8, 20, 8, 0, 0)
            },
            new
            {
                id = "survey_pharma_allergy_spring_2024",
                surveyId = "survey_pharma_allergy_spring_2024",
                campaignId = "campaign_pharma_allergy_spring_2024",
                surveyName = "Allergy Medication Brand Study",
                description = "Post-campaign brand lift for spring allergy medication",
                surveyType = "BrandLift",
                status = "Completed",
                distributionStartDate = new DateTime(2024, 5, 15),
                distributionEndDate = new DateTime(2024, 6, 15, 23, 59, 59),
                targetAudience = "{\"ageRange\":[\"25-34\",\"35-44\",\"45-54\",\"55-64\"],\"cohorts\":[\"exposed\",\"control\"]}",
                questions = new List<object>
                {
                    new
                    {
                        questionId = "q1_awareness",
                        questionText = "How familiar are you with HealthFirst Allergy Relief?",
                        questionType = "LikertScale",
                        metric = "brand_awareness",
                        options = new List<string> { "Not at all familiar", "Slightly familiar", "Moderately familiar", "Very familiar", "Extremely familiar" },
                        required = true,
                        order = 1
                    },
                    new
                    {
                        questionId = "q2_purchase_intent",
                        questionText = "How likely are you to use HealthFirst Allergy Relief for your allergy symptoms?",
                        questionType = "LikertScale",
                        metric = "purchase_intent",
                        options = new List<string> { "Not at all likely", "Slightly likely", "Moderately likely", "Very likely", "Extremely likely" },
                        required = true,
                        order = 2
                    },
                    new
                    {
                        questionId = "q3_favorability",
                        questionText = "What is your overall opinion of HealthFirst as a pharmaceutical brand?",
                        questionType = "LikertScale",
                        metric = "brand_favorability",
                        options = new List<string> { "Very unfavorable", "Somewhat unfavorable", "Neutral", "Somewhat favorable", "Very favorable" },
                        required = true,
                        order = 3
                    },
                    new
                    {
                        questionId = "q4_message_recall",
                        questionText = "Do you recall seeing advertising for HealthFirst Allergy Relief in the past 3 months?",
                        questionType = "YesNo",
                        metric = "message_recall",
                        required = true,
                        order = 4
                    }
                },
                createdAt = new DateTime(2024, 5, 10, 10, 0, 0),
                updatedAt = new DateTime(2024, 6, 15, 23, 59, 59)
            },
            new
            {
                id = "survey_finance_credit_card_q2_2024",
                surveyId = "survey_finance_credit_card_q2_2024",
                campaignId = "campaign_finance_credit_card_q2_2024",
                surveyName = "Credit Card Brand Lift Study",
                description = "Measure effectiveness of premium credit card campaign",
                surveyType = "BrandLift",
                status = "Completed",
                distributionStartDate = new DateTime(2024, 6, 15),
                distributionEndDate = new DateTime(2024, 7, 15, 23, 59, 59),
                targetAudience = "{\"ageRange\":[\"25-34\",\"35-44\",\"45-54\"],\"cohorts\":[\"exposed\",\"control\"]}",
                questions = new List<object>
                {
                    new
                    {
                        questionId = "q1_awareness",
                        questionText = "How familiar are you with GlobalBank Rewards Plus credit card?",
                        questionType = "LikertScale",
                        metric = "brand_awareness",
                        options = new List<string> { "Not at all familiar", "Slightly familiar", "Moderately familiar", "Very familiar", "Extremely familiar" },
                        required = true,
                        order = 1
                    },
                    new
                    {
                        questionId = "q2_application_intent",
                        questionText = "How likely are you to apply for the GlobalBank Rewards Plus card?",
                        questionType = "LikertScale",
                        metric = "purchase_intent",
                        options = new List<string> { "Not at all likely", "Slightly likely", "Moderately likely", "Very likely", "Extremely likely" },
                        required = true,
                        order = 2
                    },
                    new
                    {
                        questionId = "q3_value_perception",
                        questionText = "Rate the perceived value of the rewards program (1-10)",
                        questionType = "Rating",
                        metric = "value_perception",
                        scale = 10,
                        required = true,
                        order = 3
                    }
                },
                createdAt = new DateTime(2024, 6, 10, 10, 0, 0),
                updatedAt = new DateTime(2024, 7, 15, 23, 59, 59)
            },
            new
            {
                id = "survey_travel_cruise_summer_2024",
                surveyId = "survey_travel_cruise_summer_2024",
                campaignId = "campaign_travel_cruise_summer_2024",
                surveyName = "Luxury Cruise Brand Lift",
                description = "Mediterranean cruise campaign effectiveness study",
                surveyType = "BrandLift",
                status = "Active",
                distributionStartDate = new DateTime(2024, 6, 15),
                distributionEndDate = new DateTime(2024, 8, 15, 23, 59, 59),
                targetAudience = "{\"ageRange\":[\"35-44\",\"45-54\",\"55-64\",\"65+\"],\"cohorts\":[\"exposed\",\"control\"]}",
                questions = new List<object>
                {
                    new
                    {
                        questionId = "q1_awareness",
                        questionText = "How familiar are you with OceanVoyage Cruises?",
                        questionType = "LikertScale",
                        metric = "brand_awareness",
                        options = new List<string> { "Not at all familiar", "Slightly familiar", "Moderately familiar", "Very familiar", "Extremely familiar" },
                        required = true,
                        order = 1
                    },
                    new
                    {
                        questionId = "q2_booking_intent",
                        questionText = "How likely are you to book a Mediterranean cruise with OceanVoyage in the next 12 months?",
                        questionType = "LikertScale",
                        metric = "purchase_intent",
                        options = new List<string> { "Not at all likely", "Slightly likely", "Moderately likely", "Very likely", "Extremely likely" },
                        required = true,
                        order = 2
                    },
                    new
                    {
                        questionId = "q3_luxury_perception",
                        questionText = "How would you rate OceanVoyage in terms of luxury and quality?",
                        questionType = "Rating",
                        metric = "brand_perception",
                        scale = 10,
                        required = true,
                        order = 3
                    }
                },
                createdAt = new DateTime(2024, 6, 10, 10, 0, 0),
                updatedAt = new DateTime(2024, 6, 15, 8, 0, 0)
            },
            new
            {
                id = "survey_food_delivery_q3_2024",
                surveyId = "survey_food_delivery_q3_2024",
                campaignId = "campaign_food_delivery_q3_2024",
                surveyName = "Food Delivery App Brand Study",
                description = "Brand lift measurement for QuickBite app launch",
                surveyType = "BrandLift",
                status = "Active",
                distributionStartDate = new DateTime(2024, 8, 1),
                distributionEndDate = new DateTime(2024, 10, 1, 23, 59, 59),
                targetAudience = "{\"ageRange\":[\"18-24\",\"25-34\",\"35-44\"],\"cohorts\":[\"exposed\",\"control\"]}",
                questions = new List<object>
                {
                    new
                    {
                        questionId = "q1_awareness",
                        questionText = "How familiar are you with the QuickBite food delivery app?",
                        questionType = "LikertScale",
                        metric = "brand_awareness",
                        options = new List<string> { "Not at all familiar", "Slightly familiar", "Moderately familiar", "Very familiar", "Extremely familiar" },
                        required = true,
                        order = 1
                    },
                    new
                    {
                        questionId = "q2_download_intent",
                        questionText = "How likely are you to download and use QuickBite?",
                        questionType = "LikertScale",
                        metric = "purchase_intent",
                        options = new List<string> { "Not at all likely", "Slightly likely", "Moderately likely", "Very likely", "Extremely likely" },
                        required = true,
                        order = 2
                    },
                    new
                    {
                        questionId = "q3_message_recall",
                        questionText = "Do you recall seeing any advertising for QuickBite recently?",
                        questionType = "YesNo",
                        metric = "message_recall",
                        required = true,
                        order = 3
                    }
                },
                createdAt = new DateTime(2024, 7, 25, 10, 0, 0),
                updatedAt = new DateTime(2024, 8, 1, 8, 0, 0)
            },
            new
            {
                id = "survey_auto_suv_fall_2024",
                surveyId = "survey_auto_suv_fall_2024",
                campaignId = "campaign_auto_suv_fall_2024",
                surveyName = "SUV Purchase Intent Study",
                description = "Brand lift for DriveMax SUV launch campaign targeting families",
                surveyType = "BrandLift",
                status = "Scheduled",
                distributionStartDate = new DateTime(2024, 9, 15),
                distributionEndDate = new DateTime(2024, 12, 15, 23, 59, 59),
                targetAudience = "{\"ageRange\":[\"25-34\",\"35-44\",\"45-54\"],\"cohorts\":[\"exposed\",\"control\"]}",
                questions = new List<object>
                {
                    new
                    {
                        questionId = "q1_awareness",
                        questionText = "How familiar are you with DriveMax Automotive?",
                        questionType = "LikertScale",
                        metric = "brand_awareness",
                        options = new List<string> { "Not at all familiar", "Slightly familiar", "Moderately familiar", "Very familiar", "Extremely familiar" },
                        required = true,
                        order = 1
                    },
                    new
                    {
                        questionId = "q2_purchase_intent",
                        questionText = "How likely are you to consider DriveMax SUV for your next vehicle purchase?",
                        questionType = "LikertScale",
                        metric = "purchase_intent",
                        options = new List<string> { "Not at all likely", "Slightly likely", "Moderately likely", "Very likely", "Extremely likely" },
                        required = true,
                        order = 2
                    },
                    new
                    {
                        questionId = "q3_favorability",
                        questionText = "What is your overall opinion of DriveMax vehicles?",
                        questionType = "LikertScale",
                        metric = "brand_favorability",
                        options = new List<string> { "Very unfavorable", "Somewhat unfavorable", "Neutral", "Somewhat favorable", "Very favorable" },
                        required = true,
                        order = 3
                    },
                    new
                    {
                        questionId = "q4_message_recall",
                        questionText = "Do you recall seeing any advertising for DriveMax SUVs recently?",
                        questionType = "YesNo",
                        metric = "message_recall",
                        required = true,
                        order = 4
                    }
                },
                createdAt = new DateTime(2024, 8, 20, 10, 0, 0),
                updatedAt = new DateTime(2024, 8, 20, 10, 0, 0)
            },
            new
            {
                id = "survey_tech_smartphone_holiday_2024",
                surveyId = "survey_tech_smartphone_holiday_2024",
                campaignId = "campaign_tech_smartphone_holiday_2024",
                surveyName = "Smartphone Brand Lift - Holiday Season",
                description = "Measure brand awareness and purchase intent for TechNova flagship smartphone",
                surveyType = "BrandLift",
                status = "Scheduled",
                distributionStartDate = new DateTime(2024, 11, 15),
                distributionEndDate = new DateTime(2025, 1, 15, 23, 59, 59),
                targetAudience = "{\"ageRange\":[\"18-24\",\"25-34\",\"35-44\"],\"cohorts\":[\"exposed\",\"control\"]}",
                questions = new List<object>
                {
                    new
                    {
                        questionId = "q1_awareness",
                        questionText = "How familiar are you with TechNova smartphones?",
                        questionType = "LikertScale",
                        metric = "brand_awareness",
                        options = new List<string> { "Not at all familiar", "Slightly familiar", "Moderately familiar", "Very familiar", "Extremely familiar" },
                        required = true,
                        order = 1
                    },
                    new
                    {
                        questionId = "q2_purchase_intent",
                        questionText = "How likely are you to purchase a TechNova smartphone in the next 6 months?",
                        questionType = "LikertScale",
                        metric = "purchase_intent",
                        options = new List<string> { "Not at all likely", "Slightly likely", "Moderately likely", "Very likely", "Extremely likely" },
                        required = true,
                        order = 2
                    },
                    new
                    {
                        questionId = "q3_feature_perception",
                        questionText = "How would you rate TechNova's camera quality compared to competitors?",
                        questionType = "Rating",
                        metric = "feature_perception",
                        scale = 10,
                        required = true,
                        order = 3
                    },
                    new
                    {
                        questionId = "q4_message_recall",
                        questionText = "Do you recall seeing any TechNova holiday advertising?",
                        questionType = "YesNo",
                        metric = "message_recall",
                        required = true,
                        order = 4
                    }
                },
                createdAt = new DateTime(2024, 10, 1, 10, 0, 0),
                updatedAt = new DateTime(2024, 10, 1, 10, 0, 0)
            },
            new
            {
                id = "survey_insurance_home_q4_2024",
                surveyId = "survey_insurance_home_q4_2024",
                campaignId = "campaign_insurance_home_q4_2024",
                surveyName = "Home Insurance Bundle Brand Study",
                description = "Measure effectiveness of SecureLife insurance bundle campaign",
                surveyType = "BrandLift",
                status = "Scheduled",
                distributionStartDate = new DateTime(2024, 10, 15),
                distributionEndDate = new DateTime(2025, 1, 15, 23, 59, 59),
                targetAudience = "{\"ageRange\":[\"25-34\",\"35-44\",\"45-54\"],\"cohorts\":[\"exposed\",\"control\"]}",
                questions = new List<object>
                {
                    new
                    {
                        questionId = "q1_awareness",
                        questionText = "How familiar are you with SecureLife Insurance?",
                        questionType = "LikertScale",
                        metric = "brand_awareness",
                        options = new List<string> { "Not at all familiar", "Slightly familiar", "Moderately familiar", "Very familiar", "Extremely familiar" },
                        required = true,
                        order = 1
                    },
                    new
                    {
                        questionId = "q2_quote_intent",
                        questionText = "How likely are you to request a quote from SecureLife for home insurance?",
                        questionType = "LikertScale",
                        metric = "purchase_intent",
                        options = new List<string> { "Not at all likely", "Slightly likely", "Moderately likely", "Very likely", "Extremely likely" },
                        required = true,
                        order = 2
                    },
                    new
                    {
                        questionId = "q3_trust",
                        questionText = "How much do you trust SecureLife as an insurance provider?",
                        questionType = "Rating",
                        metric = "brand_trust",
                        scale = 10,
                        required = true,
                        order = 3
                    }
                },
                createdAt = new DateTime(2024, 9, 20, 10, 0, 0),
                updatedAt = new DateTime(2024, 9, 20, 10, 0, 0)
            },
            new
            {
                id = "survey_fitness_gym_new_year_2025",
                surveyId = "survey_fitness_gym_new_year_2025",
                campaignId = "campaign_fitness_gym_new_year_2025",
                surveyName = "New Year Fitness Membership Study",
                description = "Brand lift for FitLife gym membership campaign targeting New Year resolutions",
                surveyType = "BrandLift",
                status = "Scheduled",
                distributionStartDate = new DateTime(2024, 12, 20),
                distributionEndDate = new DateTime(2025, 3, 15, 23, 59, 59),
                targetAudience = "{\"ageRange\":[\"18-24\",\"25-34\",\"35-44\"],\"cohorts\":[\"exposed\",\"control\"]}",
                questions = new List<object>
                {
                    new
                    {
                        questionId = "q1_awareness",
                        questionText = "How familiar are you with FitLife Gyms?",
                        questionType = "LikertScale",
                        metric = "brand_awareness",
                        options = new List<string> { "Not at all familiar", "Slightly familiar", "Moderately familiar", "Very familiar", "Extremely familiar" },
                        required = true,
                        order = 1
                    },
                    new
                    {
                        questionId = "q2_membership_intent",
                        questionText = "How likely are you to join FitLife Gyms in 2025?",
                        questionType = "LikertScale",
                        metric = "purchase_intent",
                        options = new List<string> { "Not at all likely", "Slightly likely", "Moderately likely", "Very likely", "Extremely likely" },
                        required = true,
                        order = 2
                    },
                    new
                    {
                        questionId = "q3_message_recall",
                        questionText = "Do you recall seeing any New Year promotions from FitLife?",
                        questionType = "YesNo",
                        metric = "message_recall",
                        required = true,
                        order = 3
                    },
                    new
                    {
                        questionId = "q4_motivation",
                        questionText = "How motivated do you feel by FitLife's advertising message?",
                        questionType = "Rating",
                        metric = "message_impact",
                        scale = 10,
                        required = false,
                        order = 4
                    }
                },
                createdAt = new DateTime(2024, 11, 15, 10, 0, 0),
                updatedAt = new DateTime(2024, 11, 15, 10, 0, 0)
            }
        };

        foreach (var survey in sampleSurveys)
        {
            try
            {
                dynamic s = survey;
                string surveyId = s.surveyId;
                await container.CreateItemAsync(survey, new PartitionKey(surveyId));
                _logger.LogInformation("Seeded survey: {SurveyId}", surveyId);
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                dynamic s = survey;
                string surveyId = s.surveyId;
                _logger.LogInformation("Survey already exists: {SurveyId}", surveyId);
            }
        }

        _logger.LogInformation("Sample survey data seeding completed - 10 surveys seeded");

        // Seed survey responses - at least 2 for each campaign/survey/panelist combination
        await SeedSurveyResponsesAsync(responseContainer);
    }

    private async Task SeedSurveyResponsesAsync(Container responseContainer)
    {
        // Original 40 hand-crafted responses
        var sampleResponses = new List<object>
        {
            // Summer Beverage - exposed
            new
            {
                id = "response_001",
                responseId = "response_001",
                surveyId = "survey_summer_beverage_2024",
                campaignId = "campaign_summer_beverage_2024",
                panelistId = "panelist-001",
                cohortType = "exposed",
                answers = new List<object>
                {
                    new { questionId = "q1_awareness", answer = "Very familiar", numericValue = 4.0 },
                    new { questionId = "q2_purchase_intent", answer = "Very likely", numericValue = 4.0 },
                    new { questionId = "q3_favorability", answer = "Somewhat favorable", numericValue = 4.0 },
                    new { questionId = "q4_message_recall", answer = "Yes", numericValue = 1.0 },
                    new { questionId = "q5_consideration", answer = "8", numericValue = 8.0 }
                },
                completedAt = new DateTime(2024, 7, 20, 14, 30, 0),
                responseTime = 185,
                deviceType = "Mobile",
                createdAt = new DateTime(2024, 7, 20, 14, 30, 0),
                status = "Completed"
            },
            new
            {
                id = "response_002",
                responseId = "response_002",
                surveyId = "survey_summer_beverage_2024",
                campaignId = "campaign_summer_beverage_2024",
                panelistId = "panelist-002",
                cohortType = "exposed",
                answers = new List<object>
                {
                    new { questionId = "q1_awareness", answer = "Extremely familiar", numericValue = 5.0 },
                    new { questionId = "q2_purchase_intent", answer = "Extremely likely", numericValue = 5.0 },
                    new { questionId = "q3_favorability", answer = "Very favorable", numericValue = 5.0 },
                    new { questionId = "q4_message_recall", answer = "Yes", numericValue = 1.0 },
                    new { questionId = "q5_consideration", answer = "9", numericValue = 9.0 }
                },
                completedAt = new DateTime(2024, 7, 21, 9, 15, 0),
                responseTime = 142,
                deviceType = "Desktop",
                createdAt = new DateTime(2024, 7, 21, 9, 15, 0),
                status = "Completed"
            },
            // Summer Beverage - control
            new
            {
                id = "response_003",
                responseId = "response_003",
                surveyId = "survey_summer_beverage_2024",
                campaignId = "campaign_summer_beverage_2024",
                panelistId = "panelist-003",
                cohortType = "control",
                answers = new List<object>
                {
                    new { questionId = "q1_awareness", answer = "Slightly familiar", numericValue = 2.0 },
                    new { questionId = "q2_purchase_intent", answer = "Slightly likely", numericValue = 2.0 },
                    new { questionId = "q3_favorability", answer = "Neutral", numericValue = 3.0 },
                    new { questionId = "q4_message_recall", answer = "No", numericValue = 0.0 },
                    new { questionId = "q5_consideration", answer = "4", numericValue = 4.0 }
                },
                completedAt = new DateTime(2024, 7, 22, 11, 0, 0),
                responseTime = 198,
                deviceType = "Mobile",
                createdAt = new DateTime(2024, 7, 22, 11, 0, 0),
                status = "Completed"
            },
            new
            {
                id = "response_004",
                responseId = "response_004",
                surveyId = "survey_summer_beverage_2024",
                campaignId = "campaign_summer_beverage_2024",
                panelistId = "panelist-004",
                cohortType = "control",
                answers = new List<object>
                {
                    new { questionId = "q1_awareness", answer = "Not at all familiar", numericValue = 1.0 },
                    new { questionId = "q2_purchase_intent", answer = "Not at all likely", numericValue = 1.0 },
                    new { questionId = "q3_favorability", answer = "Neutral", numericValue = 3.0 },
                    new { questionId = "q4_message_recall", answer = "No", numericValue = 0.0 },
                    new { questionId = "q5_consideration", answer = "3", numericValue = 3.0 }
                },
                completedAt = new DateTime(2024, 7, 23, 16, 45, 0),
                responseTime = 167,
                deviceType = "Desktop",
                createdAt = new DateTime(2024, 7, 23, 16, 45, 0),
                status = "Completed"
            },
            // Retail Back to School - exposed
            new
            {
                id = "response_005",
                responseId = "response_005",
                surveyId = "survey_retail_back_to_school_2024",
                campaignId = "campaign_retail_back_to_school_2024",
                panelistId = "panelist-007",
                cohortType = "exposed",
                answers = new List<object>
                {
                    new { questionId = "q1_awareness", answer = "Very familiar", numericValue = 4.0 },
                    new { questionId = "q2_purchase_intent", answer = "Very likely", numericValue = 4.0 },
                    new { questionId = "q3_message_recall", answer = "Yes", numericValue = 1.0 }
                },
                completedAt = new DateTime(2024, 8, 25, 10, 30, 0),
                responseTime = 120,
                deviceType = "Mobile",
                createdAt = new DateTime(2024, 8, 25, 10, 30, 0),
                status = "Completed"
            },
            new
            {
                id = "response_006",
                responseId = "response_006",
                surveyId = "survey_retail_back_to_school_2024",
                campaignId = "campaign_retail_back_to_school_2024",
                panelistId = "panelist-003",
                cohortType = "exposed",
                answers = new List<object>
                {
                    new { questionId = "q1_awareness", answer = "Moderately familiar", numericValue = 3.0 },
                    new { questionId = "q2_purchase_intent", answer = "Moderately likely", numericValue = 3.0 },
                    new { questionId = "q3_message_recall", answer = "Yes", numericValue = 1.0 }
                },
                completedAt = new DateTime(2024, 8, 26, 14, 20, 0),
                responseTime = 98,
                deviceType = "Mobile",
                createdAt = new DateTime(2024, 8, 26, 14, 20, 0),
                status = "Completed"
            },
            // Retail Back to School - control
            new
            {
                id = "response_007",
                responseId = "response_007",
                surveyId = "survey_retail_back_to_school_2024",
                campaignId = "campaign_retail_back_to_school_2024",
                panelistId = "panelist-006",
                cohortType = "control",
                answers = new List<object>
                {
                    new { questionId = "q1_awareness", answer = "Slightly familiar", numericValue = 2.0 },
                    new { questionId = "q2_purchase_intent", answer = "Slightly likely", numericValue = 2.0 },
                    new { questionId = "q3_message_recall", answer = "No", numericValue = 0.0 }
                },
                completedAt = new DateTime(2024, 8, 27, 11, 10, 0),
                responseTime = 85,
                deviceType = "Desktop",
                createdAt = new DateTime(2024, 8, 27, 11, 10, 0),
                status = "Completed"
            },
            new
            {
                id = "response_008",
                responseId = "response_008",
                surveyId = "survey_retail_back_to_school_2024",
                campaignId = "campaign_retail_back_to_school_2024",
                panelistId = "panelist-017",
                cohortType = "control",
                answers = new List<object>
                {
                    new { questionId = "q1_awareness", answer = "Not at all familiar", numericValue = 1.0 },
                    new { questionId = "q2_purchase_intent", answer = "Slightly likely", numericValue = 2.0 },
                    new { questionId = "q3_message_recall", answer = "No", numericValue = 0.0 }
                },
                completedAt = new DateTime(2024, 8, 28, 9, 0, 0),
                responseTime = 76,
                deviceType = "Mobile",
                createdAt = new DateTime(2024, 8, 28, 9, 0, 0),
                status = "Completed"
            },
            // Pharma Allergy - exposed
            new
            {
                id = "response_009",
                responseId = "response_009",
                surveyId = "survey_pharma_allergy_spring_2024",
                campaignId = "campaign_pharma_allergy_spring_2024",
                panelistId = "panelist-009",
                cohortType = "exposed",
                answers = new List<object>
                {
                    new { questionId = "q1_awareness", answer = "Extremely familiar", numericValue = 5.0 },
                    new { questionId = "q2_purchase_intent", answer = "Very likely", numericValue = 4.0 },
                    new { questionId = "q3_favorability", answer = "Very favorable", numericValue = 5.0 },
                    new { questionId = "q4_message_recall", answer = "Yes", numericValue = 1.0 }
                },
                completedAt = new DateTime(2024, 5, 20, 15, 0, 0),
                responseTime = 210,
                deviceType = "Tablet",
                createdAt = new DateTime(2024, 5, 20, 15, 0, 0),
                status = "Completed"
            },
            new
            {
                id = "response_010",
                responseId = "response_010",
                surveyId = "survey_pharma_allergy_spring_2024",
                campaignId = "campaign_pharma_allergy_spring_2024",
                panelistId = "panelist-006",
                cohortType = "exposed",
                answers = new List<object>
                {
                    new { questionId = "q1_awareness", answer = "Very familiar", numericValue = 4.0 },
                    new { questionId = "q2_purchase_intent", answer = "Moderately likely", numericValue = 3.0 },
                    new { questionId = "q3_favorability", answer = "Somewhat favorable", numericValue = 4.0 },
                    new { questionId = "q4_message_recall", answer = "Yes", numericValue = 1.0 }
                },
                completedAt = new DateTime(2024, 5, 22, 10, 30, 0),
                responseTime = 175,
                deviceType = "Desktop",
                createdAt = new DateTime(2024, 5, 22, 10, 30, 0),
                status = "Completed"
            },
            // Pharma Allergy - control
            new
            {
                id = "response_011",
                responseId = "response_011",
                surveyId = "survey_pharma_allergy_spring_2024",
                campaignId = "campaign_pharma_allergy_spring_2024",
                panelistId = "panelist-012",
                cohortType = "control",
                answers = new List<object>
                {
                    new { questionId = "q1_awareness", answer = "Slightly familiar", numericValue = 2.0 },
                    new { questionId = "q2_purchase_intent", answer = "Not at all likely", numericValue = 1.0 },
                    new { questionId = "q3_favorability", answer = "Neutral", numericValue = 3.0 },
                    new { questionId = "q4_message_recall", answer = "No", numericValue = 0.0 }
                },
                completedAt = new DateTime(2024, 5, 25, 13, 0, 0),
                responseTime = 155,
                deviceType = "Desktop",
                createdAt = new DateTime(2024, 5, 25, 13, 0, 0),
                status = "Completed"
            },
            new
            {
                id = "response_012",
                responseId = "response_012",
                surveyId = "survey_pharma_allergy_spring_2024",
                campaignId = "campaign_pharma_allergy_spring_2024",
                panelistId = "panelist-010",
                cohortType = "control",
                answers = new List<object>
                {
                    new { questionId = "q1_awareness", answer = "Moderately familiar", numericValue = 3.0 },
                    new { questionId = "q2_purchase_intent", answer = "Slightly likely", numericValue = 2.0 },
                    new { questionId = "q3_favorability", answer = "Neutral", numericValue = 3.0 },
                    new { questionId = "q4_message_recall", answer = "No", numericValue = 0.0 }
                },
                completedAt = new DateTime(2024, 5, 28, 17, 15, 0),
                responseTime = 190,
                deviceType = "Desktop",
                createdAt = new DateTime(2024, 5, 28, 17, 15, 0),
                status = "Completed"
            },
            // Finance Credit Card - exposed
            new
            {
                id = "response_013",
                responseId = "response_013",
                surveyId = "survey_finance_credit_card_q2_2024",
                campaignId = "campaign_finance_credit_card_q2_2024",
                panelistId = "panelist-004",
                cohortType = "exposed",
                answers = new List<object>
                {
                    new { questionId = "q1_awareness", answer = "Very familiar", numericValue = 4.0 },
                    new { questionId = "q2_application_intent", answer = "Moderately likely", numericValue = 3.0 },
                    new { questionId = "q3_value_perception", answer = "7", numericValue = 7.0 }
                },
                completedAt = new DateTime(2024, 6, 20, 11, 0, 0),
                responseTime = 130,
                deviceType = "Desktop",
                createdAt = new DateTime(2024, 6, 20, 11, 0, 0),
                status = "Completed"
            },
            new
            {
                id = "response_014",
                responseId = "response_014",
                surveyId = "survey_finance_credit_card_q2_2024",
                campaignId = "campaign_finance_credit_card_q2_2024",
                panelistId = "panelist-008",
                cohortType = "exposed",
                answers = new List<object>
                {
                    new { questionId = "q1_awareness", answer = "Extremely familiar", numericValue = 5.0 },
                    new { questionId = "q2_application_intent", answer = "Very likely", numericValue = 4.0 },
                    new { questionId = "q3_value_perception", answer = "9", numericValue = 9.0 }
                },
                completedAt = new DateTime(2024, 6, 22, 9, 30, 0),
                responseTime = 105,
                deviceType = "Desktop",
                createdAt = new DateTime(2024, 6, 22, 9, 30, 0),
                status = "Completed"
            },
            // Finance Credit Card - control
            new
            {
                id = "response_015",
                responseId = "response_015",
                surveyId = "survey_finance_credit_card_q2_2024",
                campaignId = "campaign_finance_credit_card_q2_2024",
                panelistId = "panelist-018",
                cohortType = "control",
                answers = new List<object>
                {
                    new { questionId = "q1_awareness", answer = "Slightly familiar", numericValue = 2.0 },
                    new { questionId = "q2_application_intent", answer = "Not at all likely", numericValue = 1.0 },
                    new { questionId = "q3_value_perception", answer = "4", numericValue = 4.0 }
                },
                completedAt = new DateTime(2024, 6, 25, 14, 0, 0),
                responseTime = 95,
                deviceType = "Desktop",
                createdAt = new DateTime(2024, 6, 25, 14, 0, 0),
                status = "Completed"
            },
            new
            {
                id = "response_016",
                responseId = "response_016",
                surveyId = "survey_finance_credit_card_q2_2024",
                campaignId = "campaign_finance_credit_card_q2_2024",
                panelistId = "panelist-020",
                cohortType = "control",
                answers = new List<object>
                {
                    new { questionId = "q1_awareness", answer = "Moderately familiar", numericValue = 3.0 },
                    new { questionId = "q2_application_intent", answer = "Slightly likely", numericValue = 2.0 },
                    new { questionId = "q3_value_perception", answer = "5", numericValue = 5.0 }
                },
                completedAt = new DateTime(2024, 6, 28, 10, 20, 0),
                responseTime = 112,
                deviceType = "Desktop",
                createdAt = new DateTime(2024, 6, 28, 10, 20, 0),
                status = "Completed"
            },
            // Travel Cruise - exposed
            new
            {
                id = "response_017",
                responseId = "response_017",
                surveyId = "survey_travel_cruise_summer_2024",
                campaignId = "campaign_travel_cruise_summer_2024",
                panelistId = "panelist-012",
                cohortType = "exposed",
                answers = new List<object>
                {
                    new { questionId = "q1_awareness", answer = "Very familiar", numericValue = 4.0 },
                    new { questionId = "q2_booking_intent", answer = "Moderately likely", numericValue = 3.0 },
                    new { questionId = "q3_luxury_perception", answer = "8", numericValue = 8.0 }
                },
                completedAt = new DateTime(2024, 7, 1, 15, 30, 0),
                responseTime = 145,
                deviceType = "Desktop",
                createdAt = new DateTime(2024, 7, 1, 15, 30, 0),
                status = "Completed"
            },
            new
            {
                id = "response_018",
                responseId = "response_018",
                surveyId = "survey_travel_cruise_summer_2024",
                campaignId = "campaign_travel_cruise_summer_2024",
                panelistId = "panelist-015",
                cohortType = "exposed",
                answers = new List<object>
                {
                    new { questionId = "q1_awareness", answer = "Extremely familiar", numericValue = 5.0 },
                    new { questionId = "q2_booking_intent", answer = "Very likely", numericValue = 4.0 },
                    new { questionId = "q3_luxury_perception", answer = "9", numericValue = 9.0 }
                },
                completedAt = new DateTime(2024, 7, 3, 10, 0, 0),
                responseTime = 160,
                deviceType = "Desktop",
                createdAt = new DateTime(2024, 7, 3, 10, 0, 0),
                status = "Completed"
            },
            // Travel Cruise - control
            new
            {
                id = "response_019",
                responseId = "response_019",
                surveyId = "survey_travel_cruise_summer_2024",
                campaignId = "campaign_travel_cruise_summer_2024",
                panelistId = "panelist-009",
                cohortType = "control",
                answers = new List<object>
                {
                    new { questionId = "q1_awareness", answer = "Slightly familiar", numericValue = 2.0 },
                    new { questionId = "q2_booking_intent", answer = "Not at all likely", numericValue = 1.0 },
                    new { questionId = "q3_luxury_perception", answer = "5", numericValue = 5.0 }
                },
                completedAt = new DateTime(2024, 7, 5, 12, 30, 0),
                responseTime = 110,
                deviceType = "Tablet",
                createdAt = new DateTime(2024, 7, 5, 12, 30, 0),
                status = "Completed"
            },
            new
            {
                id = "response_020",
                responseId = "response_020",
                surveyId = "survey_travel_cruise_summer_2024",
                campaignId = "campaign_travel_cruise_summer_2024",
                panelistId = "panelist-016",
                cohortType = "control",
                answers = new List<object>
                {
                    new { questionId = "q1_awareness", answer = "Moderately familiar", numericValue = 3.0 },
                    new { questionId = "q2_booking_intent", answer = "Slightly likely", numericValue = 2.0 },
                    new { questionId = "q3_luxury_perception", answer = "6", numericValue = 6.0 }
                },
                completedAt = new DateTime(2024, 7, 8, 14, 0, 0),
                responseTime = 135,
                deviceType = "Tablet",
                createdAt = new DateTime(2024, 7, 8, 14, 0, 0),
                status = "Completed"
            },
            // Food Delivery - exposed
            new
            {
                id = "response_021",
                responseId = "response_021",
                surveyId = "survey_food_delivery_q3_2024",
                campaignId = "campaign_food_delivery_q3_2024",
                panelistId = "panelist-001",
                cohortType = "exposed",
                answers = new List<object>
                {
                    new { questionId = "q1_awareness", answer = "Very familiar", numericValue = 4.0 },
                    new { questionId = "q2_download_intent", answer = "Extremely likely", numericValue = 5.0 },
                    new { questionId = "q3_message_recall", answer = "Yes", numericValue = 1.0 }
                },
                completedAt = new DateTime(2024, 8, 10, 18, 0, 0),
                responseTime = 88,
                deviceType = "Mobile",
                createdAt = new DateTime(2024, 8, 10, 18, 0, 0),
                status = "Completed"
            },
            new
            {
                id = "response_022",
                responseId = "response_022",
                surveyId = "survey_food_delivery_q3_2024",
                campaignId = "campaign_food_delivery_q3_2024",
                panelistId = "panelist-019",
                cohortType = "exposed",
                answers = new List<object>
                {
                    new { questionId = "q1_awareness", answer = "Moderately familiar", numericValue = 3.0 },
                    new { questionId = "q2_download_intent", answer = "Very likely", numericValue = 4.0 },
                    new { questionId = "q3_message_recall", answer = "Yes", numericValue = 1.0 }
                },
                completedAt = new DateTime(2024, 8, 12, 12, 15, 0),
                responseTime = 75,
                deviceType = "Mobile",
                createdAt = new DateTime(2024, 8, 12, 12, 15, 0),
                status = "Completed"
            },
            // Food Delivery - control
            new
            {
                id = "response_023",
                responseId = "response_023",
                surveyId = "survey_food_delivery_q3_2024",
                campaignId = "campaign_food_delivery_q3_2024",
                panelistId = "panelist-005",
                cohortType = "control",
                answers = new List<object>
                {
                    new { questionId = "q1_awareness", answer = "Not at all familiar", numericValue = 1.0 },
                    new { questionId = "q2_download_intent", answer = "Slightly likely", numericValue = 2.0 },
                    new { questionId = "q3_message_recall", answer = "No", numericValue = 0.0 }
                },
                completedAt = new DateTime(2024, 8, 15, 9, 30, 0),
                responseTime = 65,
                deviceType = "Tablet",
                createdAt = new DateTime(2024, 8, 15, 9, 30, 0),
                status = "Completed"
            },
            new
            {
                id = "response_024",
                responseId = "response_024",
                surveyId = "survey_food_delivery_q3_2024",
                campaignId = "campaign_food_delivery_q3_2024",
                panelistId = "panelist-017",
                cohortType = "control",
                answers = new List<object>
                {
                    new { questionId = "q1_awareness", answer = "Slightly familiar", numericValue = 2.0 },
                    new { questionId = "q2_download_intent", answer = "Moderately likely", numericValue = 3.0 },
                    new { questionId = "q3_message_recall", answer = "No", numericValue = 0.0 }
                },
                completedAt = new DateTime(2024, 8, 18, 16, 45, 0),
                responseTime = 82,
                deviceType = "Mobile",
                createdAt = new DateTime(2024, 8, 18, 16, 45, 0),
                status = "Completed"
            },
            // Auto SUV - exposed
            new
            {
                id = "response_025",
                responseId = "response_025",
                surveyId = "survey_auto_suv_fall_2024",
                campaignId = "campaign_auto_suv_fall_2024",
                panelistId = "panelist-006",
                cohortType = "exposed",
                answers = new List<object>
                {
                    new { questionId = "q1_awareness", answer = "Very familiar", numericValue = 4.0 },
                    new { questionId = "q2_purchase_intent", answer = "Moderately likely", numericValue = 3.0 },
                    new { questionId = "q3_favorability", answer = "Somewhat favorable", numericValue = 4.0 },
                    new { questionId = "q4_message_recall", answer = "Yes", numericValue = 1.0 }
                },
                completedAt = new DateTime(2024, 9, 20, 10, 0, 0),
                responseTime = 155,
                deviceType = "Desktop",
                createdAt = new DateTime(2024, 9, 20, 10, 0, 0),
                status = "Completed"
            },
            new
            {
                id = "response_026",
                responseId = "response_026",
                surveyId = "survey_auto_suv_fall_2024",
                campaignId = "campaign_auto_suv_fall_2024",
                panelistId = "panelist-008",
                cohortType = "exposed",
                answers = new List<object>
                {
                    new { questionId = "q1_awareness", answer = "Extremely familiar", numericValue = 5.0 },
                    new { questionId = "q2_purchase_intent", answer = "Very likely", numericValue = 4.0 },
                    new { questionId = "q3_favorability", answer = "Very favorable", numericValue = 5.0 },
                    new { questionId = "q4_message_recall", answer = "Yes", numericValue = 1.0 }
                },
                completedAt = new DateTime(2024, 9, 22, 14, 30, 0),
                responseTime = 140,
                deviceType = "Desktop",
                createdAt = new DateTime(2024, 9, 22, 14, 30, 0),
                status = "Completed"
            },
            // Auto SUV - control
            new
            {
                id = "response_027",
                responseId = "response_027",
                surveyId = "survey_auto_suv_fall_2024",
                campaignId = "campaign_auto_suv_fall_2024",
                panelistId = "panelist-004",
                cohortType = "control",
                answers = new List<object>
                {
                    new { questionId = "q1_awareness", answer = "Slightly familiar", numericValue = 2.0 },
                    new { questionId = "q2_purchase_intent", answer = "Not at all likely", numericValue = 1.0 },
                    new { questionId = "q3_favorability", answer = "Neutral", numericValue = 3.0 },
                    new { questionId = "q4_message_recall", answer = "No", numericValue = 0.0 }
                },
                completedAt = new DateTime(2024, 9, 25, 11, 15, 0),
                responseTime = 125,
                deviceType = "Desktop",
                createdAt = new DateTime(2024, 9, 25, 11, 15, 0),
                status = "Completed"
            },
            new
            {
                id = "response_028",
                responseId = "response_028",
                surveyId = "survey_auto_suv_fall_2024",
                campaignId = "campaign_auto_suv_fall_2024",
                panelistId = "panelist-010",
                cohortType = "control",
                answers = new List<object>
                {
                    new { questionId = "q1_awareness", answer = "Moderately familiar", numericValue = 3.0 },
                    new { questionId = "q2_purchase_intent", answer = "Slightly likely", numericValue = 2.0 },
                    new { questionId = "q3_favorability", answer = "Neutral", numericValue = 3.0 },
                    new { questionId = "q4_message_recall", answer = "No", numericValue = 0.0 }
                },
                completedAt = new DateTime(2024, 9, 28, 16, 0, 0),
                responseTime = 130,
                deviceType = "Desktop",
                createdAt = new DateTime(2024, 9, 28, 16, 0, 0),
                status = "Completed"
            },
            // Tech Smartphone - exposed
            new
            {
                id = "response_029",
                responseId = "response_029",
                surveyId = "survey_tech_smartphone_holiday_2024",
                campaignId = "campaign_tech_smartphone_holiday_2024",
                panelistId = "panelist-001",
                cohortType = "exposed",
                answers = new List<object>
                {
                    new { questionId = "q1_awareness", answer = "Very familiar", numericValue = 4.0 },
                    new { questionId = "q2_purchase_intent", answer = "Very likely", numericValue = 4.0 },
                    new { questionId = "q3_feature_perception", answer = "8", numericValue = 8.0 },
                    new { questionId = "q4_message_recall", answer = "Yes", numericValue = 1.0 }
                },
                completedAt = new DateTime(2024, 11, 20, 18, 30, 0),
                responseTime = 150,
                deviceType = "Mobile",
                createdAt = new DateTime(2024, 11, 20, 18, 30, 0),
                status = "Completed"
            },
            new
            {
                id = "response_030",
                responseId = "response_030",
                surveyId = "survey_tech_smartphone_holiday_2024",
                campaignId = "campaign_tech_smartphone_holiday_2024",
                panelistId = "panelist-002",
                cohortType = "exposed",
                answers = new List<object>
                {
                    new { questionId = "q1_awareness", answer = "Extremely familiar", numericValue = 5.0 },
                    new { questionId = "q2_purchase_intent", answer = "Extremely likely", numericValue = 5.0 },
                    new { questionId = "q3_feature_perception", answer = "9", numericValue = 9.0 },
                    new { questionId = "q4_message_recall", answer = "Yes", numericValue = 1.0 }
                },
                completedAt = new DateTime(2024, 11, 22, 10, 0, 0),
                responseTime = 120,
                deviceType = "Desktop",
                createdAt = new DateTime(2024, 11, 22, 10, 0, 0),
                status = "Completed"
            },
            // Tech Smartphone - control
            new
            {
                id = "response_031",
                responseId = "response_031",
                surveyId = "survey_tech_smartphone_holiday_2024",
                campaignId = "campaign_tech_smartphone_holiday_2024",
                panelistId = "panelist-019",
                cohortType = "control",
                answers = new List<object>
                {
                    new { questionId = "q1_awareness", answer = "Slightly familiar", numericValue = 2.0 },
                    new { questionId = "q2_purchase_intent", answer = "Slightly likely", numericValue = 2.0 },
                    new { questionId = "q3_feature_perception", answer = "5", numericValue = 5.0 },
                    new { questionId = "q4_message_recall", answer = "No", numericValue = 0.0 }
                },
                completedAt = new DateTime(2024, 11, 25, 14, 0, 0),
                responseTime = 95,
                deviceType = "Mobile",
                createdAt = new DateTime(2024, 11, 25, 14, 0, 0),
                status = "Completed"
            },
            new
            {
                id = "response_032",
                responseId = "response_032",
                surveyId = "survey_tech_smartphone_holiday_2024",
                campaignId = "campaign_tech_smartphone_holiday_2024",
                panelistId = "panelist-017",
                cohortType = "control",
                answers = new List<object>
                {
                    new { questionId = "q1_awareness", answer = "Not at all familiar", numericValue = 1.0 },
                    new { questionId = "q2_purchase_intent", answer = "Not at all likely", numericValue = 1.0 },
                    new { questionId = "q3_feature_perception", answer = "3", numericValue = 3.0 },
                    new { questionId = "q4_message_recall", answer = "No", numericValue = 0.0 }
                },
                completedAt = new DateTime(2024, 11, 28, 9, 30, 0),
                responseTime = 80,
                deviceType = "Mobile",
                createdAt = new DateTime(2024, 11, 28, 9, 30, 0),
                status = "Completed"
            },
            // Insurance Home - exposed
            new
            {
                id = "response_033",
                responseId = "response_033",
                surveyId = "survey_insurance_home_q4_2024",
                campaignId = "campaign_insurance_home_q4_2024",
                panelistId = "panelist-007",
                cohortType = "exposed",
                answers = new List<object>
                {
                    new { questionId = "q1_awareness", answer = "Very familiar", numericValue = 4.0 },
                    new { questionId = "q2_quote_intent", answer = "Moderately likely", numericValue = 3.0 },
                    new { questionId = "q3_trust", answer = "7", numericValue = 7.0 }
                },
                completedAt = new DateTime(2024, 10, 20, 11, 0, 0),
                responseTime = 110,
                deviceType = "Mobile",
                createdAt = new DateTime(2024, 10, 20, 11, 0, 0),
                status = "Completed"
            },
            new
            {
                id = "response_034",
                responseId = "response_034",
                surveyId = "survey_insurance_home_q4_2024",
                campaignId = "campaign_insurance_home_q4_2024",
                panelistId = "panelist-006",
                cohortType = "exposed",
                answers = new List<object>
                {
                    new { questionId = "q1_awareness", answer = "Extremely familiar", numericValue = 5.0 },
                    new { questionId = "q2_quote_intent", answer = "Very likely", numericValue = 4.0 },
                    new { questionId = "q3_trust", answer = "8", numericValue = 8.0 }
                },
                completedAt = new DateTime(2024, 10, 22, 15, 30, 0),
                responseTime = 100,
                deviceType = "Desktop",
                createdAt = new DateTime(2024, 10, 22, 15, 30, 0),
                status = "Completed"
            },
            // Insurance Home - control
            new
            {
                id = "response_035",
                responseId = "response_035",
                surveyId = "survey_insurance_home_q4_2024",
                campaignId = "campaign_insurance_home_q4_2024",
                panelistId = "panelist-003",
                cohortType = "control",
                answers = new List<object>
                {
                    new { questionId = "q1_awareness", answer = "Not at all familiar", numericValue = 1.0 },
                    new { questionId = "q2_quote_intent", answer = "Not at all likely", numericValue = 1.0 },
                    new { questionId = "q3_trust", answer = "3", numericValue = 3.0 }
                },
                completedAt = new DateTime(2024, 10, 25, 10, 0, 0),
                responseTime = 70,
                deviceType = "Mobile",
                createdAt = new DateTime(2024, 10, 25, 10, 0, 0),
                status = "Completed"
            },
            new
            {
                id = "response_036",
                responseId = "response_036",
                surveyId = "survey_insurance_home_q4_2024",
                campaignId = "campaign_insurance_home_q4_2024",
                panelistId = "panelist-009",
                cohortType = "control",
                answers = new List<object>
                {
                    new { questionId = "q1_awareness", answer = "Slightly familiar", numericValue = 2.0 },
                    new { questionId = "q2_quote_intent", answer = "Slightly likely", numericValue = 2.0 },
                    new { questionId = "q3_trust", answer = "4", numericValue = 4.0 }
                },
                completedAt = new DateTime(2024, 10, 28, 13, 45, 0),
                responseTime = 88,
                deviceType = "Tablet",
                createdAt = new DateTime(2024, 10, 28, 13, 45, 0),
                status = "Completed"
            },
            // Fitness Gym - exposed
            new
            {
                id = "response_037",
                responseId = "response_037",
                surveyId = "survey_fitness_gym_new_year_2025",
                campaignId = "campaign_fitness_gym_new_year_2025",
                panelistId = "panelist-002",
                cohortType = "exposed",
                answers = new List<object>
                {
                    new { questionId = "q1_awareness", answer = "Very familiar", numericValue = 4.0 },
                    new { questionId = "q2_membership_intent", answer = "Very likely", numericValue = 4.0 },
                    new { questionId = "q3_message_recall", answer = "Yes", numericValue = 1.0 },
                    new { questionId = "q4_motivation", answer = "8", numericValue = 8.0 }
                },
                completedAt = new DateTime(2025, 1, 2, 10, 0, 0),
                responseTime = 95,
                deviceType = "Desktop",
                createdAt = new DateTime(2025, 1, 2, 10, 0, 0),
                status = "Completed"
            },
            new
            {
                id = "response_038",
                responseId = "response_038",
                surveyId = "survey_fitness_gym_new_year_2025",
                campaignId = "campaign_fitness_gym_new_year_2025",
                panelistId = "panelist-005",
                cohortType = "exposed",
                answers = new List<object>
                {
                    new { questionId = "q1_awareness", answer = "Extremely familiar", numericValue = 5.0 },
                    new { questionId = "q2_membership_intent", answer = "Extremely likely", numericValue = 5.0 },
                    new { questionId = "q3_message_recall", answer = "Yes", numericValue = 1.0 },
                    new { questionId = "q4_motivation", answer = "9", numericValue = 9.0 }
                },
                completedAt = new DateTime(2025, 1, 5, 16, 15, 0),
                responseTime = 85,
                deviceType = "Tablet",
                createdAt = new DateTime(2025, 1, 5, 16, 15, 0),
                status = "Completed"
            },
            // Fitness Gym - control
            new
            {
                id = "response_039",
                responseId = "response_039",
                surveyId = "survey_fitness_gym_new_year_2025",
                campaignId = "campaign_fitness_gym_new_year_2025",
                panelistId = "panelist-019",
                cohortType = "control",
                answers = new List<object>
                {
                    new { questionId = "q1_awareness", answer = "Slightly familiar", numericValue = 2.0 },
                    new { questionId = "q2_membership_intent", answer = "Slightly likely", numericValue = 2.0 },
                    new { questionId = "q3_message_recall", answer = "No", numericValue = 0.0 },
                    new { questionId = "q4_motivation", answer = "4", numericValue = 4.0 }
                },
                completedAt = new DateTime(2025, 1, 2, 10, 30, 0),
                responseTime = 72,
                deviceType = "Mobile",
                createdAt = new DateTime(2025, 1, 2, 10, 30, 0),
                status = "Completed"
            },
            new
            {
                id = "response_040",
                responseId = "response_040",
                surveyId = "survey_fitness_gym_new_year_2025",
                campaignId = "campaign_fitness_gym_new_year_2025",
                panelistId = "panelist-011",
                cohortType = "control",
                answers = new List<object>
                {
                    new { questionId = "q1_awareness", answer = "Not at all familiar", numericValue = 1.0 },
                    new { questionId = "q2_membership_intent", answer = "Not at all likely", numericValue = 1.0 },
                    new { questionId = "q3_message_recall", answer = "No", numericValue = 0.0 },
                    new { questionId = "q4_motivation", answer = "2", numericValue = 2.0 }
                },
                completedAt = new DateTime(2025, 1, 5, 16, 15, 0),
                responseTime = 68,
                deviceType = "Mobile",
                createdAt = new DateTime(2025, 1, 5, 16, 15, 0),
                status = "Completed"
            }
        };

        // Generate additional responses from new panelists (panelist-021 through panelist-100)
        var surveyConfigs = new[]
        {
            new { SurveyId = "survey_summer_beverage_2024", CampaignId = "campaign_summer_beverage_2024",
                  Questions = new[] { "q1_awareness", "q2_purchase_intent", "q3_favorability", "q4_message_recall", "q5_consideration" } },
            new { SurveyId = "survey_retail_back_to_school_2024", CampaignId = "campaign_retail_back_to_school_2024",
                  Questions = new[] { "q1_awareness", "q2_purchase_intent", "q3_message_recall" } },
            new { SurveyId = "survey_pharma_allergy_spring_2024", CampaignId = "campaign_pharma_allergy_spring_2024",
                  Questions = new[] { "q1_awareness", "q2_purchase_intent", "q3_favorability", "q4_message_recall" } },
            new { SurveyId = "survey_finance_credit_card_q2_2024", CampaignId = "campaign_finance_credit_card_q2_2024",
                  Questions = new[] { "q1_awareness", "q2_application_intent", "q3_value_perception" } },
            new { SurveyId = "survey_travel_cruise_summer_2024", CampaignId = "campaign_travel_cruise_summer_2024",
                  Questions = new[] { "q1_awareness", "q2_booking_intent", "q3_luxury_perception" } },
            new { SurveyId = "survey_food_delivery_q3_2024", CampaignId = "campaign_food_delivery_q3_2024",
                  Questions = new[] { "q1_awareness", "q2_download_intent", "q3_message_recall" } },
            new { SurveyId = "survey_auto_suv_fall_2024", CampaignId = "campaign_auto_suv_fall_2024",
                  Questions = new[] { "q1_awareness", "q2_purchase_intent", "q3_favorability", "q4_message_recall" } },
            new { SurveyId = "survey_tech_smartphone_holiday_2024", CampaignId = "campaign_tech_smartphone_holiday_2024",
                  Questions = new[] { "q1_awareness", "q2_purchase_intent", "q3_feature_perception", "q4_message_recall" } },
            new { SurveyId = "survey_insurance_home_q4_2024", CampaignId = "campaign_insurance_home_q4_2024",
                  Questions = new[] { "q1_awareness", "q2_quote_intent", "q3_trust" } },
            new { SurveyId = "survey_fitness_gym_new_year_2025", CampaignId = "campaign_fitness_gym_new_year_2025",
                  Questions = new[] { "q1_awareness", "q2_membership_intent", "q3_message_recall", "q4_motivation" } }
        };

        var likertAnswers = new[] { "Not at all familiar", "Slightly familiar", "Moderately familiar", "Very familiar", "Extremely familiar" };
        var devices = new[] { "Mobile", "Desktop", "Tablet" };
        var cohorts = new[] { "exposed", "control" };
        var rng = new Random(99);
        var responseIdx = 41;

        // Assign ~6 additional responses per survey (3 exposed, 3 control) from new panelists
        for (int si = 0; si < surveyConfigs.Length; si++)
        {
            var cfg = surveyConfigs[si];
            var basePanelist = 21 + si * 8;

            for (int r = 0; r < 6; r++)
            {
                var panelistNum = basePanelist + r;
                if (panelistNum > 100) panelistNum = 21 + (panelistNum % 80);
                var panelistId = $"panelist-{panelistNum:D3}";
                var cohort = r < 3 ? "exposed" : "control";
                var baseScore = cohort == "exposed" ? rng.Next(3, 6) : rng.Next(1, 4);

                var answers = new List<object>();
                foreach (var qId in cfg.Questions)
                {
                    var score = Math.Clamp(baseScore + rng.Next(-1, 2), 1, 5);
                    if (qId.Contains("recall") || qId.Contains("message_recall"))
                    {
                        var recalled = cohort == "exposed" ? rng.NextDouble() > 0.3 : rng.NextDouble() > 0.7;
                        answers.Add(new { questionId = qId, answer = recalled ? "Yes" : "No", numericValue = recalled ? 1.0 : 0.0 });
                    }
                    else if (qId.Contains("consideration") || qId.Contains("perception") || qId.Contains("trust") || qId.Contains("motivation"))
                    {
                        var ratingVal = cohort == "exposed" ? rng.Next(6, 10) : rng.Next(2, 7);
                        answers.Add(new { questionId = qId, answer = ratingVal.ToString(), numericValue = (double)ratingVal });
                    }
                    else
                    {
                        answers.Add(new { questionId = qId, answer = likertAnswers[score - 1], numericValue = (double)score });
                    }
                }

                var respId = $"response_{responseIdx:D3}";
                var daysAgo = rng.Next(5, 60);
                sampleResponses.Add(new
                {
                    id = respId,
                    responseId = respId,
                    surveyId = cfg.SurveyId,
                    campaignId = cfg.CampaignId,
                    panelistId = panelistId,
                    cohortType = cohort,
                    answers = answers,
                    completedAt = DateTime.UtcNow.AddDays(-daysAgo),
                    responseTime = rng.Next(60, 250),
                    deviceType = devices[rng.Next(devices.Length)],
                    createdAt = DateTime.UtcNow.AddDays(-daysAgo),
                    status = "Completed"
                });
                responseIdx++;
            }
        }

        foreach (var response in sampleResponses)
        {
            try
            {
                dynamic r = response;
                string responseId = r.responseId;
                await responseContainer.CreateItemAsync(response, new PartitionKey(responseId));
                _logger.LogInformation("Seeded survey response: {ResponseId}", responseId);
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                dynamic r = response;
                string responseId = r.responseId;
                _logger.LogInformation("Survey response already exists: {ResponseId}", responseId);
            }
        }

        _logger.LogInformation("Sample survey response data seeding completed - {Count} responses seeded", sampleResponses.Count);
    }

    private async Task CreateSurveyContainerAsync(Database database, string containerName)
    {
        var containerProperties = new ContainerProperties
        {
            Id = containerName,
            PartitionKeyPath = "/surveyId",
            IndexingPolicy = new IndexingPolicy
            {
                Automatic = true,
                IndexingMode = IndexingMode.Consistent,
                IncludedPaths = { new IncludedPath { Path = "/*" } },
                ExcludedPaths = { new ExcludedPath { Path = "/\"_etag\"/?" } },
                CompositeIndexes =
                {
                    new Collection<CompositePath>
                    {
                        new CompositePath { Path = "/campaignId", Order = CompositePathSortOrder.Ascending },
                        new CompositePath { Path = "/createdAt", Order = CompositePathSortOrder.Descending }
                    },
                    new Collection<CompositePath>
                    {
                        new CompositePath { Path = "/status", Order = CompositePathSortOrder.Ascending },
                        new CompositePath { Path = "/distributionStartDate", Order = CompositePathSortOrder.Ascending }
                    }
                }
            }
        };

        _logger.LogInformation("Creating container: {ContainerName}", containerName);
        var containerResponse = await database.CreateContainerIfNotExistsAsync(containerProperties);

        if (containerResponse.StatusCode == System.Net.HttpStatusCode.Created)
        {
            _logger.LogInformation("Container created: {ContainerName}", containerName);
        }
        else
        {
            _logger.LogInformation("Container already exists: {ContainerName}", containerName);
        }
    }

    private async Task CreateResponseContainerAsync(Database database, string containerName)
    {
        var containerProperties = new ContainerProperties
        {
            Id = containerName,
            PartitionKeyPath = "/responseId",
            IndexingPolicy = new IndexingPolicy
            {
                Automatic = true,
                IndexingMode = IndexingMode.Consistent,
                IncludedPaths = { new IncludedPath { Path = "/*" } },
                ExcludedPaths = { new ExcludedPath { Path = "/\"_etag\"/?" } },
                CompositeIndexes =
                {
                    new Collection<CompositePath>
                    {
                        new CompositePath { Path = "/surveyId", Order = CompositePathSortOrder.Ascending },
                        new CompositePath { Path = "/completedAt", Order = CompositePathSortOrder.Descending }
                    },
                    new Collection<CompositePath>
                    {
                        new CompositePath { Path = "/panelistId", Order = CompositePathSortOrder.Ascending },
                        new CompositePath { Path = "/campaignId", Order = CompositePathSortOrder.Ascending }
                    }
                }
            }
        };

        _logger.LogInformation("Creating container: {ContainerName}", containerName);
        var containerResponse = await database.CreateContainerIfNotExistsAsync(containerProperties);

        if (containerResponse.StatusCode == System.Net.HttpStatusCode.Created)
        {
            _logger.LogInformation("Container created: {ContainerName}", containerName);
        }
        else
        {
            _logger.LogInformation("Container already exists: {ContainerName}", containerName);
        }
    }
}
