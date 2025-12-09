# Deploy Your App to Heroku - Quick Steps

Your code is committed! Now you need to deploy it. Since Heroku CLI isn't installed, use one of these methods:

## Option 1: Connect GitHub (Easiest - Recommended)

1. Go to https://dashboard.heroku.com/apps/vulnradar
2. Click the **"Deploy"** tab
3. Scroll down to **"Deployment method"**
4. Click **"Connect to GitHub"**
5. Authorize Heroku to access your GitHub
6. Search for your repo: `GroupProject3`
7. Click **"Connect"**
8. Choose the branch: `NoahBranch`
9. Click **"Enable Automatic Deploys"** (optional - auto-deploys on push)
10. Click **"Deploy Branch"** to deploy now

## Option 2: Manual Deploy via GitHub

1. First, push your code to GitHub:
   ```bash
   git push origin NoahBranch
   ```

2. Then in Heroku Dashboard:
   - Go to https://dashboard.heroku.com/apps/vulnradar
   - Click **"Deploy"** tab
   - Connect GitHub (if not already)
   - Click **"Deploy Branch"**

## Option 3: Install Heroku CLI (If you want to use command line)

1. Download from: https://devcenter.heroku.com/articles/heroku-cli
2. Install it
3. Then run:
   ```bash
   heroku git:remote -a vulnradar
   git push heroku NoahBranch:main
   ```

## After Deployment

Once deployed, check:
- https://vulnradar.herokuapp.com/ (should show API is running)
- https://vulnradar.herokuapp.com/swagger (should show all your endpoints)

