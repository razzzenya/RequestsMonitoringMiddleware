var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("cache")
    .WithRedisCommander();

var openSearch = builder.AddContainer("opensearch", "opensearchproject/opensearch", "2.12.0")
    .WithHttpEndpoint(port: 9200, targetPort: 9200, name: "http")
    .WithEnvironment("discovery.type", "single-node")
    .WithEnvironment("OPENSEARCH_INITIAL_ADMIN_PASSWORD", "Admin@123")
    .WithEnvironment("DISABLE_SECURITY_PLUGIN", "true");

var openSearchDashboard = builder.AddContainer("opensearch-dashboard", "opensearchproject/opensearch-dashboards", "2.12.0")
    .WithHttpEndpoint(port: 5601, targetPort: 5601, name: "http")
    .WithEnvironment("OPENSEARCH_HOSTS", "http://opensearch:9200")
    .WithEnvironment("DISABLE_SECURITY_DASHBOARDS_PLUGIN", "true")
    .WaitFor(openSearch);

var api = builder.AddProject<Projects.RequestMonitoring_Test_Api>("api")
    .WaitFor(openSearch)
    .WaitFor(redis)
    .WithReference(redis)
    .WithEnvironment("OpenSearch__Uri", "http://localhost:9200")
    .WithEnvironment("OpenSearch__Index", "request-logs");

builder.Build().Run();
