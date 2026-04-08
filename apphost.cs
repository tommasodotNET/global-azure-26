#:sdk Aspire.AppHost.Sdk@13.2.1
#:package Aspire.Hosting.Foundry@13.2.1-preview.1.26180.6
#:package Aspire.Hosting.Python@13.2.1

using Aspire.Hosting.Foundry;

var builder = DistributedApplication.CreateBuilder(args);

var tenantId = builder.AddParameterFromConfiguration("tenant", "Azure:TenantId");

var foundry = builder.AddFoundry("aif-globalazure");
var project = foundry.AddProject("proj-globalazure");
var chat = project.AddModelDeployment("chat", FoundryModel.OpenAI.Gpt41);

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
