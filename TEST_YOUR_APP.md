# How to Test Your App

## Quick Tests

### 1. Test Backend is Running
Open in browser: `https://vulnradar.herokuapp.com/`
- ✅ Should see: `{"Message":"VulnRadar API is running"...}`

### 2. Test Database Connection
Open in browser: `https://vulnradar.herokuapp.com/api/database/test`
- ✅ Should see: `{"Success":true,"Message":"Database connection successful"...}`
- ❌ If error: Database connection issue

### 3. Test Companies Endpoint
Open in browser: `https://vulnradar.herokuapp.com/api/companies`
- ✅ Should see: `{"Success":true,"Count":1,"Companies":[...]}`
- ❌ If empty: No companies in database (run seed_data_heroku.sql)
- ❌ If error: Check connection string

### 4. Test Vendors Endpoint
Open in browser: `https://vulnradar.herokuapp.com/api/vendors`
- ✅ Should see: `{"Success":true,"Count":10,"Vendors":[...]}`
- ❌ If empty: No vendors (run seed_data_heroku.sql)

### 5. Test Frontend Connection
1. Open your frontend page
2. Open browser console (F12)
3. Look for errors
4. Try to sign up or log in
5. Check if companies dropdown loads

## What Each Test Means

- **Backend Running** = Your API is deployed and responding
- **Database Connection** = Your Heroku database is connected
- **Companies Endpoint** = Database has data and queries work
- **Frontend Connection** = Your HTML can talk to your API

## If Something Fails

- **Backend not running**: Check Heroku logs, redeploy
- **Database connection fails**: Check config var in Heroku
- **Empty results**: Run SQL scripts in MySQL Workbench
- **Frontend errors**: Check CORS, check API URL in api-config.js

