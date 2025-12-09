# Simple HTTP Server for Frontend
$port = 8080
$url = "http://localhost:$port/"

Write-Host "Starting server on $url" -ForegroundColor Green
Write-Host "Press Ctrl+C to stop" -ForegroundColor Yellow

# Start browser
Start-Process $url

# Create HTTP listener
$listener = New-Object System.Net.HttpListener
$listener.Prefixes.Add($url)
$listener.Start()

try {
    while ($listener.IsListening) {
        $context = $listener.GetContext()
        $request = $context.Request
        $response = $context.Response
        
        # Get file path
        $localPath = $request.Url.LocalPath
        if ($localPath -eq "/") {
            $localPath = "/index.html"
        }
        
        $filePath = Join-Path $PSScriptRoot $localPath.TrimStart('/')
        
        # Serve file or 404
        if (Test-Path $filePath -PathType Leaf) {
            $content = [System.IO.File]::ReadAllBytes($filePath)
            
            # Set MIME type
            $mimeType = "text/html"
            if ($filePath -like "*.css") { $mimeType = "text/css" }
            elseif ($filePath -like "*.js") { $mimeType = "application/javascript" }
            elseif ($filePath -like "*.json") { $mimeType = "application/json" }
            elseif ($filePath -like "*.png") { $mimeType = "image/png" }
            elseif ($filePath -like "*.jpg" -or $filePath -like "*.jpeg") { $mimeType = "image/jpeg" }
            elseif ($filePath -like "*.svg") { $mimeType = "image/svg+xml" }
            
            $response.ContentType = $mimeType
            $response.ContentLength64 = $content.Length
            $response.StatusCode = 200
            $response.OutputStream.Write($content, 0, $content.Length)
        } else {
            $response.StatusCode = 404
            $notFound = [System.Text.Encoding]::UTF8.GetBytes("404 Not Found")
            $response.ContentLength64 = $notFound.Length
            $response.OutputStream.Write($notFound, 0, $notFound.Length)
        }
        
        $response.Close()
    }
} finally {
    $listener.Stop()
    Write-Host "Server stopped." -ForegroundColor Red
}






