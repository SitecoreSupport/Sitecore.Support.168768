define(["sitecore"],
    function(Sitecore) {
        Sitecore.Factories.createBaseComponent({
            name: "DeployMarketingDefinitions",
            base: "ControlBase",
            selector: ".sc-DeployMarketingDefinitions",
            attributes: [
                { name: "isBusy", defaultValue: false },
                { name: "deploymentComplete", defaultValue: false },
                { name: "deploymentError", defaultValue: false }
            ],

            extendModel: {
    
            },

            initialize: function() {
                var renderingId = this.model.get("name"),
                    deployButton = this.app[renderingId + "btnDeploy"];

                deployButton.on("click",
                    function() {
                        this.deployDefinitions();
                    },
                    this);
            },

            afterRender: function() {

            },

            deployDefinitions: function() {
                var renderingId = this.model.get("name");
                var self = this;
                var definitionTypes = [];

                var automationPlans = this.app[renderingId + "cbAutomationPlans"].get("isChecked");
                if (automationPlans)
                    definitionTypes.push("automationplans");

                var campaigns = this.app[renderingId + "cbCampaigns"].get("isChecked");
                if (campaigns)
                    definitionTypes.push("campaigns");

                var events = this.app[renderingId + "cbEvents"].get("isChecked");
                if (events)
                    definitionTypes.push("events");

                var funnels = this.app[renderingId + "cbFunnels"].get("isChecked");
                if (funnels)
                    definitionTypes.push("funnels");

                var goals = this.app[renderingId + "cbGoals"].get("isChecked");
                if (goals)
                    definitionTypes.push("goals");

                var assets = this.app[renderingId + "cbMarketingAssets"].get("isChecked");
                if (assets)
                    definitionTypes.push("marketingassets");

                var pageevents = this.app[renderingId + "cbPageEvents"].get("isChecked");
                if (pageevents)
                    definitionTypes.push("pageevents");

                var outcomes = this.app[renderingId + "cbOutcomes"].get("isChecked");
                if (outcomes)
                    definitionTypes.push("outcomes");

                var profiles = this.app[renderingId + "cbProfiles"].get("isChecked");
                if (profiles)
                    definitionTypes.push("profiles");

                var segments = this.app[renderingId + "cbSegments"].get("isChecked");
                if (segments)
                    definitionTypes.push("segments");

                var publishTaxonomies = this.app[renderingId + "cbTaxonomies"].get("isChecked");

                var showDeploymentComplete = function () {
                    self.model.set("deploymentComplete", true);
                    self.model.set("isBusy", false);
                }

                var showDeploymentError = function () {
                    self.model.set("deploymentError", true);
                    self.model.set("isBusy", false);
                }

                jQuery.ajax({
                    type: "POST",
                    url: "/api/sitecore/DeployMarketingDefinitions/DeployDefinitions",
                    data: {
                        "definitionTypes": JSON.stringify(definitionTypes),
                        "publishTaxonomies":
                            publishTaxonomies
                    },
                    beforeSend: function() {
                        self.model.set("isBusy", true);
                    },
                    success: function (result) {
                        if (result.success) {
                            if (publishTaxonomies) {
                                if (result.jobName) {
                                    deployRequestWithRetries(result.jobName);
                                } else {
                                    showDeploymentError();
                                }
                            } else {
                                showDeploymentComplete();
                            }
                        } else {
                            showDeploymentError();
                        }
                    },
                    error: function() {
                        showDeploymentError();
                    },
                });

                var deployRequestWithRetries = function(jobName) {
                    jQuery.ajax({
                        type: "POST",
                        url: "/api/sitecore/DeployMarketingDefinitions/GetDeployDefinitionsJobStatus",
                        data: { "jobName": jobName },
                        success: function(result) {
                            if (result.completed === true) {
                                showDeploymentComplete();
                            } else {
                                self.model.set("isBusy", true);
                                setTimeout(function() { deployRequestWithRetries(jobName); }, 1000);
                            }
                        },
                        error: function() {
                            showDeploymentError();
                        }
                    });
                };
            }
        });
    });