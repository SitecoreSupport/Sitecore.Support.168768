using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Web.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Sitecore.Analytics;
using Sitecore.Data;
using Sitecore.DependencyInjection;
using Sitecore.Diagnostics;
using Sitecore.Framework.Conditions;
using Sitecore.Jobs;
using Sitecore.Marketing.Definitions.AutomationPlans.Model;
using Sitecore.Marketing.Definitions.Campaigns;
using Sitecore.Marketing.Definitions.Events;
using Sitecore.Marketing.Definitions.Funnels;
using Sitecore.Marketing.Definitions.Goals;
using Sitecore.Marketing.Definitions.MarketingAssets;
using Sitecore.Marketing.Definitions.Outcomes.Model;
using Sitecore.Marketing.Definitions.PageEvents;
using Sitecore.Marketing.Definitions.Profiles;
using Sitecore.Marketing.Definitions.Segments;
using Sitecore.Marketing.xMgmt.Pipelines.DeployDefinition;
using Sitecore.Marketing.xMgmt.ReferenceData.Observers.Activation.Taxonomy.Deployment;
using WellKnownIdentifiers = Sitecore.Marketing.Taxonomy.WellKnownIdentifiers;

namespace Sitecore.Support.Shell.Client.Applications.Marketing.Utilities.DeployMarketingDefinitions
{
    public class SupportDeployMarketingDefinitionsController : Controller
    {
        private readonly DeploymentManager _deploymentManager;

        public SupportDeployMarketingDefinitionsController()
        {
            _deploymentManager = ServiceLocator.ServiceProvider.GetService<DeploymentManager>();
            Condition.Ensures(_deploymentManager, nameof(_deploymentManager)).IsNotNull();
        }

        public Database Database => Context.Database;

        [HttpPost]
        [ActionName("DeployDefinitions")]
        public virtual async Task<ActionResult> DeployDefinitions(
            string definitionTypes,
            bool publishTaxonomies)
        {
            if (Tracker.IsActive && !Tracker.Current.CurrentPage.IsCancelled)
            {
                Tracker.Current.CurrentPage.Cancel();
            }

            var siteName = Sitecore.Client.Site.Name;

            await DeployDefinitionTypes(JsonConvert.DeserializeObject<string[]>(definitionTypes));

            var str = string.Empty;
            if (publishTaxonomies)
            {
                str = StartTaxonomiesDeploymentJob(siteName).Name;
            }

            return Json(new
            {
                success = true,
                jobName = str
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ActionName("GetDeployDefinitionsJobStatus")]
        public virtual ActionResult GetDeployDefinitionsJobStatus(string jobName)
        {
            if (Tracker.IsActive && !Tracker.Current.CurrentPage.IsCancelled)
            {
                Tracker.Current.CurrentPage.Cancel();
            }

            var job = JobManager.GetJob(jobName);
            Condition.Ensures(job, "Taxonomy deployment job was not found.").IsNotNull();

            if (!job.IsDone)
            {
                return Json(new
                {
                    completed = false
                }, JsonRequestBehavior.AllowGet);
            }

            Log.Debug("All taxonomies were deployed.");
            return Json(new
            {
                completed = true
            }, JsonRequestBehavior.AllowGet);
        }

        protected virtual async Task DeployDefinitionTypes(string[] definitionTypes)
        {
            var culture = CultureInfo.InvariantCulture;
            foreach (var definitionType in definitionTypes)
            {
                Log.Debug($"Deploying definition type '{definitionType}'");
                switch (definitionType.ToLowerInvariant())
                {
                    case "automationplans":
                        await _deploymentManager.DeployAllAsync<IAutomationPlanDefinition>(culture);
                        break;
                    case "campaigns":
                        await _deploymentManager.DeployAllAsync<ICampaignActivityDefinition>(culture);
                        break;
                    case "events":
                        await _deploymentManager.DeployAllAsync<IEventDefinition>(culture);
                        break;
                    case "funnels":
                        await _deploymentManager.DeployAllAsync<IFunnelDefinition>(culture);
                        break;
                    case "goals":
                        await _deploymentManager.DeployAllAsync<IGoalDefinition>(culture);
                        break;
                    case "marketingassets":
                        await _deploymentManager.DeployAllAsync<IMarketingAssetDefinition>(culture);
                        break;
                    case "outcomes":
                        await _deploymentManager.DeployAllAsync<IOutcomeDefinition>(culture);
                        break;
                    case "pageevents":
                        await _deploymentManager.DeployAllAsync<IPageEventDefinition>(culture);
                        break;
                    case "profiles":
                        await _deploymentManager.DeployAllAsync<IProfileDefinition>(culture);
                        break;
                    case "segments":
                        await _deploymentManager.DeployAllAsync<ISegmentDefinition>(culture);
                        break;
                }
            }
        }

        protected virtual Job StartTaxonomiesDeploymentJob()
        {
            return StartTaxonomiesDeploymentJob(Sitecore.Client.Site.Name);
        }

        protected virtual Job StartTaxonomiesDeploymentJob(string siteName)
        {
            var service = ServiceLocator.ServiceProvider.GetService<IDeployManager>();
            var options = new JobOptions($"Deploy all taxonomies. Deployment job id: {Guid.NewGuid()}.",
                "Sitecore.Marketing.Client", siteName, service, "Deploy", new object[1]
                {
                    WellKnownIdentifiers.Items.Taxonomies.TaxonomyRootId
                });

            Log.Debug("Starting a job to deploy all taxonomies.");

            return JobManager.Start(options);
        }
    }
}