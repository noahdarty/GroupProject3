# VulnRadar

A cybersecurity threat management system for Bio-ISAC that helps companies filter and manage high-volume vulnerability threats by intelligently prioritizing vulnerabilities based on their specific vendor selections and technology use cases.

## Getting Started

This project consists of a .NET 8.0 backend API and a vanilla HTML/CSS/JavaScript frontend.

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- A web browser (Chrome, Firefox, Edge, etc.)
- A simple HTTP server for the frontend (or use VS Code Live Server extension)

### Running the Backend

**Easiest way:** Double-click `start-backend.bat` in the project root.

**Manual way:**
1. Navigate to the backend directory:
   ```bash
   cd backend/MyProject.API
   ```

2. Restore dependencies (if needed):
   ```bash
   dotnet restore
   ```

3. Run the API:
   ```bash
   dotnet run
   ```

   The API will start on:
   - HTTP: `http://localhost:5155`
   - HTTPS: `https://localhost:7077`
   - Swagger UI: `http://localhost:5155/swagger` (when running in Development mode)

### Running the Frontend

**Easiest way:** Double-click `start-frontend.bat` in the project root.

This will:
- Start a local HTTP server (using Python if available)
- Automatically open the frontend in your browser

**Alternative options:**

#### Option 1: VS Code Live Server (Recommended)
1. Install the "Live Server" extension in VS Code
2. Right-click on `frontend/index.html`
3. Select "Open with Live Server"
4. The frontend will open in your browser (typically at `http://localhost:5500`)

#### Option 2: Python HTTP Server
```bash
cd frontend
python -m http.server 8080
```
Then open `http://localhost:8080` in your browser.

#### Option 3: Node.js http-server
```bash
npx http-server frontend -p 8080
```
Then open `http://localhost:8080` in your browser.

### Testing the Connection

1. Start the backend first (see "Running the Backend" above)
2. Start the frontend (see "Running the Frontend" above)
3. Open the frontend in your browser
4. Click "Test Backend Connection" to verify the API is accessible
5. Click "Fetch Weather Forecast" to test the API endpoint

### API Endpoints

- `GET /weatherforecast` - Returns a sample weather forecast (5 days)

### Troubleshooting

- **CORS Errors**: Make sure the backend is running and CORS is configured correctly
- **Connection Refused**: Verify the backend is running on `http://localhost:5155`
- **Port Already in Use**: Change the port in `backend/MyProject.API/Properties/launchSettings.json` or stop the conflicting process