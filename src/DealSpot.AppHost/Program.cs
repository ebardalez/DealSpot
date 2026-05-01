var builder = DistributedApplication.CreateBuilder(args);

// Pin the password so it never changes between runs.
// Store the actual value in AppHost user secrets:
//   dotnet user-secrets set "Parameters:pg-password" "your-password" --project src/DealSpot.AppHost
var pgPassword = builder.AddParameter("pg-password", secret: true);

// PostgreSQL container with persistent named volume - data survives restarts
var postgres = builder.AddPostgres("pg", password: pgPassword)
    .WithPgAdmin()
    .WithDataVolume("dealspot-pg-data")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithHostPort(5432);

var db = postgres.AddDatabase("dealspot");

// Redis for output cache - persistent so cached data survives restarts
var redis = builder.AddRedis("redis")
    .WithDataVolume("dealspot-redis-data")
    .WithLifetime(ContainerLifetime.Persistent);

// DealSpot web app
builder.AddProject<Projects.DealSpot_Web>("dealspot-web")
    .WithReference(db)
    .WithReference(redis)
    .WaitFor(db)
    .WaitFor(redis);

builder.Build().Run();

