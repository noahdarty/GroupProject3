# Set Connection String Using Heroku Dashboard

## Step-by-Step Instructions

### Step 1: Log into Heroku Dashboard
1. Go to https://dashboard.heroku.com
2. Log in with your Heroku account

### Step 2: Select Your App
1. Click on your app name from the list (or create a new app if you haven't already)
2. If you need to create an app:
   - Click "New" → "Create new app"
   - Enter an app name (e.g., `vulnradar-api`)
   - Choose a region
   - Click "Create app"

### Step 3: Navigate to Config Vars
1. In your app's dashboard, click on the **"Settings"** tab at the top
2. Scroll down to the **"Config Vars"** section
3. Click the **"Reveal Config Vars"** button (if it says "Hide Config Vars", you're already there)

### Step 4: Add the Connection String
1. Click **"Add"** or **"Edit"** button
2. In the **KEY** field, enter:
   ```
   ConnectionStrings__DefaultConnection
   ```
   (Note: Use double underscore `__` not a single underscore)

3. In the **VALUE** field, paste this entire connection string:
   ```
   Server=i5x1cqhq5xbqtv00.cbetxkdyhwsb.us-east-1.rds.amazonaws.com;Port=3306;Database=yysdw4264cwdz2h0;User=vdr6h9c8xf3uat9h;Password=hoks78tpbg6np3cu;SslMode=Required;
   ```

4. Click **"Add"** or **"Save"** to save the config var

### Step 5: Verify It's Set
You should now see a row in the Config Vars table showing:
- **KEY**: `ConnectionStrings__DefaultConnection`
- **VALUE**: (your connection string, partially hidden for security)

## Important Notes

- The double underscore `__` in `ConnectionStrings__DefaultConnection` is important - it tells .NET to map this to `ConnectionStrings:DefaultConnection` in your app
- Make sure `SslMode=Required;` is included at the end - Heroku databases require SSL
- After setting this, you may need to restart your app for the changes to take effect

## Restart Your App (if deployed)
1. Go to the **"More"** menu (three dots) in the top right
2. Click **"Restart all dynos"**

## That's It! ✅

Your app will now use the Heroku database connection string when it runs on Heroku.

