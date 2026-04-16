#:sdk Aspire.AppHost.Sdk@13.2.1
#:package Aspire.Hosting.Foundry@13.2.1-preview.1.26180.6
#:package Aspire.Hosting.Python@13.2.1

using Aspire.Hosting.Foundry;
using Azure.Provisioning.Authorization;
using Azure.Provisioning.CognitiveServices;
using Azure.Provisioning.Expressions;

var builder = DistributedApplication.CreateBuilder(args);

var tenantId = builder.AddParameterFromConfiguration("tenant", "Azure:TenantId");

var foundry = builder.AddFoundry("aif-globalazure");
var project = foundry.AddProject("proj-globalazure");
var chat = project.AddModelDeployment("chat", FoundryModel.OpenAI.Gpt41);

// workaround for https://github.com/microsoft/aspire/issues/15971
project.ConfigureInfrastructure(infra =>
    {
        var project = infra.GetProvisionableResources().OfType<CognitiveServicesProject>().Single();

        var foundryAccount = (CognitiveServicesAccount)foundry.Resource.AddAsExistingResource(infra);

        var cogUserRa = foundryAccount.CreateRoleAssignment(CognitiveServicesBuiltInRole.CognitiveServicesUser, RoleManagementPrincipalType.ServicePrincipal, project.Identity.PrincipalId);
        // There's a bug in the CDK, see https://github.com/Azure/azure-sdk-for-net/issues/47265
        cogUserRa.Name = BicepFunction.CreateGuid(foundryAccount.Id, project.Id, cogUserRa.RoleDefinitionId);
        infra.Add(cogUserRa);
    });

// project.AddAndPublishPromptAgent(
//     chat,
//     "joker-agent",
//     instructions: "You are good at telling jokes.");

var app = builder.AddPythonApp("weather-agent", "./app", "main.py")
    .WithUv()
    .WithReference(project).WaitFor(project)
    .WithReference(chat).WaitFor(chat)
    .WithEnvironment("AZURE_TENANT_ID", tenantId)
    .PublishAsHostedAgent(project);

builder.Build().Run();
