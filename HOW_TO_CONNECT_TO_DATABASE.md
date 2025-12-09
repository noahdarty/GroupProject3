# How to Connect to the Heroku Database (For Your Teacher)

## Step-by-Step Instructions

### Step 1: Get the Connection String
You should receive a connection string that looks like:
```
mysql://u6ya6l1ogtazrvrs:115y9wr9dj2i4yla@hcm4e9frmbwfez47.cbetxkdyhwsb.us-east-1.rds.amazonaws.com:3306/qfewi9g30broii73
```

### Step 2: Parse the Connection String
Break it down into parts:
- **Format:** `mysql://username:password@host:port/database`
- **Username:** `u6ya6l1ogtazrvrs` (before the `:`)
- **Password:** `115y9wr9dj2i4yla` (between `:` and `@`)
- **Host:** `hcm4e9frmbwfez47.cbetxkdyhwsb.us-east-1.rds.amazonaws.com` (between `@` and `:`)
- **Port:** `3306` (between `:` and `/`)
- **Database:** `qfewi9g30broii73` (after the `/`)

### Step 3: Open MySQL Workbench
1. Open MySQL Workbench on your computer
2. You'll see the MySQL Connections screen

### Step 4: Create New Connection
1. Click the **+** button (or click "MySQL Connections" → "+" icon)
2. A new connection form will appear

### Step 5: Enter Connection Details
Fill in the form with the parsed values:

**Connection Name:** (Give it a name like "VulnRadar Heroku" or "Student Database")

**Connection Method:** Select **Standard (TCP/IP)**

**Parameters Tab:**
- **Hostname:** Paste the host part (e.g., `hcm4e9frmbwfez47.cbetxkdyhwsb.us-east-1.rds.amazonaws.com`)
- **Port:** Paste the port (usually `3306`)
- **Username:** Paste the username (e.g., `u6ya6l1ogtazrvrs`)
- **Password:** Click "Store in Keychain" or "Store in Vault", then enter the password (e.g., `115y9wr9dj2i4yla`)
- **Default Schema:** Paste the database name (e.g., `qfewi9g30broii73`)

### Step 6: Test Connection
1. Click **Test Connection** button at the bottom
2. If successful, you'll see "Successfully made the MySQL connection"
3. Click **OK** to save the connection

### Step 7: Connect
1. Double-click the connection you just created
2. Enter the password if prompted
3. You're now connected!

### Step 8: View Tables
1. In the left sidebar, expand your database (click the arrow next to the database name)
2. Expand "Tables"
3. You'll see all tables:
   - Users
   - Companies
   - Vendors
   - Vulnerabilities
   - Tasks
   - CompanyVendors
   - UserCompanies
   - etc.

### Step 9: View Data
1. Right-click on any table (e.g., "Users")
2. Select **Select Rows - Limit 1000**
3. You'll see all the data in that table

## Quick Reference: Where to Paste What

When creating the connection in MySQL Workbench:

| Field | What to Paste |
|-------|---------------|
| **Hostname** | The part between `@` and `:` (e.g., `hcm4e9frmbwfez47.cbetxkdyhwsb.us-east-1.rds.amazonaws.com`) |
| **Port** | The number between `:` and `/` (usually `3306`) |
| **Username** | The part between `mysql://` and `:` (e.g., `u6ya6l1ogtazrvrs`) |
| **Password** | The part between `:` and `@` (e.g., `115y9wr9dj2i4yla`) |
| **Default Schema** | The part after `/` (e.g., `qfewi9g30broii73`) |

## Troubleshooting

**Connection fails?**
- Check that all parts of the connection string were copied correctly
- Make sure there are no extra spaces
- Verify the password is correct (it's case-sensitive)

**Can't see tables?**
- Make sure you expanded the database in the left sidebar
- Check that you're connected (green dot next to connection name)

**Need to run SQL scripts?**
- Click on your database in the left sidebar
- Go to File → Open SQL Script
- Or click the SQL editor icon and paste SQL code

