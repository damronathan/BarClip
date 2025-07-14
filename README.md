# BarClip

## Overview
BarClip is a platform that automates video trimming for weightlifters using computer vision to determine barbell movement. This repo contains the back end source code.

## Live MVP
[Try BarClip here](https://www.barclip.com)

## Usage
1. **Create an account** - Sign up using the authentication system
2. **Upload your video** - Use the choose video button to select which video you would like to trim. (You can still use videos that are not lifting related and the app will return the full video.)
3. **Process video** - Press the trim video button and watch the magic happen.
4. **View or download** - Trimmed video will automatically populate a media player with a download link.

> **Tip:** Refresh the page to start the process over.

## Dependencies

### Frontend
- React

### Backend
- ASP.NET Core Web API
- Azure Functions (event-driven microservices, app role authentication)
- SignalR (real-time communication)

### Database
- SQL Server
- Entity Framework Core (ORM)

### AI/ML
- YOLOv8 (custom-trained model)
- ONNX

### Video Processing
- FFmpeg

### Authentication
- Microsoft Entra External ID

### Security
- Microsoft Entra External ID for user authentication
- App role authentication for Azure Functions

### Infrastructure
- Cloudflare (React frontend hosting)
- Azure App Service (ASP.NET Core Web API hosting)
- Docker/DockerHub (containerization and hosting)
- Azure Container Apps (Azure Function hosting)
- Azure SQL Server (database hosting)
- Azure Storage Account (cloud storage)
