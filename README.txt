TasteAtDoor Final Project

Project Description:
TasteAtDoor is a catering platform for large events such as weddings, corporate meetings, receptions, celebrations, and outdoor events. Customers can select nearby caterers, view catering packages, choose guest count, add packages to cart, complete payment simulation, generate agreement PDFs, use chat/live call, and submit package/caterer reviews.

Technologies:
- ASP.NET Core MVC
- Entity Framework Core
- SQL Server LocalDB
- ASP.NET Core Identity
- SignalR
- QuestPDF
- Google Maps API
- Bootstrap / CSS / JavaScript

How to Run:
1. Open the project folder.
2. Open SQL Server Management Studio.
3. Run TasteAtDoor_Final_Database_Schema_And_Data.sql.
4. Check appsettings.json connection string:
   Server=(localdb)\MSSQLLocalDB;Database=TasteAtDoorWeek2Db;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true
5. Open terminal inside the TasteAtDoor folder.
6. Run:
   dotnet restore
   dotnet build
   dotnet watch

Default Admin:
Email: admin@tasteatdoor.com
Password: Admin123

Important Notes:
- Customer login uses a 6-digit email verification code.
- Caterer accounts can manage catering packages and locations.
- Agreement PDFs include the caterer logo if a PNG/JPG logo is uploaded in the caterer profile.
- Database script includes both schema and demo data.