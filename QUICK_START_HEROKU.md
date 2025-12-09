# Quick Start: Deploy to Heroku (For Your Teacher)

## What You Need to Do (5 Steps)

### Step 1: Set Database Connection on Heroku

Open PowerShell and run this command (replace `your-app-name` with your Heroku app name):

```bash
heroku config:set ConnectionStrings__DefaultConnection="Server=i5x1cqhq5xbqtv00.cbetxkdyhwsb.us-east-1.rds.amazonaws.com;Port=3306;Database=yysdw4264cwdz2h0;User=vdr6h9c8xf3uat9h;Password=hoks78tpbg6np3cu;SslMode=Required;" -a your-app-name
```

### Step 2: Create Tables in Your Database

1. Open **MySQL Workbench** (or HeidiSQL)
2. Connect using these credentials:
   - Host: `i5x1cqhq5xbqtv00.cbetxkdyhwsb.us-east-1.rds.amazonaws.com`
   - Username: `vdr6h9c8xf3uat9h`
   - Password: `hoks78tpbg6np3cu`
   - Port: `3306`
   - Database: `yysdw4264cwdz2h0`

3. Run these SQL files **in this order**:
   - Open and run: `backend/MyProject.API/Database/schema_heroku.sql`
   - Then run: `backend/MyProject.API/Database/seed_data_heroku.sql`

4. Refresh your database view - you should now see 10 tables!

### Step 3: Deploy Your Code

If you haven't deployed yet:

```bash
# Make sure you're in the project folder
cd C:\Users\noahd\OneDrive\Documents\Mis321\GroupP3\GroupProject3

# Login to Heroku (if not already)
heroku login

# Create app (if you haven't already)
heroku create your-app-name

# Deploy
git init  # if you haven't already
git add .
git commit -m "Deploy to Heroku"
heroku git:remote -a your-app-name
git push heroku main
```

### Step 4: Give Your Teacher the Database Info

Share these connection details with your teacher:

```
Host: i5x1cqhq5xbqtv00.cbetxkdyhwsb.us-east-1.rds.amazonaws.com
Username: vdr6h9c8xf3uat9h
Password: hoks78tpbg6np3cu
Port: 3306
Database: yysdw4264cwdz2h0
```

They can connect using MySQL Workbench, HeidiSQL, or any MySQL client to see all your tables!

### Step 5: Verify Everything Works

Check your API is running:
```bash
heroku open
```

Or visit: `https://your-app-name.herokuapp.com/swagger`

## That's It! 🎉

Your teacher can now:
- ✅ See all your database tables
- ✅ Query the data
- ✅ Verify your database structure

## Need Help?

- Check logs: `heroku logs --tail`
- See full guide: `HEROKU_DEPLOYMENT.md`

