# Nomenclature
We should refine our language so that the concepts leap out of the screen without effort. 

## Project Names
I've written FileIt.Common for handling triggers and queue concerns as a function app, passing inputs over to the application layer in FileIt.Common.App. Therefore the term "App" is confusing as it can mean different things, the function app or the application logic. The Azure function app is an implementation choice, but that could be replaced in the future with an AWS Lambda function or a RabbitMQ artifact. We should reserve the App signifier for the application logic, but for the SDK wirings, like queues, triggers, subscriptions, and consider Host as a stronger term that could retain that meaning regardless of platform.

Common itself is misleading. Common, like Core or Shared, is a term typically used for a set of utility classes incorporated into a project or set of projects. In this solution, it refers to a set of integrations with API services, mediated by service bus queues. Therefore the more apt name would be Services.

Units of development and deployment need a distinctive namespace for easy identification for which I propose the term Module. Using it we could enforce clean principles with unit tests to look for FileIt.Module.*.App.csproj and ensure that they reference FileIt.Interface.csproj but not FileIt.Domain.csproj

Module partially replaces the term Provider, which is an imperfect term really. There may be some cases that we are our own provider or that there is no provider, such as a module dedicated to data maintenance or message caretaking. For these instances and existing use cases, I prefer the term flow because for most cases data flows in and out of the system. No one thinks first of the system, only of the data flowing through it. Module speaks to IT teams understanding it as a unit of deployment; flow speaks to the business about the path its data will take. There can be many flows within a module, depending on conditional logic and feature. This is where Vertical Slice Architecture can both bundle features devoted to one business, while segregating them for cleaner fulfillment.

The module name should be indicative enough for the line of business and its data flow needs. It is up to the development team to decide when a flow exceeds the bounds of one module and spawns a new one for that business.

Some project conventions include a company name, like CompanyName.ApplicationName.Feature.Area. For internal applications we can dispense with the CompanyName. Here is a table transforming the names currently in use to a new convention:

| Element | Taxonomic Standard | Existing Term | Suggestion|
|--------|--------|--------|--------|
| Function App Project | FileIt.Module.Name.Host | FileIt.Common | FileIt.Module.Services.Host |
| Application Logic Project | FileIt.Module.Name.App | FileIt.Common.App | FileIt.Module.Services.App |
| Application Logic Test Project | FileIt.Module.Name.App.Test | FileIt.Common.Test | FileIt.Module.Services.Test |
| Integration Test Project | FileIt.Module.Name.Integration | FileIt.Common.Integration | FileIt.Module.Services.Integration |
| Function App Project | FileIt.Module.Name.Host | FileIt.Module.SimpleFlow | FileIt.Module.SimpleFlow.Host |
| Application Logic Project | FileIt.Module.Name.App | FileIt.Module.SimpleFlow.App | FileIt.Module.SimpleFlow.App |
| Application Logic Test Project | FileIt.Module.Name.App.Test | FileIt.Module.SimpleFlow.Test | FileIt.Module.SimpleFlow.App.Test |
| Integration Test Project | FileIt.Module.Name.Integration | FileIt.Module.SimpleFlow.Integration | FileIt.Module.SimpleFlow.Integration |