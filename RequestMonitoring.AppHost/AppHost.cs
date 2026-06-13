var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("cache")
    .WithRedisCommander();

var api = builder.AddProject<Projects.RequestMonitoring_Test_Api>("api")
    .WaitFor(redis)
    .WithReference(redis);

var adminApi = builder.AddProject<Projects.RequestMonitoring_AdminApi>("adminapi")
    .WaitFor(redis)
    .WithReference(redis);

var adminPanel = builder.AddProject<Projects.RequestMonitoring_AdminPanel>("adminpanel")
    .WaitFor(adminApi);

builder.Build().Run();
