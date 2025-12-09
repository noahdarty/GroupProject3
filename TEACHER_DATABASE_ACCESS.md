# How to Access the Heroku Database

## For Your Teacher

Your teacher can access the database in several ways:

## Method 1: MySQL Workbench (Recommended - Easiest)

1. **Get the Database Connection String:**
   - Go to your Heroku Dashboard: https://dashboard.heroku.com
   - Select your app: `vulnradar2` (or your app name)
   - Go to **Settings** tab
   - Click **Reveal Config Vars**
   - Find `JAWSDB_URL` or `ConnectionStrings__DefaultConnection`
   - Copy the connection string (it looks like: `mysql://user:password@host:port/database`)

2. **Connect in MySQL Workbench:**
   - Open MySQL Workbench
   - Click the **+** button to create a new connection
   - **Connection Method:** Standard (TCP/IP)
   - **Hostname:** Extract from connection string (e.g., `hcm4e9frmbwfez47.cbetxkdyhwsb.us-east-1.rds.amazonaws.com`)
   - **Port:** Extract from connection string (usually `3306`)
   - **Username:** Extract from connection string (e.g., `u6ya6l1ogtazrvrs`)
   - **Password:** Extract from connection string (e.g., `l15y9wr9dj2i4yla`)
   - **Default Schema:** Extract database name from connection string (e.g., `qfewi9g30broii73`)
   - Click **Test Connection** to verify
   - Click **OK** to save

3. **View Tables:**
   - Once connected, expand the database in the left sidebar
   - You'll see all tables: Users, Companies, Vendors, Vulnerabilities, Tasks, etc.

## Method 2: Share Connection String Directly

You can share the connection string with your teacher:

1. Go to Heroku Dashboard → Your App → Settings → Config Vars
2. Copy the `JAWSDB_URL` value
3. Share it with your teacher (they can use it in MySQL Workbench or any MySQL client)

**Format:** `mysql://username:password@host:port/database`

## Method 3: Heroku CLI (Advanced)

If your teacher has Heroku CLI installed:

```bash
heroku pg:psql --app vulnradar2
```

Or for JawsDB MySQL:

```bash
heroku mysql:credentials --app vulnradar2
```

## Method 4: Heroku Dashboard (View Only)

Your teacher can:
1. Go to https://dashboard.heroku.com
2. If they have access to your app, they can see:
   - Database add-ons
   - Connection info
   - But they can't directly query the database from the dashboard

## Recommended Approach

**Best for your teacher:** Share the connection string (Method 2) so they can connect with MySQL Workbench (Method 1). This gives them full access to view all tables and data.

## Security Note

- The connection string contains sensitive credentials
- Share it securely (email, secure message, etc.)
- Consider changing the password after the course if needed

