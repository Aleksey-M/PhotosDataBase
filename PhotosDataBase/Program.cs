using MongoDB.Bson.Serialization;
using PhotosDataBase.AppServices;
using PhotosDataBase.Data;
using PhotosDataBase.Data.Serializers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.Configure<MongoDbConfig>(builder.Configuration.GetSection("MongoDb"));

BsonSerializer.RegisterSerializer(UFraction32Serializer.Instance);
BsonSerializer.RegisterSerializer(Fraction32Serializer.Instance);
BsonSerializer.RegisterSerializer(JFIFThumbnailSerializer.Instance);

builder.Services.AddSingleton<MongoDbService>();
builder.Services.AddSingleton<ImagesReader>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
