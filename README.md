# Online Quiz Application (ASP.NET Core MVC + PostgreSQL)

This version is configured for **PostgreSQL** (switched from SQL Server) so it can be deployed for free on **Render.com**.

## Local Development Setup

### 1. Run PostgreSQL locally via Docker
```bash
docker run -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=OnlineQuizDb -p 5432:5432 --name pg_quiz -d postgres:16
```

### 2. Restore packages
```bash
dotnet restore
```

### 3. Create migrations
```bash
dotnet ef migrations add InitialCreate
```

### 4. Run the app
Migrations and seed data (admin user/roles) are applied automatically on startup.
```bash
dotnet run
```

Open `http://localhost:5000`.

### Default Admin Account
- Email: `admin@quizapp.com`
- Password: `Admin@123`

---

## Deploying to Render.com (Free Tier)

### 1. Push this project to GitHub
Create a new repo and push all files (the `.gitignore` excludes `bin/`, `obj/`, etc.).

### 2. Create a PostgreSQL database on Render
- Render Dashboard -> New -> PostgreSQL
- Choose the **Free** plan
- Once created, copy the **Internal Database URL** (or External, if connecting from outside Render)

### 3. Create a Web Service on Render
- Render Dashboard -> New -> Web Service
- Connect your GitHub repo
- Environment: **Docker** (it will detect the `Dockerfile`)
- Plan: **Free**

### 4. Set environment variable for connection string
In the Web Service -> Environment tab, add:

```
ConnectionStrings__DefaultConnection = Host=<your-render-postgres-host>;Port=5432;Database=<dbname>;Username=<user>;Password=<password>;SSL Mode=Require;Trust Server Certificate=true
```

Render's Postgres connection details are shown on the database's info page. Note: Render Postgres requires SSL, hence `SSL Mode=Require;Trust Server Certificate=true`.

### 5. Deploy
Render will build the Docker image and deploy automatically. On first startup, the app:
- Applies all EF Core migrations (`db.Database.MigrateAsync()`)
- Seeds the Admin role and default admin account

### 6. Access your app
Render gives you a URL like `https://your-app-name.onrender.com`. Share this with your group.

### Notes
- Free tier web services sleep after 15 minutes of inactivity; the first request after sleeping takes ~30-60 seconds to wake up.
- Free Postgres databases on Render expire after 90 days unless upgraded -- re-create or back up data as needed for long-term use.
- Change the default admin password after first login for security.

## Project Structure
```
OnlineQuizApp/
├── Models/              # EF Core entities
├── Data/                # DbContext + SeedData
├── Controllers/         # Home, Quiz, Category, QuizAdmin, Question
├── ViewModels/          # Play/Result/Submission view models
├── Views/               # Razor views
├── Areas/Identity/      # Login, Register, Logout, Lockout pages
├── wwwroot/             # CSS/JS
├── Dockerfile           # For Render deployment
├── Program.cs
└── appsettings.json
```
