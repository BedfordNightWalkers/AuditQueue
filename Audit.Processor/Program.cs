using Audit.Infrastructure;
using Audit.Processor;

var builder = Host.CreateApplicationBuilder(args);

// Register shared Infrastructure services
builder.Services.AddInfrastructure(builder.Configuration);

// Register background worker
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();