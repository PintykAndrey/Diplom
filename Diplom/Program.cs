using Microsoft.EntityFrameworkCore;

using Diplom.Data;

using Diplom.Localization;

using Microsoft.Extensions.Localization;

using Diplom.Models.Tools;

using Microsoft.AspNetCore.Localization;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Diplom.Services.Email;
using Diplom.Services.Identity;
using Diplom.Models.Identity;


var builder = WebApplication.CreateBuilder(args);


builder.Services.AddDbContext<ApplicationDbContext>(options =>

    options.UseNpgsql(

        builder.Configuration.GetConnectionString("DefaultConnection")

    )

);


builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AuthorizeFilter());
})
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();

builder.Services.AddLocalization();


builder.Services.AddSingleton<VocabularyCache>();


builder.Services.AddScoped<DbVocabularyStringLocalizer>();


builder.Services.AddHttpContextAccessor();


builder.Services.AddSingleton<IStringLocalizerFactory,

    DbVocabularyStringLocalizerFactory>();


builder.Services.AddScoped<IStringLocalizer>(sp =>

    sp.GetRequiredService<DbVocabularyStringLocalizer>());

builder.Services.Configure<SmtpEmailSettings>(
    builder.Configuration.GetSection("Smtp"));

builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();


builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{

    options.Password.RequireDigit = false;

    options.Password.RequiredLength = 6;

    options.Password.RequireNonAlphanumeric = false;

    options.Password.RequireUppercase = false;

    options.Password.RequireLowercase = false;

    options.User.RequireUniqueEmail = true;

    options.SignIn.RequireConfirmedEmail = true;

})

.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, ApplicationUserClaimsPrincipalFactory>();

builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
{
    options.TokenLifespan = TimeSpan.FromMinutes(5);
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/Login";
});


builder.Services.AddDistributedMemoryCache();


builder.Services.AddSession(options =>

{

    options.IdleTimeout = TimeSpan.FromMinutes(60);

    options.Cookie.HttpOnly = true;

    options.Cookie.IsEssential = true;

});


var app = builder.Build();


using (var scope = app.Services.CreateScope())

{

    var context =

        scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    context.Database.Migrate();


    var cache =

        scope.ServiceProvider.GetRequiredService<VocabularyCache>();


    cache.Load(context);

}


var supportedCultures = new[] { "ru", "uk", "bg", "en" };

var localizationOptions = new RequestLocalizationOptions()

    .SetDefaultCulture("en")
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);


localizationOptions.RequestCultureProviders =
[
    new QueryStringRequestCultureProvider(),
    new CookieRequestCultureProvider()
];

app.UseRequestLocalization(localizationOptions);


using (var scope = app.Services.CreateScope())

{

    var context =

        scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    var oldSowing = context.EncyclopediaItems
        .Where(e => e.Category == "Operation" && e.Name == "Посев")
        .ToList();

    foreach (var item in oldSowing)
    {
        item.Name = "Sowing";
    }

    var users = context.Users.ToList();

    foreach (var user in users)
    {
        var hasSowing = context.EncyclopediaItems.Any(e =>
            e.OwnerUserId == user.Id &&
            e.Category == "Operation" &&
            e.Name == "Sowing");

        if (!hasSowing)
        {
            context.EncyclopediaItems.Add(new EncyclopediaItem
            {
                OwnerUserId = user.Id,
                Category = "Operation",
                Name = "Sowing"
            });
        }

        var duplicateSowing = context.EncyclopediaItems
            .Where(e =>
                e.OwnerUserId == user.Id &&
                e.Category == "Operation" &&
                e.Name == "Sowing")
            .OrderBy(e => e.Id)
            .Skip(1)
            .ToList();

        if (duplicateSowing.Any())
        {
            context.EncyclopediaItems.RemoveRange(duplicateSowing);
        }
    }

    context.SaveChanges();

}


if (!app.Environment.IsDevelopment())

{

    app.UseExceptionHandler("/Home/Error");

    app.UseHsts();

}

app.UseStaticFiles();


app.UseRouting();


app.UseAuthentication();

app.UseSession();

app.UseAuthorization();


app.MapControllerRoute(

    name: "default",

    pattern: "{controller=Home}/{action=Index}/{id?}"

);
    
app.MapControllerRoute(

    name: "default",

    pattern: "{controller=Fields}/{action=Index}/{id?}"

);

app.MapControllerRoute(

    name: "default", 

    pattern: "{controller=Fields}/{action=CropRotation}/{id?}"

);

app.MapControllerRoute(

    name: "default",

    pattern: "{controller=Fields}/{action=FieldSituationLog}/{id?}"

);

app.MapControllerRoute(

    name: "fieldworklog",

    pattern: "{controller=Fields}/{action=FieldWorkLog}/{id?}"

);

app.MapControllerRoute(

    name: "saveworklog",

    pattern: "Fields/FieldWorkLog/SaveWorkLog",

    defaults: new { controller = "FieldWorkLog", action = "SaveWorkLog" }

);

app.MapControllerRoute(

    name: "default",

    pattern: "{controller=Fields}/{action=FieldsJournal}/{id?}"

);

app.MapControllerRoute(

    name: "default",

    pattern: "{controller=Tools}/{action=Index}/{id?}"

);

app.MapControllerRoute(

    name: "default",

    pattern: "{controller=Tools}/{action=Archive}/{id?}"

);

app.MapControllerRoute(

    name: "default",

    pattern: "{controller=Tools}/{action=Vocabulary}/{id?}"

);

app.MapControllerRoute(

    name: "default",

    pattern: "{controller=Equipment}/{action=Index}/{id?}"

);

app.MapControllerRoute(

    name: "equipmentjournal",
    pattern: "EquipmentJournal/{action=Index}/{id?}",
    defaults: new { controller = "EquipmentJournal" }

);

app.MapControllerRoute(

    name: "maintenance",

    pattern: "Equipment/Maintenance/{id?}",

    defaults: new { controller = "Equipment", action = "Maintenance" }

);

app.MapControllerRoute(

    name: "default",

    pattern: "{controller=Navigation}/{action=Index}/{id?}"

);

app.MapControllerRoute(

    name: "default",

    pattern: "{controller=Navigation}/{action=Equipment}/{id?}"

);

app.MapControllerRoute(

    name: "default",

    pattern: "{controller=Navigation}/{action=Maintenance}/{id?}"

);

app.MapControllerRoute(

    name: "default",

    pattern: "{controller=Navigation}/{action=Tools}/{id?}"

);

app.MapControllerRoute(

    name: "default",

    pattern: "{controller=Navigation}/{action=CropRotation}/{id?}"

);

app.MapControllerRoute(

    name: "default",

    pattern: "{controller=Navigation}/{action=Fields}/{id?}"

);

app.MapControllerRoute(

    name: "default",

    pattern: "{controller=Navigation}/{action=Warehouses}/{id?}"

);

app.MapControllerRoute(

    name: "default",

    pattern: "{controller=Warehouses}/{action=Index}/{id?}"

);

app.MapControllerRoute(

    name: "default",

    pattern: "{controller=Warehouses}/{action=Pesticides}/{id?}"

);

app.MapControllerRoute(

    name: "default",

    pattern: "{controller=Warehouses}/{action=Fertilizers}/{id?}"

);

app.MapControllerRoute(

    name: "default",

    pattern: "{controller=Warehouses}/{action=Seeds}/{id?}"

);

app.MapControllerRoute(

    name: "default",

    pattern: "{controller=Warehouses}/{action=FuelLubricants}/{id?}"

);

app.MapControllerRoute(

    name: "default",

    pattern: "{controller=InventoryHistory}/{action=Index}/{id?}"

);

app.MapControllerRoute(

    name: "default",

    pattern: "{controller=MaterialLog}/{action=Index}/{id?}"

);

app.MapControllerRoute(

    name: "default",

    pattern: "{controller=MaterialLog}/{action=Inventory}/{id?}"

);

app.MapControllerRoute(

    name: "default",

    pattern: "{controller=MaterialLog}/{action=InventoryHistory}/{id?}"

);

app.MapControllerRoute(

    name: "default",

    pattern: "{controller=MaterialLog}/{action=Pesticides}/{id?}"

);

app.MapControllerRoute(

    name: "default",

    pattern: "{controller=MaterialLog}/{action=Fertilizers}/{id?}"

);

app.MapControllerRoute(

    name: "default",

    pattern: "{controller=MaterialLog}/{action=Seeds}/{id?}"

);

app.MapControllerRoute(

    name: "default",

    pattern: "{controller=MaterialLog}/{action=FuelLubricants}/{id?}"

);

app.MapControllerRoute(

    name: "api",

    pattern: "api/{controller}/{action}/{id?}"

);


app.Run();