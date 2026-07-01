Social Media Platform
OdinBook is a full-stack, real-time social media platform designed to connect users through an intuitive, modern interface. Built with a robust ASP.NET Core backend and a highly responsive React frontend, the platform features instant messaging, interactive feeds, dynamic notification systems, and secure user relationships.

🚀 Key Features
Secure Authentication: Custom JWT-based authentication coupled with BCrypt password hashing.

Real-Time Interactions: Powered by SignalR for instantaneous instant messaging, live typing indicators, and read receipts.

Robust Social Graph: Comprehensive follow system featuring explicit pending, accepted, and rejected request workflows.

Dynamic Feed & Engagement: Personalized post feeds with support for text content, likes, nesting comments, and media.

Media Management: Seamless image uploading and processing integrated directly with Cloudinary.

Instant Notifications: Real-time push alerts for likes, comments, and incoming connection requests.

🛠️ Tech Stack
Backend
Framework: ASP.NET Core (Web API)

Real-Time Communication: SignalR

Database ORM: Entity Framework Core (EF Core)

Database: Supabase PostgreSQL

Logging: Serilog

Containerization: Docker

Frontend
Library/Language: React, TypeScript

Data Fetching & State Management: TanStack Query (React Query)

Styling: Tailwind CSS

🏗️ Architecture & Infrastructure
The application separates concerns cleanly across a decoupled architecture optimized for performance, scalability, and seamless data synchronization:

┌──────────────┐         HTTPS / REST          ┌─────────────────────┐
│  React App   ├──────────────────────────────>│   ASP.NET Core API  │
│   (Vercel)   │<──────────────────────────────┤  (Render / Docker)  │
└──────┬───────┘           WebSockets          └──────────┬──────────┘
       │                 (SignalR Hubs)                   │
       ▼                                                  ▼
┌──────────────┐                               ┌─────────────────────┐
│  Cloudinary  │                               │ Supabase PostgreSQL │
│(Media Assets)│                               │  (Data Persistence) │
└──────────────┘                               └─────────────────────┘
Frontend Deployment: Hosted on Vercel to provide an optimized, global CDN-driven delivery of static UI assets.

Backend Deployment: Containerized with Docker and hosted on Render, ensuring uniform runtime environments and predictable deployments.

Data Tier: Hosted on Supabase (PostgreSQL), utilizing structured relational indexing to handle complex social operations efficiently.

⚙️ Getting Started
Prerequisites
.NET 8 SDK (or latest stable)

Node.js (v18+ recommended)

Docker (Optional, for containerized local runs)

Backend Configuration
Navigate to the backend directory:

Bash
cd backend/OdinBook.Api
Create an appsettings.Development.json file and populate your environment strings:

JSON
{
  "ConnectionStrings": {
    "DefaultConnection": "YOUR_SUPABASE_POSTGRES_CONNECTION_STRING"
  },
  "JwtSettings": {
    "Secret": "YOUR_SUPER_SECRET_JWT_KEY_MIN_32_CHARS",
    "Issuer": "OdinBookAuth",
    "Audience": "OdinBookClient"
  },
  "CloudinarySettings": {
    "CloudName": "YOUR_CLOUD_NAME",
    "ApiKey": "YOUR_API_KEY",
    "ApiSecret": "YOUR_API_SECRET"
  }
}
Run migrations and update the database:

Bash
dotnet ef database update
Start the server:

Bash
dotnet run
Frontend Configuration
Navigate to the frontend directory:

Bash
cd frontend
Create a .env file in the root:

Code snippet
VITE_API_URL=http://localhost:5000/api
VITE_HUB_URL=http://localhost:5000/hubs
Install dependencies and start the development server:

Bash
npm install
Bash
npm run dev
🔒 Security Practices
Password Safety: Raw passwords never touch the persistence layer; they are securely transformed using the BCrypt work factor algorithm before indexing.

Stateful Context: API endpoints are heavily guarded behind authorization layers validating incoming headers against the JWT signature.

CORS Policies: Configured specifically to restrict resource consumption exclusively to authorized frontend origins.
